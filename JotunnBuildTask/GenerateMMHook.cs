using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Mono.Cecil;
using MonoMod;
using MonoMod.RuntimeDetour.HookGen;
using Microsoft.Build.Framework;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace JotunnBuildTask
{
    public class GenerateMMHook : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string ValheimPath { get; set; }

        internal const string ValheimServerData = "valheim_server_Data";
        internal const string ValheimData= "valheim_Data";
        internal const string Managed = "Managed";
        internal const string UnstrippedCorlib = "unstripped_corlib";
        internal const string PublicizedAssemblies = "publicized_assemblies";
        internal const string Bepinex = "BepInEx";
        internal const string Plugins = "plugins";
        internal const string Mmhook = "MMHOOK";
        internal const string Publicized = "publicized";

        /// <summary>
        ///     Hash file and compare with old hash.
        ///     Create new MMHOOK dll if not equal.
        /// </summary>
        /// <param name="file">dll to monomod</param>
        /// <param name="outputFolder">output file name</param>
        private bool HashAndCompare(string file, string outputFolder)
        {

            Log.LogMessage(MessageImportance.High, $"Processing {file}");

            var hash = MD5HashFile(file);

            string hashFilePath = Path.Combine(outputFolder, Path.GetFileName(file) + ".hash");
            if (File.Exists(hashFilePath))
            {
                // read hash and compare
                var oldHash = File.ReadAllText(hashFilePath);
                if (hash == oldHash)
                {
                    return true;
                }
            }

            // Try to publicize
            if (!AssemblyPublicizer.PublicizeDll(file, ValheimPath, Log))
            {
                return false;
            }

            // Try to generate HookGen
            if (!InvokeHookgen(file, Path.Combine(outputFolder, $"{Mmhook}_{Path.GetFileName(file)}"), hash))
            {
                return false;
            }

            // Write hash if everything was built
            File.WriteAllText(hashFilePath, hash);
            return true;
        }

        /// <summary>
        ///     Call Monomod hookgen.
        /// </summary>
        /// <param name="file">input dll</param>
        /// <param name="output">output dll</param>
        /// <returns></returns>
        private bool InvokeHookgen(string file, string output, string md5)
        {
            MonoModder modder = new MonoModder();
            modder.InputPath = file;
            modder.OutputPath = output;
            modder.ReadingMode = ReadingMode.Deferred;

            ((BaseAssemblyResolver) modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(Environment.CurrentDirectory, "bin", "Debug"));
            ((BaseAssemblyResolver) modder.AssemblyResolver)?.AddSearchDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Replace("file:///home","/home").Replace("file:///", "")));

            if (Directory.Exists(Path.Combine(ValheimPath, ValheimData, Managed)))
            {
                ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(ValheimPath, ValheimData, Managed));
            }

            if (Directory.Exists(Path.Combine(ValheimPath, ValheimServerData, Managed)))
            {
                ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(ValheimPath, ValheimServerData, Managed));
            }
            ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(ValheimPath, UnstrippedCorlib));
            
            /*
            foreach (var dir in ((BaseAssemblyResolver) modder.AssemblyResolver)?.GetSearchDirectories())
            {
                Log.LogMessage(MessageImportance.High,$"Searching in {dir}");
            }
            */

            modder.Read();

            modder.MapDependencies();

            if (File.Exists(output))
            {
                Log.LogMessage(MessageImportance.High, $"Clearing {output}");
                File.Delete(output);
            }

            HookGenerator hookGenerator = new HookGenerator(modder, Path.GetFileName(output));
            
            hookGenerator.HookPrivate = true;

            using (ModuleDefinition mOut = hookGenerator.OutputModule)
            {
                hookGenerator.Generate();
                mOut.Types.Add(new TypeDefinition("BepHookGen", "hash" + md5, TypeAttributes.AutoClass));
                mOut.Write(output);
            }

            Log.LogMessage(MessageImportance.High, $"Finished writing {output}");

            return true;
        }

        /// <summary>
        ///     Hash file (MD5).
        /// </summary>
        /// <param name="filename">filename</param>
        /// <returns>MD5 hash</returns>
        internal static string MD5HashFile(string filename)
        {
            var hash = MD5.Create().ComputeHash(File.ReadAllBytes(filename));
            return BitConverter.ToString(hash).Replace("-", "");
        }

        public override bool Execute()
        {

            Log.LogMessage(MessageImportance.High, "Current folder:  "+Environment.CurrentDirectory);
            Log.LogMessage(MessageImportance.High, "DLL Folder: "+ Assembly.GetExecutingAssembly().CodeBase);

            try
            {
                if (!Directory.Exists(Path.Combine(ValheimPath, Bepinex, Plugins, Mmhook)))
                {
                    Directory.CreateDirectory(Path.Combine(ValheimPath, Bepinex, Plugins, Mmhook));
                }

                var outputFolder = Path.Combine(ValheimPath, Bepinex, Plugins, Mmhook);

                
                if (Directory.Exists(Path.Combine(ValheimPath, ValheimData, Managed)))
                {
                    foreach (var file in Directory.GetFiles(Path.Combine(ValheimPath, ValheimData, Managed), "assembly_*.dll"))
                    {
                        if (!HashAndCompare(file, outputFolder))
                        {
                            Log.LogError($"Error occured on {file}");
                            return false;
                        }
                    }
                }

                if (Directory.Exists(Path.Combine(ValheimPath, ValheimServerData, Managed)))
                {
                    foreach (var file in Directory.GetFiles(Path.Combine(ValheimPath, ValheimServerData, Managed), "assembly_*.dll"))
                    {
                        if (!HashAndCompare(file, outputFolder))
                        {
                            Log.LogError($"Error occured on {file}");
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
