using Mono.Cecil;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldBoxModdingToolChain.Analysis
{
    public class GameClassMetaObject
    {
        public string Name { get; set; }
        public TypeReference? ReturnType { get; set; }
        public CompletionItemKind Kind { get; set; }
        public GameClassMetaObject(string name, TypeReference returnType, CompletionItemKind kind)
        {
            Name= name;
            ReturnType= returnType;
            Kind= kind;
        }

       
        public string TypeName() => ReturnType?.Name ?? "Unknown";


        public string KindName() => Kind.ToString();

        public override string ToString() =>
        $"{Name} ({TypeName()}) - {KindName()}";
    }
}
