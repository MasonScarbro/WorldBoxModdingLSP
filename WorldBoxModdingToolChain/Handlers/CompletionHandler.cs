﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;
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
        private readonly IDictionary<Uri, SourceText> _documentContents;
        private readonly AnalysisStorage _analysisStorage;
        public CompletionHandler(GameCodeMetaDataRender metaDataRender, IDictionary<Uri, SourceText> documentContents, AnalysisStorage analysisStorage)
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
            var documentLines = _documentContents[(Uri)request.TextDocument.Uri];



            var documentText = string.Join(Environment.NewLine, documentLines);
            FileLogger.Log("Docuemnt text: " + documentText);

            var syntaxTree = CSharpSyntaxTree.ParseText(documentText);
            var root = syntaxTree.GetRoot();

            var text = syntaxTree.GetText();





            var absolutePosition = text.Lines.GetPosition(new Microsoft.CodeAnalysis.Text.LinePosition(request.Position.Line, request.Position.Character));
            FileLogger.Log("Absolute Position: " + absolutePosition);

            var tokenAtCursor = root.FindToken(absolutePosition);
            FileLogger.Log("Token: " + tokenAtCursor);

            var completionItems = new List<CompletionItem>();
            List<string> memberNames = new List<string>();
            var fields_and_properties = _metaDataRender.GetFieldsAndProperties();
            var instanceClassesAndProperties = _metaDataRender.GetInstanceCreatableClasses();

            
            //handles member accessing generation 
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
                                    Documentation = metadata.Documentation,
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
            //handles inline object creation ex: new SomeObj {} and gets the fields
            if (IsObjectCreation(tokenAtCursor))
            {
                var objectCreation = tokenAtCursor.Parent.AncestorsAndSelf()
                .OfType<ObjectCreationExpressionSyntax>()
                .FirstOrDefault();


                if (objectCreation != null)
                {
                    // Check if the cursor is within the initializer span
                    if (IsWithinBrace(objectCreation, absolutePosition))
                    {
                        FileLogger.Log("Cursor inside ObjectCreationExpressionSyntax initializer.");
                        var objectType = objectCreation.Type;
                        if (objectType != null)
                        {
                            // You can get the name of the object type
                            var objectName = objectType.ToString(); // This will give the full type name
                            FileLogger.Log($"Object type being created: {objectName}");
                            if (instanceClassesAndProperties.ContainsKey(objectName))
                            {
                                FileLogger.Log($"Class '{objectName}' found, fetching members...");
                                var members = instanceClassesAndProperties[objectName];

                                foreach (GameClassMetaObject metadata in members)
                                {
                                    completionItems.Add
                                        (
                                            new CompletionItem
                                            {
                                                Label = metadata.Name,
                                                Kind = metadata.Kind,
                                                Documentation = metadata.Documentation,
                                                Detail = metadata.ToString(),
                                                InsertText = GetProperInsertText(metadata.Name, metadata.Kind)

                                            }
                                        );
                                }
                            }
                        }
                    }
                    else
                    {
                        completionItems.AddRange(
                        GetClassCompletionItems(
                            GetCurrentTrimmedWord(
                                GetCurrentLine(request)
                                )
                            )
                        );
                    }
                }
                else
                {
                    FileLogger.Log("No ObjectCreationExpressionSyntax found at cursor position.");
                }
            }
            else
            {
                completionItems.AddRange(
                GetClassCompletionItems(
                    GetCurrentTrimmedWord(
                        GetCurrentLine(request)
                        )
                    )
                );
            }
            
            
            
            FileLogger.Log("Dummy Handle Result: " + string.Join(", ", memberNames));

            
            //completionItems.AddRange(GetCompletionItemsFromGameCode(memberNames));
            FileLogger.Log("Returning completion items: " + completionItems.Count);

            return Task.FromResult(new CompletionList(completionItems));


        }

        

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
            if (_documentContents.TryGetValue((Uri)uri, out var src))
            {
                var lines = src.Lines;
                // Ensure the line number is within range
                if (pos.Line < lines.Count)
                {

                    return lines[pos.Line].ToString();

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

        #region Truthy&Falsey

        public bool IsObjectCreation(SyntaxToken tokenAtCursor)
        {
            return tokenAtCursor.Parent is ObjectCreationExpressionSyntax || tokenAtCursor.Parent.Ancestors().OfType<ObjectCreationExpressionSyntax>().Any();
        }

        public bool IsWithinBrace(ObjectCreationExpressionSyntax objectCreation, int absolutePosition)
        {
            return objectCreation.Initializer != null &&
                        absolutePosition >= objectCreation.Initializer.SpanStart &&
                        absolutePosition <= objectCreation.Initializer.Span.End;
        }

        #endregion

    }
}
