using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using MonoMod;
using MonoMod.RuntimeDetour.HookGen;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace JotunnBuildTask
{
    public static class MMHookGenerator
    {

        /// <summary>
        ///     Call Monomod hookgen.
        /// </summary>
        /// <param name="input">Input assembly</param>
        /// <param name="mmhookFolder">MMHOOK output folder</param>
        /// <returns></returns>
        public static bool GenerateMMHook(string input, string mmhookFolder, string md5, string ValheimPath, TaskLoggingHelper Log)
        {
            string output = Path.Combine(mmhookFolder, $"MMHOOK_{Path.GetFileName(input)}");

            MonoModder modder = new MonoModder();
            modder.InputPath = input;
            modder.OutputPath = output;
            modder.ReadingMode = ReadingMode.Deferred;

            ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(Environment.CurrentDirectory, "bin", "Debug"));
            ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Replace("file:///home", "/home").Replace("file:///", "")));

            if (Directory.Exists(Path.Combine(ValheimPath, JotunnBuildTask.ValheimData, JotunnBuildTask.Managed)))
            {
                ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(ValheimPath, JotunnBuildTask.ValheimData, JotunnBuildTask.Managed));
            }

            if (Directory.Exists(Path.Combine(ValheimPath, JotunnBuildTask.ValheimServerData, JotunnBuildTask.Managed)))
            {
                ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(ValheimPath, JotunnBuildTask.ValheimServerData, JotunnBuildTask.Managed));
            }
            ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(ValheimPath, JotunnBuildTask.UnstrippedCorlib));

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

    }
}
