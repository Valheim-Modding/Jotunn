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
        /// <param name="input">Input assembly</param>
        /// <returns></returns>
        public static bool PublicizeDll(string input, string hash, string publicizedFolder, string ValheimPath)
        {
            if (!File.Exists(input))
            {
                System.Console.WriteLine($"File {input} not found.");
                return false;
            }

            AssemblyDefinition assemblyDefinition;

            try
            {
                System.Console.WriteLine($"Publicizing {input}.");

                assemblyDefinition = AssemblyDefinition.ReadAssembly(input);
                BaseAssemblyResolver assemblyResolver = (BaseAssemblyResolver)assemblyDefinition.MainModule.AssemblyResolver;

                string managedFolder = Path.Combine(ValheimPath, JotunnBuildTask.ValheimData, JotunnBuildTask.Managed);
                string serverManagedFolder = Path.Combine(ValheimPath, JotunnBuildTask.ValheimServerData, JotunnBuildTask.Managed);

                if (Directory.Exists(managedFolder))
                {
                    assemblyResolver.AddSearchDirectory(managedFolder);
                }

                if (Directory.Exists(serverManagedFolder))
                {
                    assemblyResolver.AddSearchDirectory(serverManagedFolder);
                }
            }
            catch (Exception exception)
            {
                System.Console.WriteLine($"{exception.Message}");
                return false;
            }

            // Get all type definitions
            var types = GetTypeDefinitions(assemblyDefinition.MainModule);

            var methods = types.SelectMany(x => x.Methods).Where(x => x.IsPublic == false);
            var fields = types.SelectMany(x => x.Fields).Where(x => x.IsPublic == false);
            var events = types.SelectMany(x => x.Events);

            foreach (var type in types)
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

            foreach (var method in methods)
            {
                method.IsPublic = true;
            }

            List<string> eventNames = new List<string>();
            foreach (var ev in events)
            {
                eventNames.Add(ev.Name);
            }

            foreach (var field in fields)
            {
                if (!eventNames.Contains(field.Name))
                {
                    field.IsPublic = true;
                }
            }

            assemblyDefinition.MainModule.Assembly.CustomAttributes.Add(new CustomAttribute(assemblyDefinition.MainModule.ImportReference(typeof(JVLOriginalAssemblyHashAttribute).GetConstructor(new[] { typeof(string) })))
            {
                ConstructorArguments = { new CustomAttributeArgument(assemblyDefinition.MainModule.ImportReference(typeof(string)), hash) }
            });

            var outputFilename = Path.Combine(publicizedFolder, $"{Path.GetFileNameWithoutExtension(input)}_{JotunnBuildTask.Publicized}{Path.GetExtension(input)}");

            try
            {
                assemblyDefinition.Write(outputFilename);
            }
            catch (Exception exception)
            {
                System.Console.WriteLine($"Could not write file {outputFilename}.");
                System.Console.WriteLine(exception.Message);
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
