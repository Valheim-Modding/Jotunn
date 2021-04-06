using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace JotunnBuildTask
{
    public static class AssemblyPublicizer
    {
        /// <summary>
        ///     Publicize a dll
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool PublicizeDll(string file, string ValheimPath)
        {
            var outputPath = Path.Combine(Path.GetDirectoryName(file), Program.PublicizedAssemblies);

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
                if (Directory.Exists(Path.Combine(ValheimPath, Program.ValheimData,
                    Program.Managed)))
                {
                    ((BaseAssemblyResolver)assemblyDefinition.MainModule.AssemblyResolver).AddSearchDirectory(Path.Combine(ValheimPath, Program.ValheimData,
                      Program.Managed));
                }
                if (Directory.Exists(Path.Combine(ValheimPath, Program.ValheimServerData,
                    Program.Managed)))
                {
                    ((BaseAssemblyResolver)assemblyDefinition.MainModule.AssemblyResolver).AddSearchDirectory(Path.Combine(ValheimPath, Program.ValheimServerData,
                        Program.Managed));
                }

                ((BaseAssemblyResolver)assemblyDefinition.MainModule.AssemblyResolver).AddSearchDirectory(Path.Combine(ValheimPath, Program.UnstrippedCorlib));
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{exception.Message}");
                return false;
            }

            // Get all type definitions
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


            var outputFilename = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(file)}_{Program.Publicized}{Path.GetExtension(file)}");

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
        ///     Returns all type definitions for the module
        /// </summary>
        /// <param name="moduleDefinition"></param>
        /// <returns></returns>
        private static IEnumerable<TypeDefinition> GetTypeDefinitions(ModuleDefinition moduleDefinition)
        {
            return GetTypeDefinitionsRecursive(moduleDefinition.Types);
        }

        /// <summary>
        ///     Get all type definitions recursive
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