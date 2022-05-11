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
        private static readonly Dictionary<string, List<Recipe>> Recipes = new Dictionary<string, List<Recipe>>();

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
            PatchZNetViewPatches(Harmony.GetPatchInfo(zNetAwake).Prefixes);
            PatchZNetViewPatches(Harmony.GetPatchInfo(zNetAwake).Postfixes);
            PatchZNetViewPatches(Harmony.GetPatchInfo(zNetAwake).Finalizers);

            var objectDBAwake = AccessTools.Method(typeof(ObjectDB), nameof(ObjectDB.Awake));
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBAwake).Prefixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBAwake).Postfixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBAwake).Finalizers);

            var objectDBCopyOtherDB = AccessTools.Method(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB));
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBCopyOtherDB).Prefixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBCopyOtherDB).Postfixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBCopyOtherDB).Finalizers);

            var objectDBUpdateItemHashes = AccessTools.Method(typeof(ObjectDB), nameof(ObjectDB.UpdateItemHashes));
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBUpdateItemHashes).Prefixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBUpdateItemHashes).Postfixes);
            PatchObjectDbPatches(Harmony.GetPatchInfo(objectDBUpdateItemHashes).Finalizers);
        }

        private static void PatchZNetViewPatches(ICollection<Patch> patches)
        {
            foreach (var patch in patches)
            {
                var pre = AccessTools.Method(typeof(ModQuery), nameof(BeforeZNetPatch));
                var post = AccessTools.Method(typeof(ModQuery), nameof(AfterZNetPatch));
                Main.Harmony.Patch(patch.PatchMethod, new HarmonyMethod(pre), new HarmonyMethod(post));
            }
        }

        private static void PatchObjectDbPatches(ICollection<Patch> patches)
        {
            foreach (var patch in patches)
            {
                var pre = AccessTools.Method(typeof(ModQuery), nameof(BeforeObjectDBPatch));
                var post = AccessTools.Method(typeof(ModQuery), nameof(AfterObjectDBPatch));
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

        private static void AfterObjectDBPatch(object[] __args, ref ObjectDBState __state)
        {
            ObjectDB objectDB = GetObjectDB(__args);
            var plugin = BepInExUtils.GetPluginInfoFromAssembly(ReflectionHelper.GetCallingAssembly());

            if (plugin == null)
            {
                Logger.LogWarning($"Assembly {ReflectionHelper.GetCallingAssembly().FullName} is not a BepInEx plugin");
                return;
            }

            var itemsAdded = objectDB.m_items.Except(__state.items);
            var itemByHashAdded = objectDB.m_itemByHash.Except(__state.itemByHash);
            var recipesAdded = objectDB.m_recipes.Except(__state.recipes);

            AddPrefabs(itemsAdded, plugin.Metadata);
            AddPrefabs(itemByHashAdded.Select(i => i.Value), plugin.Metadata);
            AddRecipes(recipesAdded, plugin.Metadata);

            Logger.LogInfo($"{plugin.Metadata.GUID} added {Prefabs[plugin.Metadata.GUID].Count} prefabs");

            foreach (var prefab in Prefabs[plugin.Metadata.GUID])
            {
                Logger.LogInfo($"\t{prefab.Value.Prefab.name}");
            }

            Logger.LogInfo($"{plugin.Metadata.GUID} added {Recipes[plugin.Metadata.GUID].Count} recipes");

            foreach (var recipe in Recipes[plugin.Metadata.GUID])
            {
                Logger.LogInfo($"\t{recipe.m_item.name}");
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


        private static void AddRecipes(IEnumerable<Recipe> recipes, BepInPlugin plugin)
        {
            if (!Recipes.ContainsKey(plugin.GUID))
            {
                Recipes.Add(plugin.GUID, new List<Recipe>());
            }

            foreach (var recipe in recipes)
            {
                if (!Recipes[plugin.GUID].Contains(recipe))
                {
                    Recipes[plugin.GUID].Add(recipe);
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

        private static ObjectDB GetObjectDB(object[] __args)
        {
            foreach (var arg in __args)
            {
                if (arg is ObjectDB zNetScene)
                {
                    return zNetScene;
                }
            }

            return ObjectDB.instance;
        }

        private struct ZNetSceneState
        {
            public readonly Dictionary<int, GameObject> namedPrefabs;
            public readonly List<GameObject> prefabs;

            public ZNetSceneState(ZNetScene zNetScene)
            {
                this.namedPrefabs = new Dictionary<int, GameObject>(zNetScene.m_namedPrefabs);
                this.prefabs = new List<GameObject>(zNetScene.m_prefabs);
            }
        }

        private struct ObjectDBState
        {
            public List<GameObject> items;
            public List<Recipe> recipes;
            public Dictionary<int, GameObject> itemByHash;

            public ObjectDBState(ObjectDB objectDB)
            {
                items = new List<GameObject>(objectDB.m_items);
                recipes = new List<Recipe>(objectDB.m_recipes);
                itemByHash = new Dictionary<int, GameObject>(objectDB.m_itemByHash);
            }
        }
    }
}
