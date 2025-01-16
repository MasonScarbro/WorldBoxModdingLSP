using MediatR;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldBoxModdingToolChain.Analysis;
using WorldBoxModdingToolChain.Utils;

namespace WorldBoxModdingToolChain.Handlers
{
    public class DiagnosticHandler : IDidChangeTextDocumentHandler
    {
        private readonly ILanguageServerFacade _facade;
        private readonly GameCodeMetaDataRender _metaDataRender;
        private readonly IDictionary<Uri, SourceText> _documentContents;
        public DiagnosticHandler(ILanguageServerFacade facade, GameCodeMetaDataRender metaDataRender, IDictionary<Uri, SourceText> documentContents) 
        { 
            _facade = facade;
            _metaDataRender = metaDataRender;
            _documentContents = documentContents;
        }

        public TextDocumentChangeRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new TextDocumentSyncRegistrationOptions
            {
                DocumentSelector = new TextDocumentSelector(
                    new TextDocumentFilter
                    {
                        Pattern = "**/*.cs"
                    }
                ),
                Change = TextDocumentSyncKind.Full,
                Save = new SaveOptions { IncludeText = true }
            };
        }

        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            FileLogger.Log("is Dis even gettin called?");
            var static_classes = _metaDataRender.GetStaticClasses();
            var documentLines = _documentContents[(Uri)request.TextDocument.Uri];
            var documentText = string.Join(Environment.NewLine, documentLines);
            

            // Parse syntax tree and get root
            var syntaxTree = CSharpSyntaxTree.ParseText(documentText);
            var root = syntaxTree.GetRoot();
            var text = syntaxTree.GetText();

            foreach (var line in text.Lines)
            {
                
                var node = root.FindNode(line.Span);

                if (node.Parent is MemberAccessExpressionSyntax memberAccess)
                {

                    FileLogger.Log("detected: " + memberAccess.Name);
                }
            }
            
            var diagnostics = ImmutableArray<Diagnostic>.Empty.ToBuilder();
            //diagnostics.Add(new Diagnostic()
            //{
            //    Code = "ErrorCode_001",
            //    Severity = DiagnosticSeverity.Error,
            //    Message = "Something bad happened",
            //    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, 0, 0),
            //    Source = "XXX",
            //    Tags = new Container<DiagnosticTag>(new DiagnosticTag[] { DiagnosticTag.Unnecessary })
            //});

            //_facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
            //{
            //    Diagnostics = new Container<Diagnostic>(diagnostics.ToArray()),
            //    Uri = request.TextDocument.Uri,
            //    Version = request.TextDocument.Version
            //});

            return Unit.Task;
        }
    }
}
