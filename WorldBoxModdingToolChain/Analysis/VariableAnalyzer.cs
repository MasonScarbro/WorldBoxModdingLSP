using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using WorldBoxModdingToolChain.Utils;


namespace WorldBoxModdingToolChain.Analysis
{
    public class VariableAnalyzer
    {
       
        public static List<string> VariableNames(string code) => AnalyzeVariables(code).Values.ToList();

        public static List<string> TypeNames(string code) => AnalyzeVariables(code).Keys.ToList();

        public static Dictionary<string, string> GetVariableDictionary(string code) => AnalyzeVariables(code);

        private static Dictionary<string, string> AnalyzeVariables(string code)
        {
            
            var variableTypes = new Dictionary<string, string>();


            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(("C:\\Program Files (x86)\\Steam\\steamapps\\common\\worldbox\\worldbox_Data\\Managed\\Assembly-CSharp.dll")) // Add your assembly here
            };

            var compilation = CSharpCompilation.Create("Analysis")
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTree);

            var model = compilation.GetSemanticModel(syntaxTree);

            var root = syntaxTree.GetRoot();

            // Identify variable declarations
            var declarators = root.DescendantNodes().OfType<VariableDeclarationSyntax>();
            var nodes = root.DescendantNodes();

            foreach ( var declarator in declarators)
            {
                foreach (var variable in declarator.Variables)
                {
                    var symbol = model.GetDeclaredSymbol(variable) as ILocalSymbol;

                    if (symbol != null)
                    {
                        variableTypes[symbol.Name] = symbol.Type.ToString();
                        FileLogger.Log($"Variable: {symbol.Name}, Type: {symbol.Type}");
                    }
                }
                
            }

            var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();

            foreach ( var assignment in assignments)
            {
                var left = assignment.Left;
                var symbol = model.GetSymbolInfo(left).Symbol as ILocalSymbol;

                if ( symbol != null )
                {
                    // If variable is assigned and not yet in the dictionary, add it
                    variableTypes[symbol.Name] = symbol.Type.ToString();
                    FileLogger.Log($"Variable: {symbol.Name}, Type: {symbol.Type}");
                }
            }
            return variableTypes;
        }

    }
}
