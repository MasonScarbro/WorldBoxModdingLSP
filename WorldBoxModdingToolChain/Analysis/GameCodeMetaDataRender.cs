using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using WorldBoxModdingToolChain.Utils;


namespace WorldBoxModdingToolChain.Analysis
{
    public class GameCodeMetaDataRender
    {
        private Dictionary<string, List<GameClassMetaObject>> ClassesMetaData = new Dictionary<string, List<GameClassMetaObject>>(StringComparer.OrdinalIgnoreCase);


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
                    var fields = type.Fields.Select(f => (new GameClassMetaObject(f.Name, f.FieldType, CompletionItemKind.Field))).ToList();
                    var properties = type.Properties.Select(p => (new GameClassMetaObject(p.Name, null ,CompletionItemKind.Property))).ToList();
                    var methods = type.Methods.Select(m => (new GameClassMetaObject(m.Name, m.ReturnType, CompletionItemKind.Method))).ToList();

                    //FileLogger.Log($"Loading type: {type.Name}");
                    var fieldsPropertiesMethods = fields.Concat(properties).Concat(methods).ToList();


                    ClassesMetaData[type.Name] = fieldsPropertiesMethods;
                }
            }
            //TEST
            //foreach (string klass in ClassesMetaData.Keys)
            //{
            //    for (int i = 0; i < ClassesMetaData[klass].Count; i++)
            //    {
            //        FileLogger.Log("Meta Data for" + klass + ClassesMetaData[klass][i].ToString());
            //    }
                
            //}
        }
        
        public Dictionary<string, List<GameClassMetaObject>> GetFieldsAndProperties() => ClassesMetaData;
        public List<string> GetClasses() => ClassesMetaData.Keys.ToList();

    }
}
