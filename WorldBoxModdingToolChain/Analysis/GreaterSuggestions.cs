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
        public Dictionary<string, Func<string?, string>> GreaterSuggestionsDict = new Dictionary<string, Func<string?, string>>
        {
            { 
                "$Traits",
                (param) => Constants.TraitsBoilerplate + (param ?? string.Empty)
            },
            {
                "$Effects",
                (param) => Constants.EffectsBoilerplate + (param ?? string.Empty)
            },
            {
                "$Statuses",
                (param) => Constants.StatusesBoilerplate + (param ?? string.Empty)
            },
            {
                "$Units",
                (param) => Constants.UnitsBoilerplate + (param ?? string.Empty)
            },
            {
                "$NewTrait",
                (param) => GetNewTraitCode(param)

            },
        };

        public CompletionItem GetBoilerplate(string command, string appendage="")
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
                    InsertText = GreaterSuggestionsDict[associatedKey](appendage)

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

        public static string GetNewTraitCode(string variable)
        {
            FileLogger.Log($"New {variable} (INSIDE GETNEWTRAITCODE)");
            if (variable == string.Empty) return "";
            return $"new ActorTrait();\r\n" +
                   $"{variable}.id = \"{variable}\";\r\n" +
                   $"{variable}.path_icon = \"ui/icons/blessed\";\r\n" +
                   $"{variable}.base_stats[S.damage] += 20f;\r\n" +
                   $"{variable}.base_stats[S.health] += 550;\r\n" +
                   $"{variable}.base_stats[S.attack_speed] += 3f;\r\n" +
                   $"{variable}.base_stats[S.critical_chance] += 0.25f;\r\n" +
                   $"{variable}.base_stats[S.scale] = 0.02f;\r\n" +
                   $"{variable}.base_stats[S.dodge] += 3f;\r\n" +
                   $"{variable}.base_stats[S.range] += 6f;\r\n" +
                   $"//{variable}.action_attack_target = new AttackAction({variable}Attack);\r\n" +
                   $"//{variable}.action_death = (WorldAction)Delegate.Combine({variable}.action_death, new WorldAction({variable}sDeath));\r\n" +
                   $"//{variable}.action_special_effect = (WorldAction)Delegate.Combine({variable}.action_special_effect, new WorldAction({variable}EraStatus));\r\n" +
                   $"AssetManager.traits.add({variable});\r\n" +
                   $"PlayerConfig.unlockTrait({variable}.id);\r\n" +
                   $"addTraitToLocalizedLibrary({variable}.id, \"Fill this out with a description of the trait\");";
        }
    }
}
