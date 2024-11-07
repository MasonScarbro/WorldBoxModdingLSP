using Microsoft.CodeAnalysis.Diagnostics;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldBoxModdingToolChain.Utils;

namespace WorldBoxModdingToolChain.Analysis
{
    public class AnalysisStorage
    {
        public Dictionary<Uri, Dictionary<string, string>> VariableDictionary { get; } = new();
        public Uri CurrentDocument { get; set; }
        public void UpdateVariables(Uri uri, Dictionary<string, string> variables)
        {
            VariableDictionary[uri] = variables;
            CurrentDocument = uri;
            LogAnalysis(uri, variables);

        }

        public Dictionary<string, string> GetVariables(Uri uri)
        {
            VariableDictionary.TryGetValue(uri, out var variables);
            return variables ?? new Dictionary<string, string>();
        }

        //For now we will just get all current document variables
        public Dictionary<string, string> GetCurrentDocumentVariables()
        {
            VariableDictionary.TryGetValue(CurrentDocument, out var variables);
            return variables ?? new Dictionary<string, string>();
        }

        private void LogAnalysis(Uri uri, Dictionary<string, string> analysisResults)
        {
            foreach (var entry in analysisResults)
            {
                FileLogger.Log($"File: {uri}, Variable: {entry.Key}, Type: {entry.Value}");
            }
        }
    }
}
