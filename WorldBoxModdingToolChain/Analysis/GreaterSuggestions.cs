using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldBoxModdingToolChain.Utils;

namespace WorldBoxModdingToolChain.Analysis
{
    public class GreaterSuggestions
    {
        public Dictionary<string, string> GreaterSuggestionsDict = new Dictionary<string, string>
        {
            { 
                "$Traits",
                Constants.TraitsBoilerplate
            },
            {
                "$Effects",
                Constants.EffectsBoilerplate
            },
            {
                "$Statuses",
                Constants.StatusesBoilerplate
            },
            {
                "$Units",
                Constants.UnitsBoilerplate
            }
        };

        public CompletionItem GetBoilerplate(string command)
        {
            FileLogger.Log("Inside BoilerPlate and command is " + command);
            if (MostCharactersAreInKey(command, out string associatedKey))
            {
                return new CompletionItem
                {
                    Label = associatedKey,
                    Kind = CompletionItemKind.Keyword,
                    Documentation = "Boiler Plate code for the " + associatedKey,
                    InsertTextFormat = InsertTextFormat.Snippet,
                    InsertText = GreaterSuggestionsDict[associatedKey]

                };
            }
            //else
            return new CompletionItem();
            
        }
        
        public bool MostCharactersAreInKey(string word, out string closestKey)
        {
            
            if (GreaterSuggestionsDict.ContainsKey(word))
            {
                closestKey = word;
                return true;
            }
            // Get the dictionary keys
            var dictKeys = GreaterSuggestionsDict.Keys;
            int maxMatchedCount = 0;
            closestKey = null;

            // Check the number of matched characters for each key
            foreach (var key in dictKeys)
            {
                FileLogger.Log("Matched Count " + maxMatchedCount);
                int matchedCharacterCount = word.Count(c => key.Contains(c));

                // If more characters match than the previous highest match, update the closestKey
                if (matchedCharacterCount > maxMatchedCount)
                {
                    maxMatchedCount = matchedCharacterCount;
                    closestKey = key;
                }
            }
            FileLogger.Log("Closest Key " + closestKey);
            // Return true if we found a key with matching characters, otherwise false
            return maxMatchedCount > word.Length / 2;
        }
    }
}
