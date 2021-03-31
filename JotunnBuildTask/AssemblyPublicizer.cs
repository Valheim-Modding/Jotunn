using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace JotunnBuildTask
{
    public static class AssemblyPublicizer
    {
        /// <summary>
        /// Publicize a dll
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool PublicizeDll(string file, string ValheimPath)
        {
            string outputPath = Path.Combine(Path.GetDirectoryName(file), "publicized_assemblies");

            if (!File.Exists(file))
            {
                Console.WriteLine($"File {file} not found.");
                return false;
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            AssemblyDefinition assemblyDefinition;

            try
            {
                assemblyDefinition = AssemblyDefinition.ReadAssembly(file);
                ((BaseAssemblyResolver)assemblyDefinition.MainModule.AssemblyResolver).AddSearchDirectory(Path.Combine(ValheimPath, "valheim_Data", "Managed"));
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{exception.Message}");
                return false;
            }

            var types = GetTypeDefinitions(assemblyDefinition.MainModule);
            var methods = types.SelectMany(x => x.Methods).Where(x => x.IsPublic == false);
            var fields = types.SelectMany(x => x.Fields).Where(x => x.IsPublic == false);


            foreach (var type in types)
            {
                if (!type.IsPublic && !type.IsNestedPublic)
                {
                    if (type.IsNested)
                    {
                        type.IsNestedPublic = true;
                    }
                    else
                    {
                        type.IsPublic = true;
                    }
                }
            }


            foreach (var method in methods)
            {
                method.IsPublic = true;
            }

            foreach (var field in fields)
            {
                field.IsPublic = true;
            }


            string outputFilename = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(file)}_publicized{Path.GetExtension(file)}");

            try
            {
                assemblyDefinition.Write(outputFilename);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Could not write file {outputFilename}.");
                Console.WriteLine(exception.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns all type definitions for the module
        /// </summary>
        /// <param name="moduleDefinition"></param>
        /// <returns></returns>
        private static IEnumerable<TypeDefinition> GetTypeDefinitions(ModuleDefinition moduleDefinition)
        {
            return GetTypeDefinitionsRecursive(moduleDefinition.Types);
        }

        /// <summary>
        /// Get all type definitions recursive
        /// </summary>
        /// <param name="typeDefinitions"></param>
        /// <returns></returns>
        private static IEnumerable<TypeDefinition> GetTypeDefinitionsRecursive(IEnumerable<TypeDefinition> typeDefinitions)
        {
            if (typeDefinitions?.Count() == 0)
            {
                return new List<TypeDefinition>();
            }

            return typeDefinitions.Concat(GetTypeDefinitionsRecursive(typeDefinitions.SelectMany(x => x.NestedTypes)));
        }
    }
}