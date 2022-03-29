using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Mono.Cecil;
using MonoMod;
using MonoMod.RuntimeDetour.HookGen;
using Microsoft.Build.Framework;

namespace JotunnBuildTask
{
    public class JotunnBuildTask : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string ValheimPath { get; set; }

        internal const string ValheimServerData = "valheim_server_Data";
        internal const string ValheimData = "valheim_Data";
        internal const string Managed = "Managed";
        internal const string UnstrippedCorlib = "unstripped_corlib";
        internal const string PublicizedAssemblies = "publicized_assemblies";
        internal const string Bepinex = "BepInEx";
        internal const string Plugins = "plugins";
        internal const string Publicized = "publicized";

        private string ReadHashFromDll(string dllFile)
        {
            string result = "";

            if (File.Exists(dllFile))
            {
                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(dllFile);
                foreach (var typeDefinition in assemblyDefinition.MainModule.Types.Where(x => x.Namespace == "BepHookGen"))
                {
                    if (typeDefinition.Name.StartsWith("hash"))
                    {
                        result = typeDefinition.Name.Substring(4);
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
                string managedFolder = string.Empty;
                if (Directory.Exists(Path.Combine(ValheimPath, ValheimData, Managed)))
                {
                    managedFolder = Path.Combine(ValheimPath, ValheimData, Managed);
                }
                if (Directory.Exists(Path.Combine(ValheimPath, ValheimServerData, Managed)))
                {
                    managedFolder = Path.Combine(ValheimPath, ValheimServerData, Managed);
                }
                if (string.IsNullOrEmpty(managedFolder))
                {
                    throw new Exception($"{ValheimPath} does not include {ValheimData} or {ValheimServerData}");
                }

                // Get publicized folder
                string publicizedFolder = Path.Combine(managedFolder, PublicizedAssemblies);
                if (!Directory.Exists(publicizedFolder))
                {
                    Directory.CreateDirectory(publicizedFolder);
                }

                // Loop assemblies and check if the hash has changed
                foreach (var assembly in Directory.GetFiles(managedFolder, "assembly_*.dll"))
                {
                    if (!File.Exists(Path.Combine(publicizedFolder, $"{Path.GetFileNameWithoutExtension(assembly)}_{Publicized}{Path.GetExtension(assembly)}")))
                    {
                        try
                        {
                            // Try to publicize
                            if (!AssemblyPublicizer.PublicizeDll(assembly, publicizedFolder, ValheimPath, Log))
                            {
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.LogError($"Error occured on {assembly}");
                            Log.LogError(ex.Message);
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Log.LogError(e.Message);
                return false;
            }
        }
    }
}
