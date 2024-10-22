using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldBoxModdingToolChain.Handlers
{
    public class CompletionHandler : ICompletionHandler
    {
        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new CompletionRegistrationOptions();
        }

        public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {

            var completionItems = new CompletionItem[]
            {

                new CompletionItem
                {
                    Label = "Dummy Test",
                    Kind = CompletionItemKind.Keyword,
                    InsertText = "Dummy"
                }

            };
            return Task.FromResult(new CompletionList(completionItems));
            
             
        }
    }
}
