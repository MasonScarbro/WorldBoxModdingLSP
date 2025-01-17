using ICSharpCode.Decompiler.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldBoxModdingToolChain.Utils
{
    public class DocumentParserService
    {
        private readonly ConcurrentDictionary<Uri, Microsoft.CodeAnalysis.SyntaxTree> _syntaxTreeCache = new();

        public Microsoft.CodeAnalysis.SyntaxTree GetOrParseSyntaxTree(Uri uri, string documentText)
        {
            if (_syntaxTreeCache.TryGetValue(uri, out var cachedTree) && cachedTree.ToString() == documentText)
            {
                return cachedTree;
            }
            //else

            var syntaxTree = CSharpSyntaxTree.ParseText(documentText);
            _syntaxTreeCache[uri] = syntaxTree;
            return syntaxTree;
        }

        public SyntaxNode GetRootNode(Microsoft.CodeAnalysis.SyntaxTree syntaxTree) => syntaxTree.GetRoot();

        public SyntaxToken FindTokenAtPosition(Microsoft.CodeAnalysis.SyntaxTree syntaxTree, int line, int character)
        {
            var text = syntaxTree.GetText();
            var pos = text.Lines.GetPosition(new Microsoft.CodeAnalysis.Text.LinePosition(line, character));
            return syntaxTree.GetRoot().FindToken(pos);
        }

        public int GetAbsolutePosition(Microsoft.CodeAnalysis.SyntaxTree syntaxTree, int line, int character)
        {
            var text = syntaxTree.GetText();
            return text.Lines.GetPosition(new Microsoft.CodeAnalysis.Text.LinePosition(line, character));
        }

        public SourceText GetText(Microsoft.CodeAnalysis.SyntaxTree syntaxTree)
        {
            return syntaxTree.GetText();
            
        }
        public void InvalidateCache(Uri documentUri)
        {
            _syntaxTreeCache.TryRemove(documentUri, out _);
        }
    }
}
