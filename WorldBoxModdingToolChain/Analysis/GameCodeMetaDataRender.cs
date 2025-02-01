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
        private List<string> StaticClasses = new List<string>();
        private Dictionary<string, List<GameClassMetaObject>> InstanceCreatableClassesMetaData = new Dictionary<string, List<GameClassMetaObject>>(StringComparer.OrdinalIgnoreCase);

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
                    if (type.IsAbstract && type.IsSealed) // static classes are abstract and sealed
                    {
                        StaticClasses.Add(type.Name);
                        continue;
                    }


                    var fields = type.Fields.Select(f => 
                    {
                        return new GameClassMetaObject(
                            f.Name,
                            f.FieldType,
                            CompletionItemKind.Field,
                            isStatic: f.IsStatic
                        );
                    }).ToList();

                    var properties = type.Properties.Select(p =>
                    {
                        return new GameClassMetaObject(
                            p.Name,
                            null,
                            CompletionItemKind.Property
                        );
                    }).ToList();

                    var methods = type.Methods.Select(m =>
                    {
                        var parameters = string.Join(
                            ", ",
                            m.Parameters.Select(p => $"{p.Name}: {p.ParameterType.FullName}")
                        );
                        return new GameClassMetaObject(
                            m.Name,
                            m.ReturnType,
                            CompletionItemKind.Method,
                            parameters: parameters
                        );
                    }).ToList();

                    //FileLogger.Log($"Loading type: {type.Name}");
                    var fieldsPropertiesMethods = fields.Concat(properties).Concat(methods).ToList();


                    ClassesMetaData[type.Name] = fieldsPropertiesMethods;

                    var instanceFields = type.Fields
                        .Where(f => !f.IsStatic && f.IsPublic) // Non-static and public fields
                        .Select(f => new GameClassMetaObject(f.Name, f.FieldType, CompletionItemKind.Field))
                        .ToList();

                    var instanceProperties = type.Properties
                        .Where(p => p.GetMethod != null && !p.GetMethod.IsStatic && p.GetMethod.IsPublic) // Non-static and public properties
                        .Select(p => new GameClassMetaObject(p.Name, null, CompletionItemKind.Property))
                        .ToList();

                    if (instanceFields.Any() || instanceProperties.Any())
                    {
                        InstanceCreatableClassesMetaData[type.Name] = instanceFields.Concat(instanceProperties).ToList();
                    }

                    //ProcessDecompiledCode(type, decompiler);

                }
            }
            
            
        }

        

        public Dictionary<string, List<GameClassMetaObject>> GetFieldsAndProperties() => ClassesMetaData;
        public List<string> GetClasses() => ClassesMetaData.Keys.ToList();

        public List<string> GetStaticClasses() => StaticClasses;

        public Dictionary<string, List<GameClassMetaObject>> GetInstanceCreatableClasses() => InstanceCreatableClassesMetaData;

       
        
    }
}
