using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Utility class to query metadata about added content of any mod
    /// </summary>
    public class ModQuery
    {
        private static readonly Dictionary<string, Dictionary<int, ModPrefab>> Prefabs = new Dictionary<string, Dictionary<int, ModPrefab>>();

        internal static void Init()
        {
            Main.Harmony.PatchAll(typeof(ModQuery));
        }

        private class ModPrefab : IModPrefab
        {
            public GameObject Prefab { get; }
            public BepInPlugin SourceMod { get; }

            public ModPrefab(GameObject prefab, BepInPlugin mod)
            {
                Prefab = prefab;
                SourceMod = mod;
            }
        }

        /// <summary>
        ///     Get all prefabs that were added by mods. Does not include Vanilla prefabs.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IModPrefab> GetPrefabs()
        {
            List<IModPrefab> prefabs = new List<IModPrefab>();

            foreach (var prefab in Prefabs)
            {
                prefabs.AddRange(prefab.Value.Values);
            }

            prefabs.AddRange(PrefabManager.Instance.Prefabs.Values);
            return prefabs;
        }

        /// <summary>
        ///     Get all prefabs that were added by a specific mod
        /// </summary>
        /// <param name="modGuid"></param>
        /// <returns></returns>
        public static IEnumerable<IModPrefab> GetPrefabs(string modGuid)
        {
            List<IModPrefab> prefabs = new List<IModPrefab>();
            prefabs.AddRange(Prefabs[modGuid].Values);
            prefabs.AddRange(PrefabManager.Instance.Prefabs.Values.Where(x => x.SourceMod.GUID.Equals(modGuid)));
            return prefabs;
        }

        /// <summary>
        ///     Get an prefab by its name. Does not include Vanilla prefabs, see <see cref="PrefabManager.GetPrefab" />
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IModPrefab GetPrefab(string name)
        {
            int hash = name.GetStableHashCode();

            if (PrefabManager.Instance.Prefabs.ContainsKey(hash))
            {
                return PrefabManager.Instance.Prefabs[hash];
            }

            foreach (var prefab in Prefabs)
            {
                if (prefab.Value.ContainsKey(hash))
                {
                    return prefab.Value[hash];
                }
            }

            return null;
        }

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake)), HarmonyPostfix]
        private static void FejdStartup_Awake_Postfix()
        {
            var zNetAwake = AccessTools.Method(typeof(ZNetScene), nameof(ZNetScene.Awake));
            var prefixes = Harmony.GetPatchInfo(zNetAwake).Prefixes;
            var postfixes = Harmony.GetPatchInfo(zNetAwake).Postfixes;
            var finalizers = Harmony.GetPatchInfo(zNetAwake).Finalizers;

            CatchAllAddedPrefabs(prefixes);
            CatchAllAddedPrefabs(postfixes);
            CatchAllAddedPrefabs(finalizers);
        }

        private static void CatchAllAddedPrefabs(ICollection<Patch> patches)
        {
            foreach (var patch in patches)
            {
                var pre = AccessTools.Method(typeof(ModQuery), nameof(Pre));
                var post = AccessTools.Method(typeof(ModQuery), nameof(Post));
                Main.Harmony.Patch(patch.PatchMethod, new HarmonyMethod(pre), new HarmonyMethod(post));
            }
        }

        private static void Pre(object[] __args, ref PrefabInfo __state)
        {
            ZNetScene zNetScene = GetZNetScene(__args);
            __state = new PrefabInfo(zNetScene.m_namedPrefabs, zNetScene.m_prefabs);
        }

        private static void Post(object[] __args, ref PrefabInfo __state)
        {
            ZNetScene zNetScene = GetZNetScene(__args);
            var plugin = BepInExUtils.GetPluginInfoFromAssembly(ReflectionHelper.GetCallingAssembly());

            if (plugin == null)
            {
                Logger.LogWarning($"Assembly {ReflectionHelper.GetCallingAssembly().FullName} is not a BepInEx plugin");
                return;
            }

            var namedAdded = zNetScene.m_namedPrefabs.Except(__state.namedPrefabs);
            var prefabsAdded = zNetScene.m_prefabs.Except(__state.prefabs);

            AddPrefabs(namedAdded.Select(i => i.Value), plugin.Metadata);
            AddPrefabs(prefabsAdded, plugin.Metadata);

            Logger.LogInfo($"{plugin.Metadata.GUID} added {Prefabs[plugin.Metadata.GUID].Count} prefabs");

            foreach (var prefab in Prefabs[plugin.Metadata.GUID])
            {
                Logger.LogInfo($"\t{prefab.Value.Prefab.name}");
            }
        }

        private static void AddPrefabs(IEnumerable<GameObject> prefabs, BepInPlugin plugin)
        {
            if (!Prefabs.ContainsKey(plugin.GUID))
            {
                Prefabs.Add(plugin.GUID, new Dictionary<int, ModPrefab>());
            }

            foreach (var prefab in prefabs)
            {
                int hash = prefab.name.GetStableHashCode();

                if (!Prefabs[plugin.GUID].ContainsKey(hash))
                {
                    Prefabs[plugin.GUID].Add(hash, new ModPrefab(prefab, plugin));
                }
            }
        }

        private static IEnumerable<KeyValuePair<T1, T2>> DictionaryDiff<T1, T2>(IDictionary<T1, T2> dicOne, IDictionary<T1, T2> dicTwo)
        {
            return dicOne.Except(dicTwo).Concat(dicTwo.Except(dicOne));
        }

        private static ZNetScene GetZNetScene(object[] __args)
        {
            foreach (var arg in __args)
            {
                if (arg is ZNetScene zNetScene)
                {
                    return zNetScene;
                }
            }

            return ZNetScene.instance;
        }

        private struct PrefabInfo
        {
            public readonly Dictionary<int, GameObject> namedPrefabs;
            public readonly List<GameObject> prefabs;

            public PrefabInfo(IDictionary<int, GameObject> namedPrefabs, IEnumerable<GameObject> prefabs)
            {
                this.namedPrefabs = new Dictionary<int, GameObject>(namedPrefabs);
                this.prefabs = new List<GameObject>(prefabs);
            }
        }
    }
}
