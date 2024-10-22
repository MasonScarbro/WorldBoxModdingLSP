using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

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
                    Value = "**Hover Infon You Hovered Over this!"
                })
            });
        }

        public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            throw new NotImplementedException();
        }


    }
}
