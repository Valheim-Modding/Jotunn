using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Utility class to query metadata about added content of any loaded mod, including non-Jötunn ones.
    ///     It is disabled by default, as it unnecessary increases the loading time when not used.<br/>
    ///     <see cref="ModQuery.Enable()"/> has to be called anytime before FejdStartup.Awake, meaning in your plugin's Awake or Start.
    /// </summary>
    public class ModQuery
    {
        private static readonly Dictionary<string, Dictionary<int, ModPrefab>> Prefabs = new Dictionary<string, Dictionary<int, ModPrefab>>();
        private static readonly Dictionary<string, List<Recipe>> Recipes = new Dictionary<string, List<Recipe>>();

        private static readonly HashSet<MethodInfo> PatchedZNetMethods = new HashSet<MethodInfo>();
        private static readonly HashSet<MethodInfo> PatchedObjectDBMethods = new HashSet<MethodInfo>();

        private static readonly MethodInfo PreZNetPatch = AccessTools.Method(typeof(ModQuery), nameof(BeforeZNetPatch));
        private static readonly MethodInfo PostZNetPatch = AccessTools.Method(typeof(ModQuery), nameof(AfterZNetPatch));
        private static readonly MethodInfo PreObjectDB = AccessTools.Method(typeof(ModQuery), nameof(BeforeObjectDBPatch));
        private static readonly MethodInfo PostObjectDB = AccessTools.Method(typeof(ModQuery), nameof(AfterObjectDBPatch));

        private static bool enabled = false;

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
        ///     Enables the collection of mod metadata.
        ///     It is disabled by default, as it unnecessary increases the loading time when not used.<br/>
        ///     This method has to be called anytime before FejdStartup.Awake, meaning in your plugin's Awake or Start.
        /// </summary>
        public static void Enable()
        {
            enabled = true;
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
        ///     Get an prefab by its name.
        ///     Does not include Vanilla prefabs, see <see cref="PrefabManager.GetPrefab">PrefabManager.GetPrefab(string)</see>
        ///     for those.
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
            if (!enabled)
            {
                return;
            }

            var zNetAwake = AccessTools.Method(typeof(ZNetScene), nameof(ZNetScene.Awake));
            PatchZNetViewPatches(Harmony.GetPatchInfo(zNetAwake)?.Prefixes);
            PatchZNetViewPatches(Harmony.GetPatchInfo(zNetAwake)?.Postfixes);
            PatchZNetViewPatches(Harmony.GetPatchInfo(zNetAwake)?.Finalizers);

            var objectDBAwake = AccessTools.Method(typeof(ObjectDB), nameof(ObjectDB.Awake));
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBAwake)?.Prefixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBAwake)?.Postfixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBAwake)?.Finalizers);

            var objectDBCopyOtherDB = AccessTools.Method(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB));
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBCopyOtherDB)?.Prefixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBCopyOtherDB)?.Postfixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBCopyOtherDB)?.Finalizers);

            var objectDBUpdateItemHashes = AccessTools.Method(typeof(ObjectDB), nameof(ObjectDB.UpdateItemHashes));
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBUpdateItemHashes)?.Prefixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBUpdateItemHashes)?.Postfixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBUpdateItemHashes)?.Finalizers);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPrefix, HarmonyPriority(1000)]
        private static void ObjectDBAwake(ObjectDB __instance)
        {
            if (!enabled)
            {
                return;
            }

            // make sure vanilla prefabs are already added to not assign them to the first mod that call this function in a prefix
            __instance.UpdateItemHashes();
        }

        private static void PatchZNetViewPatches(ICollection<Patch> patches)
        {
            PatchPatches(patches, PatchedZNetMethods, PreZNetPatch, PostZNetPatch);
        }

        private static void PatchObjectDbPatches(ICollection<Patch> patches)
        {
            PatchPatches(patches, PatchedObjectDBMethods, PreObjectDB, PostObjectDB);
        }

        private static void PatchPatches(ICollection<Patch> patches, HashSet<MethodInfo> patchedMethods, MethodInfo pre, MethodInfo post)
        {
            if (patches == null)
            {
                return;
            }

            foreach (var patch in patches)
            {
                if (patch.owner == Main.ModGuid)
                {
                    continue;
                }

                if (patchedMethods.Contains(patch.PatchMethod))
                {
                    continue;
                }

                patchedMethods.Add(patch.PatchMethod);
                Main.Harmony.Patch(patch.PatchMethod, new HarmonyMethod(pre), new HarmonyMethod(post));
            }
        }

        private static void BeforeZNetPatch(object[] __args, ref ZNetSceneState __state)
        {
            ZNetScene zNetScene = GetZNetScene(__args);
            __state = new ZNetSceneState(zNetScene);
        }

        private static void BeforeObjectDBPatch(object[] __args, ref ObjectDBState __state)
        {
            ObjectDB objectDB = GetObjectDB(__args);
            __state = new ObjectDBState(objectDB);
        }

        private static void AfterZNetPatch(object[] __args, ref ZNetSceneState __state)
        {
            if (!__state.valid)
            {
                return;
            }

            ZNetScene zNetScene = GetZNetScene(__args);
            var plugin = BepInExUtils.GetPluginInfoFromAssembly(ReflectionHelper.GetCallingAssembly());

            if (plugin == null)
            {
                return;
            }

            AddPrefabs(__state.namedPrefabs, zNetScene.m_namedPrefabs, plugin.Metadata);
            AddPrefabs(__state.prefabs, zNetScene.m_prefabs, plugin.Metadata);
        }

        private static void AfterObjectDBPatch(object[] __args, ref ObjectDBState __state)
        {
            if (!__state.valid)
            {
                return;
            }

            ObjectDB objectDB = GetObjectDB(__args);
            var plugin = BepInExUtils.GetPluginInfoFromAssembly(ReflectionHelper.GetCallingAssembly());

            if (plugin == null)
            {
                return;
            }

            AddPrefabs(__state.items, objectDB.m_items, plugin.Metadata);
            AddPrefabs(__state.itemByHash, objectDB.m_itemByHash, plugin.Metadata);
            AddRecipes(__state.recipes, objectDB.m_recipes, plugin.Metadata);
        }

        private static void AddPrefabs(IEnumerable<GameObject> before, IEnumerable<GameObject> after, BepInPlugin plugin)
        {
            AddPrefabs(new HashSet<GameObject>(before), new HashSet<GameObject>(after), plugin);
        }

        private static void AddPrefabs(Dictionary<int, GameObject> before, Dictionary<int, GameObject> after, BepInPlugin plugin)
        {
            AddPrefabs(new HashSet<GameObject>(before.Values), new HashSet<GameObject>(after.Values), plugin);
        }

        private static void AddPrefabs(HashSet<GameObject> before, HashSet<GameObject> after, BepInPlugin plugin)
        {
            if (!Prefabs.ContainsKey(plugin.GUID))
            {
                Prefabs.Add(plugin.GUID, new Dictionary<int, ModPrefab>());
            }

            foreach (var prefab in after)
            {
                if (before.Contains(prefab))
                {
                    continue;
                }

                int hash = prefab.name.GetStableHashCode();

                if (!Prefabs[plugin.GUID].ContainsKey(hash))
                {
                    Prefabs[plugin.GUID].Add(hash, new ModPrefab(prefab, plugin));
                }
            }
        }

        private static void AddRecipes(IEnumerable<Recipe> before, IEnumerable<Recipe> after, BepInPlugin plugin)
        {
            AddRecipes(new HashSet<Recipe>(before), new HashSet<Recipe>(after), plugin);
        }

        private static void AddRecipes(HashSet<Recipe> before, HashSet<Recipe> after, BepInPlugin plugin)
        {
            if (!Recipes.ContainsKey(plugin.GUID))
            {
                Recipes.Add(plugin.GUID, new List<Recipe>());
            }

            foreach (var recipe in after)
            {
                if (before.Contains(recipe))
                {
                    continue;
                }

                if (!Recipes[plugin.GUID].Contains(recipe))
                {
                    Recipes[plugin.GUID].Add(recipe);
                }
            }
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

        private static ObjectDB GetObjectDB(object[] __args)
        {
            foreach (var arg in __args)
            {
                if (arg is ObjectDB objectDB)
                {
                    return objectDB;
                }
            }

            return ObjectDB.instance;
        }

        private class ZNetSceneState
        {
            public bool valid;
            public readonly Dictionary<int, GameObject> namedPrefabs;
            public readonly List<GameObject> prefabs;

            public ZNetSceneState(ZNetScene zNetScene)
            {
                valid = (bool)zNetScene;

                if (!valid)
                {
                    return;
                }

                this.namedPrefabs = new Dictionary<int, GameObject>(zNetScene.m_namedPrefabs);
                this.prefabs = new List<GameObject>(zNetScene.m_prefabs);
            }
        }

        private class ObjectDBState
        {
            public bool valid;
            public List<GameObject> items;
            public List<Recipe> recipes;
            public Dictionary<int, GameObject> itemByHash;

            public ObjectDBState(ObjectDB objectDB)
            {
                valid = (bool)objectDB;

                if (!valid)
                {
                    return;
                }

                items = new List<GameObject>(objectDB.m_items);
                recipes = new List<Recipe>(objectDB.m_recipes);
                itemByHash = new Dictionary<int, GameObject>(objectDB.m_itemByHash);
            }
        }
    }
}
