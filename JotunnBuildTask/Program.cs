using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace JotunnBuildTask
{
    internal class Program
    {
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

                if (!Directory.Exists(Path.Combine(args[0], "BepinEx", "plugins", "MMHOOK")))
                {
                    Directory.CreateDirectory(Path.Combine(args[0], "BepinEx", "plugins", "MMHOOK"));
                }

                var outputFolder = Path.Combine(args[0], "BepInEx", "plugins", "MMHOOK");

                foreach (var file in Directory.GetFiles(Path.Combine(args[0], "valheim_Data", "Managed"), "assembly_*.dll"))
                {
                    HashAndCompare(file, outputFolder);
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return -1;
            }
        }

        /// <summary>
        ///     Hash file and compare with old hash
        ///     Create new MMHOOK dll if not equal
        /// </summary>
        /// <param name="file">dll to monomod</param>
        /// <param name="outputFolder">output file name</param>
        private static void HashAndCompare(string file, string outputFolder)
        {
            var hash = MD5HashFile(file);

            if (File.Exists(Path.Combine(outputFolder, Path.GetFileName(file) + ".hash")))
            {
                // read hash and compare
                var oldHash = File.ReadAllText(Path.Combine(outputFolder, Path.GetFileName(file) + ".hash"));
                if (hash == oldHash)
                {
                    return;
                }
            }

            // only write the hash to file if HookGen was successful
            if (InvokeHookgen(file, Path.Combine(outputFolder, "MMHOOK_" + Path.GetFileName(file))))
            {
                File.WriteAllText(Path.Combine(outputFolder, Path.GetFileName(file) + ".hash"), hash);
            }
        }

        /// <summary>
        ///     Call monomod hookgen
        /// </summary>
        /// <param name="file">input dll</param>
        /// <param name="output">output dll</param>
        /// <returns></returns>
        private static bool InvokeHookgen(string file, string output)
        {
            var psi = new ProcessStartInfo(Path.Combine(Environment.CurrentDirectory, "MonoMod.RuntimeDetour.HookGen.exe"));
            psi.CreateNoWindow = true;
            psi.Arguments = " --private --orig \"" + file + "\" \"" + output + "\"";
            psi.UseShellExecute = true;
            var hookGen = Process.Start(psi);
            hookGen.WaitForExit();
            return hookGen.ExitCode == 0;
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