using MediatR;
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
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using WorldBoxModdingToolChain.Analysis;
using WorldBoxModdingToolChain.Utils;

namespace WorldBoxModdingToolChain.Handlers
{
    public class TextDocumentHandler : TextDocumentSyncHandlerBase
    {
        private readonly IDictionary<Uri, SourceText> _documentContents;
        private readonly AnalysisStorage _analysisStorage;
        private readonly PathLibrary _pathLibrary;

        public TextDocumentHandler(IDictionary<Uri, SourceText> documentContents, AnalysisStorage analysisStorage, PathLibrary pathLibrary)
        {
            _documentContents = documentContents;
            _analysisStorage = analysisStorage;
            _pathLibrary = pathLibrary;


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

        

        protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new TextDocumentSyncRegistrationOptions
            {
                DocumentSelector = _textDocumentSelector,
                Change = TextDocumentSyncKind.Full,
                Save = new SaveOptions { IncludeText = true }
            };
        }
    }
}
