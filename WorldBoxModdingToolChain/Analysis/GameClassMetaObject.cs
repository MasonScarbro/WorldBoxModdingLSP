using Mono.Cecil;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldBoxModdingToolChain.Utils;

namespace WorldBoxModdingToolChain.Analysis
{
    public class GameClassMetaObject
    {
        public string Name { get; set; }
        public TypeReference? ReturnType { get; set; }

        public bool IsStatic { get; set; }
        public string? Parameters { get; set; }
        public CompletionItemKind Kind { get; set; }

        public MarkupContent Documentation { get; }
        public GameClassMetaObject(string name, TypeReference returnType, CompletionItemKind kind, bool isStatic = false,string parameters = null)
        {
            Name= name;
            ReturnType= returnType;
            Kind= kind;
            IsStatic= isStatic;
            Parameters = parameters;
            Documentation = new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = ToString()
            };
        }

       
        public string TypeName() => ReturnType?.Name ?? "Unknown";


        public string KindName() => Kind.ToString();

        

        public override string ToString()
        {
            var result = $@"
### {Name}
- **Type:** `{TypeName()}`
";

            // Add parameters if available
            if (!string.IsNullOrEmpty(Parameters))
            {
                result += $@"
- **Parameters:** {Parameters}
";
                
            }

            // Add method kind info
            result += $@"
- **Kind:** `{KindName()}`

This is an auto-generated member of type `{TypeName()}`.
";

            return result;
        }
    }
}
