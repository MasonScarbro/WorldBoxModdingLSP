using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using WorldBoxModdingToolChain.Utils;


namespace WorldBoxModdingToolChain.Analysis
{
    public class GameCodeMetaDataRender
    {
        private Dictionary<string, List<string>> classFieldsAndProperties = new Dictionary<string, List<string>>();

        public GameCodeMetaDataRender(string asmPath)
        {
            LoadGameAssembly(asmPath);
        }

        private void LoadGameAssembly(string asmPath)
        {
            var asm = AssemblyDefinition.ReadAssembly(asmPath);

            foreach (var module in asm.Modules)
            {
                //FileLogger.Log($"Loading Module: {module.Name}");
                foreach (var type in module.Types)
                {
                    var fields = type.Fields.Select(f => f.Name).ToList();
                    var properties = type.Properties.Select(p => p.Name).ToList();
                    //FileLogger.Log($"Loading type: {type.Name}");
                    var fieldsAndProperties = fields.Concat(properties).ToList();

                    classFieldsAndProperties[type.Name] = fieldsAndProperties;
                }
            }

        }

        public List<string> GetCompletions(string className)
        {
            if (classFieldsAndProperties.ContainsKey(className))
            {
                return classFieldsAndProperties[className];
            }

            //else we got NOTINGGGGGGG
            return new List<string>();
        }

    }
}
