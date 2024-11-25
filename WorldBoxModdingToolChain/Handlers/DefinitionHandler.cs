using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using WorldBoxModdingToolChain.Utils;
using WorldBoxModdingToolChain.Analysis;

namespace WorldBoxModdingToolChain.Handlers
{
    public class DefinitionHandler : IDefinitionHandler
    {

        private readonly ClassDecompiler _classDecompiler;
        private readonly IDictionary<Uri, SourceText> _documentContents;
        public DefinitionHandler(ClassDecompiler classDecompiler, IDictionary<Uri, SourceText> documentContents) 
        { 
            _classDecompiler = classDecompiler;
            _documentContents = documentContents;
        }
        public async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
        {
            
            var documentUri = request.TextDocument.Uri;
            var filePath = DocumentUri.GetFileSystemPath(documentUri);

            FileLogger.Log($"Resolved file path: {filePath}");

            var pos = request.Position;

            if (!_documentContents.ContainsKey((Uri)documentUri))
            {
                FileLogger.Log($"No content found for document: {documentUri}");
                return new LocationOrLocationLinks(); // Return an empty result if the document content is not available
            }

            var src = _documentContents[(Uri)documentUri].ToString();
            FileLogger.Log("Retrieved document content from in-memory store.");

            #region GetClickedOnWord
            var lines = src.Split("\n");

            if (pos.Line >= lines.Length)
            {
                FileLogger.Log("Line position is out of bounds.");
                
            }

            var line = lines[pos.Line];
            var charIndex = pos.Character;

            if (charIndex > line.Length)
            {
                FileLogger.Log($"Character position is out of bounds. Adjusting to the end of the line.");
                charIndex = line.Length; // Adjust to the last valid character index

            }
            if (string.IsNullOrWhiteSpace(line))
            {
                FileLogger.Log("The line is empty or contains only whitespace.");
                return new LocationOrLocationLinks();
            }

            int start = charIndex;
            int end = charIndex;

            // Move backwards to find the start of the word
            while (start > 0 && !char.IsWhiteSpace(line[start - 1]) && !IsBoundaryChar(line[start - 1]))
            {
                start--;
            }

            // Move forwards to find the end of the word
            while (end < line.Length && !char.IsWhiteSpace(line[end]) && !IsBoundaryChar(line[end]))
            {
                end++;
            }

            var word = line.Substring(start, end - start);
            FileLogger.Log($"Extracted word: {word}");
            #endregion

            _classDecompiler.DecompileByClass(word);

            var code = _classDecompiler.GetDecompiledCode(word);
            FileLogger.Log("Code: \n" + code);

            if (code != null)
            {
                // TODO: get the folder path dynamically on startup
                var decompiledFilePath = $"C:\\Users\\Admin\\source\\repos\\WorldBoxModdingLSP\\WorldBoxModdingToolChain\\Decompiled\\{word}.cs";
                File.WriteAllText(decompiledFilePath, code);

                var decompiledUri = DocumentUri.FromFileSystemPath(decompiledFilePath);
                return new LocationOrLocationLinks(
                        new OmniSharp.Extensions.LanguageServer.Protocol.Models.Location
                        {
                            Uri = decompiledUri,
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                            {
                                Start = new Position(0, 0),
                                End = new Position(0, 0)
                            }
                        }
                    );
            } 
            
            

            return new LocationOrLocationLinks();
        }
        private static bool IsBoundaryChar(char c)
        {
            return char.IsPunctuation(c) || char.IsSymbol(c);
        }
        public DefinitionRegistrationOptions GetRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new DefinitionRegistrationOptions
            {
                DocumentSelector = new TextDocumentSelector(new TextDocumentFilter
                {
                    Pattern = "**/*.cs", // Update with the appropriate pattern or file type
                    Language = "csharp"  // Update with the language you are targeting
                })
            };
        }
    }
}
