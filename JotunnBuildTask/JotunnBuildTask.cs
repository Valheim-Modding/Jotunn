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

        internal const string ValheimServerData = "valheim_server_Data";
        internal const string ValheimData = "Valheim_Data";
        internal const string Managed = "Managed";
        internal const string PublicizedAssemblies = "publicized_assemblies";
        internal const string Bepinex = "BepInEx";
        internal const string Plugins = "plugins";
        internal const string Publicized = "publicized";

        /// <summary>
        ///     Names of the game data folders to look for, in order of precedence.
        ///     The client folder wins over the dedicated server folder.
        /// </summary>
        private static readonly string[] DataFolders = { ValheimData, ValheimServerData };

        /// <summary>
        ///     Get the Managed folders of a Valheim installation, in order of precedence.
        /// </summary>
        /// <remarks>
        ///     The data folder names are matched case-insensitively: the client folder is named
        ///     Valheim_Data on Windows but valheim_Data on Linux. Both spellings resolve on the
        ///     case-insensitive filesystems of Windows and macOS, so a case-sensitive lookup only
        ///     ever fails on Linux.
        /// </remarks>
        /// <param name="valheimPath">Root folder of the Valheim installation</param>
        /// <returns>Existing Managed folders, most preferred first. Empty if none were found.</returns>
        internal static List<string> GetManagedFolders(string valheimPath)
        {
            List<string> managedFolders = new List<string>();

            if (!Directory.Exists(valheimPath))
            {
                return managedFolders;
            }

            string[] subFolders = Directory.GetDirectories(valheimPath);

            foreach (var dataFolder in DataFolders)
            {
                foreach (var subFolder in subFolders)
                {
                    if (!string.Equals(Path.GetFileName(subFolder), dataFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string managedFolder = Path.Combine(subFolder, Managed);

                    if (Directory.Exists(managedFolder))
                    {
                        managedFolders.Add(managedFolder);
                    }
                }
            }

            return managedFolders;
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
                string managedFolder = GetManagedFolders(ValheimPath).FirstOrDefault();
                if (string.IsNullOrEmpty(managedFolder))
                {
                    throw new Exception($"{ValheimPath} does not include a {ValheimData} or {ValheimServerData} folder containing a {Managed} folder");
                }

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
                            if (!AssemblyPublicizer.PublicizeDll(assembly, hash, publicizedFolder, ValheimPath))
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
