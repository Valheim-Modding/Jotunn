using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Mono.Cecil;
using Microsoft.Build.Framework;

namespace JotunnBuildTask
{
    public class JotunnBuildTask : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string ValheimPath { get; set; }

        public string ManagedPath { get; set; }

        internal const string ValheimServerData = "valheim_server_Data";
        internal const string ValheimData = "Valheim_Data";
        internal const string Managed = "Managed";
        internal const string PublicizedAssemblies = "publicized_assemblies";
        internal const string Bepinex = "BepInEx";
        internal const string Plugins = "plugins";
        internal const string Publicized = "publicized";

        private string GetManagedFolder()
        {
            if (Directory.Exists(ManagedPath))
            {
                return ManagedPath;
            }

            string clientManagedFolder = Path.Combine(ValheimPath, ValheimData, Managed);
            if (Directory.Exists(clientManagedFolder))
            {
                return clientManagedFolder;
            }

            string serverManagedFolder = Path.Combine(ValheimPath, ValheimServerData, Managed);
            if (Directory.Exists(serverManagedFolder))
            {
                return serverManagedFolder;
            }

            throw new Exception($"Could not find a managed assemblies folder at {ManagedPath} or below {ValheimPath}");
        }

        private bool HasSameHash(string assembly, string publicizedAssembly, out string hash)
        {
            hash = MD5HashFile(assembly);
            string publicizedHash = ReadHashFromDll(publicizedAssembly);
            return hash == publicizedHash;
        }

        private string ReadHashFromDll(string dllFile)
        {
            string result = "";

            if (File.Exists(dllFile))
            {
                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(dllFile);

                foreach (var customAttribute in assemblyDefinition.CustomAttributes)
                {
                    if (customAttribute.AttributeType.Name == nameof(JVLOriginalAssemblyHashAttribute))
                    {
                        result = customAttribute.ConstructorArguments[0].Value.ToString();
                        break;
                    }
                }

                assemblyDefinition.Dispose();
            }

            return result;
        }

        /// <summary>
        ///     Hash file (MD5).
        /// </summary>
        /// <param name="filename">filename</param>
        /// <returns>MD5 hash</returns>
        internal string MD5HashFile(string filename)
        {
            var hash = MD5.Create().ComputeHash(File.ReadAllBytes(filename));
            return BitConverter.ToString(hash).Replace("-", "");
        }

        public override bool Execute()
        {
            try
            {
                // Get managed folder of valheim or valheim_dedicated
                string managedFolder = GetManagedFolder();

                // Get publicized folder
                string publicizedFolder = Path.Combine(managedFolder, PublicizedAssemblies);
                if (!Directory.Exists(publicizedFolder))
                {
                    Directory.CreateDirectory(publicizedFolder);
                }

                List<string> assemblyNames = new List<string>();
                assemblyNames.AddRange(Directory.GetFiles(managedFolder, "assembly_*.dll"));
                assemblyNames.AddRange(Directory.GetFiles(managedFolder, "SoftReferenceableAssets.dll"));
                assemblyNames.AddRange(Directory.GetFiles(managedFolder, "gui_framework.dll"));

                // Loop assemblies and check if the hash has changed
                foreach (var assembly in assemblyNames)
                {
                    var publicizedAssembly = Path.Combine(publicizedFolder, $"{Path.GetFileNameWithoutExtension(assembly)}_{Publicized}{Path.GetExtension(assembly)}");

                    if (!HasSameHash(assembly, publicizedAssembly, out string hash))
                    {
                        try
                        {
                            // Try to publicize
                            if (!AssemblyPublicizer.PublicizeDll(assembly, hash, publicizedFolder, managedFolder))
                            {
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine($"Error occured on {assembly}");
                            System.Console.WriteLine(ex.Message);
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
