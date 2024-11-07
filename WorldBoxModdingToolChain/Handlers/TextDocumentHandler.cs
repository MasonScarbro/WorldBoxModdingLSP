using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
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
        private readonly IDictionary<Uri, string[]> _documentContents;
        private readonly AnalysisStorage _analysisStorage;

        public TextDocumentHandler(IDictionary<Uri, string[]> documentContents, AnalysisStorage analysisStorage)
        {
            _documentContents = documentContents;
            _analysisStorage = analysisStorage;
        }

        private readonly TextDocumentSelector _textDocumentSelector = new TextDocumentSelector(
            new TextDocumentFilter
            {
                Pattern = "**/*.cs"
            }
        );
        public TextDocumentSyncKind Change => TextDocumentSyncKind.Full;
       

        public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            return new TextDocumentAttributes(uri, "csharp");
        }

        public override async Task<MediatR.Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {

            FileLogger.Log($"Document opened: {request.TextDocument.Uri}");
            _documentContents[(Uri)request.TextDocument.Uri] = request.TextDocument.Text.Split('\n');
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
                _documentContents[documentUri] = change.Text.Split('\n');
            }

            return MediatR.Unit.Task;
        }

        public override Task<MediatR.Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            var documentUri = (Uri)request.TextDocument.Uri;
            FileLogger.Log($"Document saved: {request.TextDocument.Uri}");
            var documentText = string.Join("\n", _documentContents[documentUri]);
            AnalyzeAndStoreVariables(documentUri, documentText);
            return MediatR.Unit.Task;
        }

        public override Task<MediatR.Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            FileLogger.Log($"Document closed: {request.TextDocument.Uri}");
            return MediatR.Unit.Task;
        }

        private void AnalyzeAndStoreVariables(Uri documentUri, string documentText)
        {
            var analysisResults = VariableAnalyzer.GetVariableDictionary(documentText);
            _analysisStorage.UpdateVariables(documentUri, analysisResults);

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
