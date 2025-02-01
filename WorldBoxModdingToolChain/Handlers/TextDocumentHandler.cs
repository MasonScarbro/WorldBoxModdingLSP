using MediatR;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using WorldBoxModdingToolChain.Analysis;
using WorldBoxModdingToolChain.Utils;
using static ICSharpCode.Decompiler.IL.Transforms.Stepper;
using Microsoft.CodeAnalysis;
using Diagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using DiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;

namespace WorldBoxModdingToolChain.Handlers
{
    public class TextDocumentHandler : TextDocumentSyncHandlerBase
    {
        private readonly IDictionary<Uri, SourceText> _documentContents;
        private readonly AnalysisStorage _analysisStorage;
        private readonly PathLibrary _pathLibrary;
        private readonly ILanguageServerFacade _facade;
        private readonly GameCodeMetaDataRender _metaDataRender;
        private readonly DocumentParserService _documentParserService;

        public TextDocumentHandler(IDictionary<Uri, SourceText> documentContents,
            AnalysisStorage analysisStorage,
            PathLibrary pathLibrary,
            ILanguageServerFacade facade,
            GameCodeMetaDataRender metaDataRender,
            DocumentParserService documentParserService)
        {
            _documentContents = documentContents;
            _analysisStorage = analysisStorage;
            _pathLibrary = pathLibrary;
            _facade = facade;
            _metaDataRender = metaDataRender;
            _documentParserService = documentParserService;
        }

        private readonly TextDocumentSelector _textDocumentSelector = new TextDocumentSelector(
            new TextDocumentFilter
            {
                Pattern = "**/*.cs"
            }
        );
        public static TextDocumentSyncKind Change => TextDocumentSyncKind.Full;


        public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            return new TextDocumentAttributes(uri, "csharp");
        }

        public override async Task<MediatR.Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {

            FileLogger.Log($"Document opened: {request.TextDocument.Uri}");
            var sourceText = SourceText.From(request.TextDocument.Text);
            _documentContents[(Uri)request.TextDocument.Uri] = sourceText;
            AnalyzeAndStoreVariables((Uri)request.TextDocument.Uri, request.TextDocument.Text);

            await Task.Yield();
            return MediatR.Unit.Value;
        }

        public override Task<MediatR.Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            var documentUri = (Uri)request.TextDocument.Uri;
            FileLogger.Log($"Document changed: {request.TextDocument.Uri}");

            

            foreach (var change in request.ContentChanges)
            {
                var sourceText = SourceText.From(change.Text);
                _documentContents[documentUri] = sourceText;
            }
            HandleDiagnostics(request);
            return MediatR.Unit.Task;
        }

        public override Task<MediatR.Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {

            var documentUri = (Uri)request.TextDocument.Uri;
            FileLogger.Log($"Document saved: {request.TextDocument.Uri}");
            //Changed from string.Join("\n", _documentContents[documentUri]);
            var documentText = _documentContents[documentUri];

            string updatedText = HandleGreaterSuggestionTypo(documentText.ToString());

            if (updatedText != documentText.ToString())
            {
                try
                {
                    string originalPath = request.TextDocument.Uri.Path;

                    // Remove any leading '/' or duplicate path prefixes
                    string cleanPath = originalPath.TrimStart('/');

                    // If the path already starts with a drive letter, use it directly
                    // Otherwise, ensure it's a full path
                    string filePath = Path.IsPathRooted(cleanPath)
                        ? cleanPath
                        : Path.GetFullPath(cleanPath);
                    string fileContent = File.ReadAllText(filePath);
                    File.WriteAllText(filePath, updatedText);
                    //_documentContents[documentUri] = SourceText.From(updatedText);
                    FileLogger.Log($"Performed Mods on: {filePath}");
                }
                catch (Exception ex)
                {
                    FileLogger.Log(ex.Message);
                }
                
            }
            //documentText = _documentContents[documentUri]; //Not sure if this is really necesarry but




            AnalyzeAndStoreVariables(documentUri, documentText.ToString());
            return MediatR.Unit.Task;
        }

        public override Task<MediatR.Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            FileLogger.Log($"Document closed: {request.TextDocument.Uri}");

            var filePath = DocumentUri.GetFileSystemPath(request.TextDocument.Uri);

            // TODO: get the folder path dynamically on startup
            var decompiledFolderPath = _pathLibrary.decompiledPath;

            if (filePath.StartsWith(decompiledFolderPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        FileLogger.Log($"Deleted decompiled file: {filePath}");
                    }
                    else
                    {
                        FileLogger.Log($"File not found for deletion: {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.Log($"Error deleting file: {ex.Message}");
                }
            }
            else
            {
                FileLogger.Log($"Closed file is not in the decompiled folder: {filePath}");
            }

            return MediatR.Unit.Task;
        }

        private void AnalyzeAndStoreVariables(Uri documentUri, string documentText)
        {
            var analysisResults = VariableAnalyzer.GetVariableDictionary(documentText);
            _analysisStorage.UpdateVariables(documentUri, analysisResults);

        }

        private string HandleGreaterSuggestionTypo(string text)
        {

            int location = text.IndexOf('$');
            while (location != -1)
            {
                if (location + 1 >= text.Length || !text[location + 1].Equals('"'))
                {
                    text = text.Remove(location, 1);
                }
                else
                {
                    location++;
                }

                location = text.IndexOf('$');
            }
            return text;

        }

        private void HandleDiagnostics(DidChangeTextDocumentParams request)
        {
            FileLogger.Log("is Dis even gettin called?");
            var static_classes = _metaDataRender.GetStaticClasses();
            var documentLines = _documentContents[(Uri)request.TextDocument.Uri];
            var documentText = string.Join(Environment.NewLine, documentLines);
            var diagnostics = ImmutableArray<Diagnostic>.Empty.ToBuilder();

            // Parse syntax tree and get root
            var syntaxTree = _documentParserService.GetOrParseSyntaxTree((Uri)request.TextDocument.Uri, documentText);
            var root = _documentParserService.GetRootNode(syntaxTree);
            var text = _documentParserService.GetText(syntaxTree);
            
            foreach (var line in text.Lines)
            {

                var node = root.FindNode(line.Span);


                if (node.Parent is MemberAccessExpressionSyntax memberAccess)
                {

                    if (!static_classes.Contains(memberAccess.Expression.ToString()) && _metaDataRender.GetClasses().Contains(memberAccess.Expression.ToString()))
                    {
                        var fields_and_properties = _metaDataRender.GetFieldsAndProperties();
                        bool needsReport = true;
                        List<string> fullExpr = [.. GetFullMemberAccess(memberAccess).Split('.')];

                        if (fullExpr.Count > 1)
                        {
                            //TODO: Optimize this, also this just assumes that the chain is correct after one, Check Issue #8
                            if (fields_and_properties.TryGetValue(memberAccess.Expression.ToString(), out var fieldList) &&
                                fieldList.Any(field => field.Name == fullExpr[1] && field.IsStatic))
                            {
                                needsReport = false;
                            }
                        }
                        
                        FileLogger.Log("Ahh cant doo dat");
                        FileLogger.Log("LineStart: " + node.SyntaxTree.GetLineSpan(node.Span).EndLinePosition);
                        FileLogger.Log("CharStart: " + memberAccess.Expression.Span.Start);
                        if (needsReport)
                        {
                            diagnostics.Add(new Diagnostic()
                            {
                                Code = "ErrorCode_001",
                                Severity = DiagnosticSeverity.Error,
                                Message = $"{memberAccess.Expression} is NOT static you must create an instance",
                                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line, memberAccess.Expression.Span.Start, node.SyntaxTree.GetLineSpan(node.Span).EndLinePosition.Line, memberAccess.Expression.Span.End),
                                Source = memberAccess.Expression.ToString(),
                                Tags = new Container<DiagnosticTag>(new DiagnosticTag[] { DiagnosticTag.Unnecessary })
                            });
                        }
                        


                    }

                }
            }


            _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
            {
                Diagnostics = new Container<Diagnostic>(diagnostics.ToArray()),
                Uri = request.TextDocument.Uri,
                Version = request.TextDocument.Version
            });
        }

        protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new TextDocumentSyncRegistrationOptions
            {
                DocumentSelector = _textDocumentSelector,
                Change = TextDocumentSyncKind.Full,
                Save = new SaveOptions { IncludeText = true }
            };
        }

        /// <TODO>
        /// 1. Put this in a utils
        /// 2. integrate this with anything else that gets list of full member acess
        /// </TODO>
        private string GetFullMemberAccess(SyntaxNode node)
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                string parentExpression = GetFullMemberAccess(memberAccess.Expression);
                return parentExpression + "." + memberAccess.Name;
            }
            else if (node is InvocationExpressionSyntax invocation && invocation.Expression is MemberAccessExpressionSyntax mAccess)
            {
                // Handle method calls like `traits.add()`
                return GetFullMemberAccess(mAccess); // Append () to indicate method calls
            }
            return node.ToString();
        }
    }
}
