using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace WorldBoxModdingToolChain.Handlers
{
    public class HoverHandler : IHoverHandler
    {
        public Task<Hover> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = "**Hover Info, You Hovered Over this!"
                })
            });
        }

        public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            return new HoverRegistrationOptions
            {
                DocumentSelector = new TextDocumentSelector(new TextDocumentFilter
                {
                    Pattern = "**/*.cs", // Replace with your file type or pattern
                    Language = "csharp"  // The language ID, could be plaintext or your custom language
                }),
                WorkDoneProgress = true // If you want to show work progress
            };
        }


    }
}
