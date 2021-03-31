using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using System.Text;
using Mono.Cecil;
using MonoMod;
using MonoMod.RuntimeDetour.HookGen;

namespace JotunnBuildTask
{
    internal class Program
    {
        private static string ValheimPath = "";

        /// <summary>
        ///     Create new MMHOOK dll's if they don't exist or have changed
        /// </summary>
        /// <param name="args">Valheim folder</param>
        /// <returns>0 if successful</returns>
        private static int Main(string[] args)
        {
            try
            {

                if (args.Length != 1)
                {
                    Console.WriteLine("Only one argument: Path to Valheim");
                    return -2;
                }

                if (!Directory.Exists(Path.Combine(args[0], "BepInEx", "plugins", "MMHOOK")))
                {
                    Directory.CreateDirectory(Path.Combine(args[0], "BepInEx", "plugins", "MMHOOK"));
                }

                var outputFolder = Path.Combine(args[0], "BepInEx", "plugins", "MMHOOK");

                ValheimPath = args[0];
                if (Directory.Exists(Path.Combine(args[0], "valheim_Data", "Managed")))
                {
                    foreach (var file in Directory.GetFiles(Path.Combine(args[0], "valheim_Data", "Managed"), "assembly_*.dll"))
                    {
                        if (!HashAndCompare(file, outputFolder))
                        {
                            Console.WriteLine($"Error occured on {file}");
                            return -3;
                        }
                    }
                }

                if (Directory.Exists(Path.Combine(args[0], "valheim_server_Data", "Managed")))
                {
                    foreach (var file in Directory.GetFiles(Path.Combine(args[0], "valheim_server_Data", "Managed"), "assembly_*.dll"))
                    {
                        if (!HashAndCompare(file, outputFolder))
                        {
                            Console.WriteLine($"Error occured on {file}");
                            return -3;
                        }
                    }
                }



                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }

        /// <summary>
        ///     Hash file and compare with old hash
        ///     Create new MMHOOK dll if not equal
        /// </summary>
        /// <param name="file">dll to monomod</param>
        /// <param name="outputFolder">output file name</param>
        private static bool HashAndCompare(string file, string outputFolder)
        {
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

            if (!AssemblyPublicizer.PublicizeDll(file, ValheimPath))
            {
                return false;
            }

            string publicizedFile = Path.Combine(Path.GetDirectoryName(file), "publicized_assemblies",
                $"{Path.GetFileNameWithoutExtension(file)}_publicized{Path.GetExtension(file)}");

            // only write the hash to file if HookGen was successful
            if (InvokeHookgen(publicizedFile, Path.Combine(outputFolder, $"MMHOOK_{Path.GetFileNameWithoutExtension(file)}_publicized{Path.GetExtension(file)}"), hash))
            {
                File.WriteAllText(hashFilePath, hash);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Call monomod hookgen
        /// </summary>
        /// <param name="file">input dll</param>
        /// <param name="output">output dll</param>
        /// <returns></returns>
        private static bool InvokeHookgen(string file, string output, string md5)
        {
            MonoModder modder = new MonoModder();
            modder.InputPath = file;
            modder.OutputPath = output;
            modder.ReadingMode = ReadingMode.Deferred;

            if (Directory.Exists(Path.Combine(ValheimPath, "valheim_Data", "Managed")))
            {
                ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(ValheimPath, "valheim_Data", "Managed"));
            }

            if (Directory.Exists(Path.Combine(ValheimPath, "valheim_server_Data", "Managed")))
            {
                ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(ValheimPath, "valheim_server_Data", "Managed"));
            }
            ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(ValheimPath, "unstripped_corlib"));

            modder.Read();

            modder.MapDependencies();

            if (File.Exists(output))
            {
                Console.WriteLine($"Clearing {output}");
                File.Delete(output);
            }

            HookGenerator hookGenerator = new HookGenerator(modder, Path.GetFileName(output));

            using (ModuleDefinition mOut = hookGenerator.OutputModule)
            {
                hookGenerator.Generate();
                mOut.Types.Add(new TypeDefinition("BepHookGen", "hash" + md5, TypeAttributes.AutoClass));
                mOut.Write(output);
            }

            Console.WriteLine($"Finished writing {output}");

            return true;
        }

        /// <summary>
        ///     Hash file (MD5)
        /// </summary>
        /// <param name="filename">filename</param>
        /// <returns>MD5 hash</returns>
        private static string MD5HashFile(string filename)
        {
            var hash = MD5.Create().ComputeHash(File.ReadAllBytes(filename));
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}