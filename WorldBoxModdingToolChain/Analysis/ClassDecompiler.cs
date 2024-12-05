using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldBoxModdingToolChain.Utils;

namespace WorldBoxModdingToolChain.Analysis
{
    public class ClassDecompiler
    {
        string _asmPath;
        public ClassDecompiler(string asmPath)
        {
            _asmPath = asmPath;
        }

        private Dictionary<string, string> DecompiledClasses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private void ProcessDecompiledCode(TypeDefinition type, CSharpDecompiler decompiler)
        {
            var typeName = new FullTypeName(type.FullName);
            string decompiledCode = decompiler.DecompileTypeAsString(typeName);
            //FileLogger.Log(decompiledCode);
            DecompiledClasses[type.FullName] = decompiledCode;

        }

        public void DecompileByClass(string className)
        {

            var asm = AssemblyDefinition.ReadAssembly(_asmPath);
            var decompiler = new CSharpDecompiler(_asmPath, new ICSharpCode.Decompiler.DecompilerSettings());
            var type = asm.MainModule.Types.FirstOrDefault(t => t.FullName.Equals(className, StringComparison.OrdinalIgnoreCase));

            if (type != null)
            {
                // If the class is found, decompile it
                ProcessDecompiledCode(type, decompiler);
            }
            else
            {
                // If the class is not found, log it
                FileLogger.Log($"Class '{className}' not found in the assembly.");
            }

        }

        public string GetDecompiledCode(string className)
        {
            if (DecompiledClasses.ContainsKey(className))
            {
                return DecompiledClasses[className];
            }
            //else
            return "";
            
        }
    }
}
