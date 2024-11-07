using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private readonly AnalysisStorage _analysisStorage;
        public CompletionHandler(GameCodeMetaDataRender metaDataRender, IDictionary<Uri, string[]> documentContents, AnalysisStorage analysisStorage)
        {
            _metaDataRender = metaDataRender;
            _documentContents = documentContents;
            _analysisStorage = analysisStorage;
        }

        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new CompletionRegistrationOptions
            {
                TriggerCharacters = new[] { ".", " ", "[", "new" }
            };
        }

        public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var line = GetCurrentLine(request);
            var syntaxTree = CSharpSyntaxTree.ParseText(line);
            var root = syntaxTree.GetRoot();

            var position = request.Position.Character;
            var tokenAtCursor = root.FindToken(position);

            var completionItems = new List<CompletionItem>();
            List<string> memberNames = new List<string>();
            var fields_and_properties = _metaDataRender.GetFieldsAndProperties();

            if (tokenAtCursor.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                while (memberAccess != null)
                {
                    // Add the right side of the member access (e.g., "foo" or "bar")
                    memberNames.Insert(0, memberAccess.Name.Identifier.Text);

                    // Move to the left side of the member access (could be another MemberAccessExpression or Identifier)
                    if (memberAccess.Expression is MemberAccessExpressionSyntax nestedAccess)
                    {
                        FileLogger.Log("Is Nested Access");
                        memberAccess = nestedAccess;
                    }
                    else if (memberAccess.Expression is IdentifierNameSyntax identifier)
                    {
                        // Reached the root of the chain (e.g., "b")
                        FileLogger.Log("Found Root: " + identifier.Identifier.Text);
                        memberNames.Insert(0, identifier.Identifier.Text);
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                memberNames = memberNames.Where(name => !string.IsNullOrWhiteSpace(name)).ToList();

                FileLogger.Log("Filtered MemberNames Before TraceLookup: " + string.Join(", ", memberNames));
                var correctedMemberNames = TraceLookupClassGeneration
                        (
                            memberNames,
                            fields_and_properties
                        );

                RafactorToVariableTypes(correctedMemberNames);

                FileLogger.Log("Corrected MemberNames: " + string.Join(", ", correctedMemberNames));
                var targetClass = correctedMemberNames[0].Trim();

                

                if (fields_and_properties.ContainsKey(targetClass))
                {
                    FileLogger.Log($"Class '{targetClass}' found, fetching members...");
                    var members = fields_and_properties[targetClass];

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
            
            FileLogger.Log("Dummy Handle Result: " + string.Join(", ", memberNames));

            completionItems.AddRange(
                GetClassCompletionItems(
                    GetCurrentTrimmedWord(
                        GetCurrentLine(request)
                        )
                    )
                );
            //completionItems.AddRange(GetCompletionItemsFromGameCode(memberNames));
            FileLogger.Log("Returning completion items: " + completionItems.Count);

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


        private List<string> TraceLookupClassGeneration(List<string> wordsBeforeDot, Dictionary<string, List<GameClassMetaObject>> fields_and_properties)
        {
            FileLogger.Log("Starting TraceLookup with words: " + string.Join(", ", wordsBeforeDot));
            while (wordsBeforeDot.Count > 1)
            {
                FileLogger.Log($"We are inside the while with count: {wordsBeforeDot.Count}");

                RafactorToVariableTypes(wordsBeforeDot);

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
            string pattern = @"new\s+(.*)";
            Match match = Regex.Match(line, pattern);
            match = Regex.Match(line, pattern);
            if (match.Success)
            {
                    line = match.Groups[1].Value.Trim();
            }
            


            FileLogger.Log($"Trimmed up current word: {line}"); 
            return line.Trim();
            
        }

        private void RafactorToVariableTypes(List<string> correctedMemberNames)
        {
            if (_analysisStorage.GetCurrentDocumentVariables().TryGetValue(correctedMemberNames[0], out string variableType))
            {
                FileLogger.Log($"Changed {correctedMemberNames[0]} to {variableType}");
                //if the type is found simply change the word to the type so we can
                //check the class
                correctedMemberNames[0] = variableType;

            }
        }
        private bool IsDelimiter(char c)
        {
            return char.IsWhiteSpace(c) || c == '.' || c == '[' || c == ']' || c == '(' || c == ')';
        }
        public string GetProperInsertText(string name, CompletionItemKind kind)
        {
            return name + (kind == CompletionItemKind.Method ? "()" : "");
        }

        #region Potential
        //public List<CompletionItem> GetCompletionItemsFromGameCode(List<string> wordsBeforeDot)
        //{
        //    var fields_and_properties = _metaDataRender.GetFieldsAndProperties();
        //    var completionItems = new List<CompletionItem>();
        //    FileLogger.Log("Count: " + wordsBeforeDot.Count);
        //    if (wordsBeforeDot.Count == 1)
        //    {

        //        var targetClass = wordsBeforeDot[0].Trim();

        //        if (_analysisStorage.GetCurrentDocumentVariables().TryGetValue(wordsBeforeDot[0], out string variableType))
        //        {
        //            FileLogger.Log($"Changed {targetClass} to {variableType}");
        //            //if the type is found simply change the word to the type so we can
        //            //check the class
        //            targetClass = variableType;

        //        }

        //        FileLogger.Log("Word Before Dot: " + targetClass);

        //        if (fields_and_properties.ContainsKey(targetClass))
        //        {
        //            FileLogger.Log($"Class '{targetClass}' found, fetching members...");
        //            var members = fields_and_properties[targetClass];

        //            foreach (GameClassMetaObject metadata in members)
        //            {
        //                completionItems.Add
        //                    (
        //                        new CompletionItem
        //                        {
        //                            Label = metadata.Name,
        //                            Kind = metadata.Kind,
        //                            Documentation = $"Type: {metadata.TypeName()}",
        //                            InsertText = GetProperInsertText(metadata.Name, metadata.Kind)

        //                        }
        //                    );
        //            }
        //        }
        //        else
        //        {
        //            FileLogger.Log($"Word Before Dot '{targetClass}' was NOT in the list of classes.");
        //        }     
        //    }
        //    else  if ( wordsBeforeDot.Count > 1 )
        //    {

        //        var nestedCompletionItems = 
        //            GetCompletionItemsFromGameCode
        //            (
        //                TraceLookupClassGeneration
        //                (
        //                    wordsBeforeDot,
        //                    fields_and_properties
        //                )
        //            );
        //        completionItems.AddRange(nestedCompletionItems);
        //    }



        //    return completionItems;
        //}
        #endregion
    }
}
