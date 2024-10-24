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
using WorldBoxModdingToolChain.Utils;

namespace WorldBoxModdingToolChain.Handlers
{
    public class TextDocumentHandler : TextDocumentSyncHandlerBase
    {
        
        
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
            await Task.Yield();
            return MediatR.Unit.Value;
        }

        public override Task<MediatR.Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            FileLogger.Log($"Document changed: {request.TextDocument.Uri}");
            return MediatR.Unit.Task;
        }

        public override Task<MediatR.Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            FileLogger.Log($"Document saved: {request.TextDocument.Uri}");

            return MediatR.Unit.Task;
        }

        public override Task<MediatR.Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            FileLogger.Log($"Document closed: {request.TextDocument.Uri}");
            return MediatR.Unit.Task;
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
