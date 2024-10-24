using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WorldBoxModdingToolChain.Analysis;
using WorldBoxModdingToolChain.Utils;

namespace WorldBoxModdingToolChain.Handlers
{
    //TODO: Refactor and optimize, everything looks ugly and runs with great mediocrity, FEX DIS!
    public class CompletionHandler : ICompletionHandler
    {
        private readonly GameCodeMetaDataRender _metaDataRender;
        private readonly IDictionary<Uri, string[]> _documentContents;
        public CompletionHandler(GameCodeMetaDataRender metaDataRender, IDictionary<Uri, string[]> documentContents)
        {
            _metaDataRender = metaDataRender;
            _documentContents = documentContents;
        }

        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new CompletionRegistrationOptions
            {
                TriggerCharacters = new[] { ".", " ", "[" }
            };
        }

        public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {

            var wordsForeDot = GetWordsBeforeDots(request);
            var completionItems = GetClassCompletionItems(GetCurrentTrimmedWord(GetCurrentLine(request)));
            completionItems.AddRange(GetCompletionItemsFromGameCode(wordsForeDot));
           
            
            return Task.FromResult(new CompletionList(completionItems));


        }

        //TODO: Add Scan for word to appear in Dictionary of variables (with Associated Type)

        private List<CompletionItem> GetClassCompletionItems(string prefix)
        {
            var classCompletionItems = new List<CompletionItem>();

            // Get all classes and filter based on the prefix
            var classes = _metaDataRender.GetClasses();
            foreach (var className in classes)
            {
                if (className.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) // Case-insensitive check
                {
                    classCompletionItems.Add(new CompletionItem
                    {
                        Label = className,
                        Kind = CompletionItemKind.Class,
                        InsertText = className
                    });
                }
            }

            return classCompletionItems;
        }

        public List<CompletionItem> GetCompletionItemsFromGameCode(List<string> wordsBeforeDot)
        {
            var fields_and_properties = _metaDataRender.GetFieldsAndProperties();
            var completionItems = new List<CompletionItem>();
            FileLogger.Log("Count: " + wordsBeforeDot.Count);
            if (wordsBeforeDot.Count == 1)
            {

                var targetClass = wordsBeforeDot[0].Trim();
                FileLogger.Log("Word Before Dot: " + targetClass);

                if (fields_and_properties.ContainsKey(targetClass))
                {
                    FileLogger.Log($"Class '{targetClass}' found, fetching members...");
                    var members = fields_and_properties[wordsBeforeDot[0]];

                    foreach (GameClassMetaObject metadata in members)
                    {
                        completionItems.Add
                            (
                                new CompletionItem
                                {
                                    Label = metadata.Name,
                                    Kind = metadata.Kind,
                                    Documentation = $"Type: {metadata.TypeName()}",
                                    InsertText = GetProperInsertText(metadata.Name, metadata.Kind)

                                }
                            );
                    }
                }
                else
                {
                    FileLogger.Log($"Word Before Dot '{targetClass}' was NOT in the list of classes.");
                }     
            }
            else
            {

                var nestedCompletionItems = 
                    GetCompletionItemsFromGameCode
                    (
                        TraceLookupClassGeneration
                        (
                            wordsBeforeDot,
                            fields_and_properties
                        )
                    );
                completionItems.AddRange(nestedCompletionItems);
            }
            
            
            
            return completionItems;
        }


        private List<string> TraceLookupClassGeneration(List<string> wordsBeforeDot, Dictionary<string, List<GameClassMetaObject>> fields_and_properties)
        {
            FileLogger.Log("Starting TraceLookup with words: " + string.Join(", ", wordsBeforeDot));
            while (wordsBeforeDot.Count > 1)
            {
                if (fields_and_properties.ContainsKey(wordsBeforeDot[0]))
                {

                    var members = fields_and_properties[wordsBeforeDot[0]];

                    // Look for the next word in the chain within the current object's members
                    var nextWord = wordsBeforeDot[1];

                    foreach (GameClassMetaObject metadata in members)
                    {
                        if (metadata.Name == nextWord)
                        {
                            
                            wordsBeforeDot[1] = metadata.TypeName().Trim();
                            FileLogger.Log($"Updated '{nextWord}' to its return type '{metadata.TypeName()}'");
                            break;
                        }
                    }
                    wordsBeforeDot.RemoveAt(0);
                }
                else
                {
                    FileLogger.Log($"Class '{wordsBeforeDot[0]}' not found in metadata.");
                    break;
                }

            }
            FileLogger.Log("Last Word: " + wordsBeforeDot[0]);
            return wordsBeforeDot;
        }


        private string GetCurrentLine(CompletionParams request)
        {
            var uri = request.TextDocument.Uri;
            FileLogger.Log("URI: " + uri);

            var pos = request.Position;
            FileLogger.Log("Pos: " + pos);
            if (_documentContents.TryGetValue((Uri)uri, out var lines))
            {
                // Ensure the line number is within range
                if (pos.Line < lines.Length)
                {

                    return lines[pos.Line];

                }
            }
            FileLogger.Log("RETURNED STRING EMPTY (Line)");
            return String.Empty;
        }

        private string GetCurrentTrimmedWord(string line)
        {
            if (line.Contains("["))
            {
                string pattern = @"\[(.*)";
                Match match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    if (line.Contains(']'))
                    {
                        line = match.Groups[1].Value.Trim(']');
                    }
                    else
                    {
                        line = match.Groups[1].Value;
                    }
                    
                }
            }
            //TODO: This fucks up getting return types for functions FIX!
            else if (line.Contains("("))
            {
                string pattern = @"\((.*)";
                Match match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    if (line.Contains(')'))
                    {
                        line = match.Groups[1].Value.Trim(')');
                    }
                    else
                    {
                        line = match.Groups[1].Value;
                    }

                }
            }
            else if (line.Contains("{"))
            {
                string pattern = @"\{(.*)";
                Match match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    if (line.Contains('}'))
                    {
                        line = match.Groups[1].Value.Trim(')');
                    }
                    else
                    {
                        line = match.Groups[1].Value;
                    }

                }
            }
            FileLogger.Log($"Trimmed up current word: {line}"); 
            return line;
            
        }





        private List<string> GetWordsBeforeDots(CompletionParams request)
        {
            var line = GetCurrentLine(request);
            FileLogger.Log("Line: " + line);
            // Get the character index before the position
            var characterIndex = request.Position.Character;

            var words = new List<string>();

            if (characterIndex <=0) return words;

            var textUpToCurser = line.Substring(0, characterIndex);
            FileLogger.Log($"Text up to cursor: {textUpToCurser}");
            var segments = textUpToCurser.Split('.');
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i].Trim();

                if (!string.IsNullOrEmpty(segment))
                {
                    var trimmedWord = GetCurrentTrimmedWord(segment);
                    //FileLogger.Log("(GetWords) Word: " + line);
                    words.Add(trimmedWord);
                }
                    
            }
            FileLogger.Log($"Found words before dots: {string.Join(", ", words)}");
            return words;
        }


        private string ExtractWordBeforeDot(string line, int characterIndex)
        {
            // Look backwards in the line to find the start of the word
            var startIndex = characterIndex - 1;
            
            

            while (startIndex > 0 && !char.IsWhiteSpace(line[startIndex - 1]) && line[startIndex - 1] != '.')
            {
                startIndex--;
                FileLogger.Log($"StartIndex: {startIndex} Line: {line}");
            }
            
            // Return the word found before the dot
            return line.Substring(startIndex + (char.IsWhiteSpace(line[startIndex]) ? 1 : 0), characterIndex - startIndex - 1).Trim();
        }

        private bool IsDelimiter(char c)
        {
            return char.IsWhiteSpace(c) || c == '.' || c == '[' || c == ']' || c == '(' || c == ')';
        }
        public string GetProperInsertText(string name, CompletionItemKind kind)
        {
            return name + (kind == CompletionItemKind.Method ? "()" : "");
        }

    }
}
