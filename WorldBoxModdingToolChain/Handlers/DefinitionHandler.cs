﻿using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
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
using System.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WorldBoxModdingToolChain.Handlers
{
    public class DefinitionHandler : IDefinitionHandler
    {

        private readonly ClassDecompiler _classDecompiler;
        private readonly IDictionary<Uri, SourceText> _documentContents;
        private readonly PathLibrary _pathLibrary;
        private readonly DocumentParserService _documentParserService;
        public DefinitionHandler(ClassDecompiler classDecompiler, IDictionary<Uri, SourceText> documentContents, PathLibrary pathLibrary, DocumentParserService documentParserService) 
        { 
            _classDecompiler = classDecompiler;
            _documentContents = documentContents;
            _pathLibrary = pathLibrary;
            _documentParserService = documentParserService;
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

            FileLogger.Log("Line: " + line);
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
            return GetLocationFromWord(word);
        }

        private LocationOrLocationLinks GetLocationFromWord(string word)
        {
            
            //if its trying to access we need to do analyze it
            if (word.Contains('.'))
            {
                var accessors = word.Split('.');
                //if its only length two just find it in the class code
                if (accessors.Length == 2)
                {
                    var classWord = accessors[0];
                    
                    _classDecompiler.DecompileByClass(classWord);
                    
                    var classCode = _classDecompiler.GetDecompiledCode(classWord);
                    var syntaxTree = _documentParserService.GetOrParseSyntaxTree(new Uri($"{_pathLibrary.decompiledPath}/{classWord}.cs"), classCode);
                    var root = _documentParserService.GetRootNode(syntaxTree);
                    var text = _documentParserService.GetText(syntaxTree);

                    foreach (var line in text.Lines)
                    {
                        
                        if (line.Text.ToString().Contains(accessors[1]))
                        {

                            
                            var node = root.DescendantNodes()
                            .FirstOrDefault(n =>
                                (n is VariableDeclaratorSyntax varDecl && varDecl.Identifier.Text == accessors[1]) ||
                                (n is MethodDeclarationSyntax methodDecl && methodDecl.Identifier.Text == accessors[1])
                            );
                            if (node == null)
                            {
                                FileLogger.Log($"Could not find exact node for '{accessors[1]}'");
                                continue;
                            }
                            var location = node.GetLocation().GetLineSpan();
                            var range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                location.StartLinePosition.Line, location.StartLinePosition.Character,
                                location.EndLinePosition.Line, location.EndLinePosition.Character
                            );
                            //FileLogger.Log($"Found '{accessors[1]}' at Range: {range}");
                            return GetAppropiateLocation(classCode, classWord, range);
                        }
                        
                    }
                  
                    
                }
                //TODO: Trace Foward to find refernce in class
                if (accessors.Length > 2)
                {

                }
            }
            //else
            _classDecompiler.DecompileByClass(word);
            var code = _classDecompiler.GetDecompiledCode(word);
            if (code == string.Empty) return new LocationOrLocationLinks();
            return GetAppropiateLocation(code, word, new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, 0 ,0));

        }

        private LocationOrLocationLinks GetAppropiateLocation(string code, string word, OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range)
        {
            if (code != null)
            {
                
                var decompiledFilePath = $"{_pathLibrary.decompiledPath}/{word}.cs";
                File.WriteAllText(decompiledFilePath, code);

                FileLogger.Log(_pathLibrary.decompiledPath + "Decompiled Folder path");

                var decompiledUri = DocumentUri.FromFileSystemPath(decompiledFilePath);

                return new LocationOrLocationLinks(
                        new OmniSharp.Extensions.LanguageServer.Protocol.Models.Location
                        {
                            Uri = decompiledUri,
                            Range = range,
                        }
                    );
            }
            return new LocationOrLocationLinks();
        }

        private static bool IsBoundaryChar(char c)
        {
            // Excluding periods and underscores
            return (char.IsPunctuation(c) && c != '.' && c != '_') || char.IsSymbol(c);
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
