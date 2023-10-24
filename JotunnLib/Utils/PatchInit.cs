using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jotunn.Utils
{
    internal class PatchInit
    {
        /// <summary>
        ///     Invoke patch initialization methods for all loaded mods.
        /// </summary>
        [Obsolete]
        internal static void InitializePatches()
        {
            List<Tuple<MethodInfo, int>> types = new List<Tuple<MethodInfo, int>>();
            HashSet<Assembly> searchedAssemblies = new HashSet<Assembly>();

            // Check in all Jotunn mods
            foreach (var baseUnityPlugin in BepInExUtils.GetDependentPlugins().Values)
            {
                try
                {
                    Assembly asm = baseUnityPlugin.GetType().Assembly;

                    // Skip already searched assemblies
                    if (searchedAssemblies.Contains(asm))
                    {
                        continue;
                    }

                    searchedAssemblies.Add(asm);

                    // Search in all types
                    foreach (var type in asm.GetTypes())
                    {
                        try
                        {
                            // on methods with the PatchInit attribute
                            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                         .Where(x => x.GetCustomAttributes(typeof(PatchInitAttribute), false).Length == 1))
                            {
                                var attribute = method.GetCustomAttributes(typeof(PatchInitAttribute), false).FirstOrDefault() as PatchInitAttribute;
                                types.Add(new Tuple<MethodInfo, int>(method, attribute.Priority));
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            // Invoke the method
            foreach (Tuple<MethodInfo, int> tuple in types.OrderBy(x => x.Item2))
            {
                Jotunn.Logger.LogDebug($"Applying patches in {tuple.Item1.DeclaringType.Name}.{tuple.Item1.Name}");
                tuple.Item1.Invoke(null, null);
            }
        }
    }
}
