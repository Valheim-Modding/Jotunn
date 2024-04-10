using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Jotunn.Extensions;
using Jotunn.Managers.MockSystem;
using Jotunn.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Handles all logic to do with managing mocked prefabs added into the game.
    /// </summary>
    internal class MockManager : IManager
    {
        private static MockManager _instance;

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static MockManager Instance => _instance ??= new MockManager();

        private static MethodInfo Object_IsPersistent { get; } = AccessTools.Method(typeof(Object), "IsPersistent");

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private MockManager()
        {
            ((IManager)this).Init();
        }

        /// <summary>
        ///     Legacy ValheimLib prefix used by the Mock System to recognize Mock gameObject that must be replaced at some point.
        /// </summary>
        [Obsolete("Legacy ValheimLib mock prefix. Use JVLMockPrefix \"JVLmock_\" instead.")]
        public const string MockPrefix = "VLmock_";

        /// <summary>
        ///     Prefix used by the Mock System to recognize Mock gameObject that must be replaced at some point.
        /// </summary>
        public const string JVLMockPrefix = "JVLmock_";

        /// <summary>
        ///     String used by the Mock System to recognize start of a relative path of a replacement for a Mock gameObject.
        /// </summary>
        public const string JVLMockSeparator = "__";

        /// <summary>
        ///     Internal container for mocked prefabs
        /// </summary>
        internal GameObject MockPrefabContainer;

        private Dictionary<string, GameObject> mockedPrefabs = new Dictionary<string, GameObject>();
        private static HashSet<Material> fixedMaterials = new HashSet<Material>();
        private static HashSet<Material> queuedToFixMaterials = new HashSet<Material>();
        private static bool allVanillaObjectsAvailable;

        /// <summary>
        ///     Creates the container and registers all hooks
        /// </summary>
        void IManager.Init()
        {
            Main.LogInit("MockManager");

            MockPrefabContainer = new GameObject("MockPrefabs");
            MockPrefabContainer.transform.parent = Main.RootObject.transform;
            MockPrefabContainer.SetActive(false);

            Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            // use a later event to fix materials as more textures have loaded by then
            [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start)), HarmonyPostfix]
            private static void ZoneSystem_Start(ZoneSystem __instance) => FixQueuedMaterials();
        }

        /// <summary>
        ///     Create an empty GameObject with the mock string prepended
        /// </summary>
        /// <param name="prefabName">Name of the mocked vanilla prefab</param>
        /// <returns>Mocked GameObject reference</returns>
        public GameObject CreateMockedGameObject(string prefabName)
        {
            string name = JVLMockPrefix + prefabName;

            if (mockedPrefabs.TryGetValue(name, out GameObject mock) && mock)
            {
                return mock;
            }

            GameObject g = new GameObject(name);
            g.transform.parent = MockPrefabContainer.transform;
            g.SetActive(false);
            mockedPrefabs[name] = g;

            return g;
        }

        /// <summary>
        ///     Create a mocked component on an empty GameObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        public T CreateMockedPrefab<T>(string prefabName) where T : Component
        {
            GameObject g = CreateMockedGameObject(prefabName);
            string name = g.name;

            T mock = g.GetOrAddComponent<T>();
            if (!mock)
            {
                Logger.LogWarning($"Could not create mock for prefab {prefabName} of type {typeof(T)}");
                return null;
            }

            mock.name = name;
            return mock;
        }

        /// <summary>
        ///     Will try to find the real vanilla prefab from the given mock
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="unityObject"></param>
        /// <returns>the real prefab</returns>
        public static T GetRealPrefabFromMock<T>(Object unityObject) where T : Object
        {
            return (T)GetRealPrefabFromMock(unityObject, typeof(T));
        }

        /// <summary>
        ///     Will try to find the real vanilla prefab from the given mock
        /// </summary>
        /// <param name="unityObject"></param>
        /// <param name="mockObjectType"></param>
        /// <returns>the real prefab</returns>
        public static Object GetRealPrefabFromMock(Object unityObject, Type mockObjectType)
        {
            if (!unityObject)
            {
                return null;
            }

            if (GUIUtils.IsHeadless && mockObjectType == typeof(Texture))
            {
                return null;
            }

            if (!IsMockName(GetCleanedName(mockObjectType, unityObject.name), out string assetName, out List<string> childNames))
            {
                return null;
            }

            if (childNames.Count == 0 && TryGetAsset(mockObjectType, assetName, out Object asset))
            {
                return asset;
            }

            var prefab = PrefabManager.Cache.GetPrefab<GameObject>(assetName);

            if (!prefab)
            {
                throw new MockResolveException($"Object with name '{assetName}' was not found.", assetName, mockObjectType);
            }

            if (childNames.Count > 0)
            {
                var child = prefab.FindDeepChild(childNames);

                if (!child || child.name != childNames.Last())
                {
                    throw new MockResolveException($"Child '{childNames.Last()}' not found with the specified path.", assetName, childNames, mockObjectType);
                }

                prefab = child.gameObject;
            }

            if (TryFindAssetInSelfOrChildComponents(prefab, mockObjectType, out asset))
            {
                return asset;
            }

            if (childNames.Count > 0)
            {
                var usedPath = prefab.transform.GetPath().TrimStart('/');
                throw new MockResolveException($"{mockObjectType.Name} not found at child '{usedPath}'.", assetName, childNames, mockObjectType);
            }
            else
            {
                throw new MockResolveException($"{mockObjectType.Name} not found at prefab '{assetName}'.", assetName, mockObjectType);
            }
        }

        private static bool TryGetAsset(Type mockObjectType, string assetName, out Object asset)
        {
            asset = PrefabManager.Cache.GetPrefab(mockObjectType, assetName);
            return (bool)asset;
        }

        internal static void FixReferences(object objectToFix, int depth)
        {
            // This is totally arbitrary.
            // I had to add a depth because of call stack exploding otherwise
            if (depth == 5 || objectToFix == null)
            {
                return;
            }

            var type = objectToFix.GetType();
            ClassMember classMember = ClassMember.GetClassMember(type);

            foreach (var member in classMember.Members)
            {
                FixMemberReferences(member, objectToFix, depth + 1);
            }
        }

        internal static void ReplaceMockGameObject(Transform child, GameObject realPrefab, GameObject parent)
        {
            if (IsPersistent(parent))
            {
                Logger.LogWarning($"Cannot replace mock child {child.name} in persistent prefab {parent.name}. " +
                                  $"Clone the prefab before replacing mocks, i.e. with PrefabManager.Instance.CreateClonedPrefab " +
                                  $"or ZoneManager.Instance.CreateLocationContainer for locations");
                return;
            }

            var newObject = Object.Instantiate(realPrefab, parent.transform);
            newObject.name = realPrefab.name;
            newObject.SetActive(child.gameObject.activeSelf);
            newObject.transform.position = child.gameObject.transform.position;
            newObject.transform.rotation = child.gameObject.transform.rotation;
            newObject.transform.localScale = child.gameObject.transform.localScale;

            int siblingIndex = child.GetSiblingIndex();
            Object.DestroyImmediate(child.gameObject);
            newObject.transform.SetSiblingIndex(siblingIndex);
        }

        private static bool IsPersistent(GameObject parent) {
            return (bool)Object_IsPersistent.Invoke(null, new object[] { parent });
        }

        private static bool IsMockName(string name, out string assetName, out List<string> childNames)
        {
            if (name.StartsWith(JVLMockPrefix, StringComparison.Ordinal))
            {
                var mockTarget = name.Substring(JVLMockPrefix.Length);
                var splitNames = mockTarget.Split(new[] { JVLMockSeparator }, StringSplitOptions.RemoveEmptyEntries);
                assetName = splitNames[0];
                childNames = splitNames.Skip(1).ToList();
                return true;
            }

#pragma warning disable CS0618
            if (name.StartsWith(MockPrefix, StringComparison.Ordinal))
            {
                assetName = name.Substring(MockPrefix.Length);
                childNames = new List<string>();
                return true;
            }
#pragma warning restore CS0618

            assetName = name;
            childNames = new List<string>();
            return false;
        }

        private static string GetCleanedName(Type objectType, string name)
        {
            // Cut off the suffix in the name to correctly query the original
            if (objectType == typeof(Material))
            {
                return name.RemoveSuffix(" (Instance)");
            }
            else if (objectType == typeof(Mesh))
            {
                return name.RemoveSuffix(" Instance");
            }

            return name;
        }

        private static void FixMemberReferences(MemberBase member, object objectToFix, int depth)
        {
            // Special treatment for DropTable, its a List of struct DropData
            // Maybe there comes a time when I am willing to do some generic stuff
            // But mono did not implement FieldInfo.GetValueDirect()
            if (member.MemberType == typeof(DropTable))
            {
                FixDropTable(member, objectToFix);
                return;
            }

            if (member.IsUnityObject && member.HasGetMethod)
            {
                var target = (Object)member.GetValue(objectToFix);
                var realPrefab = GetRealPrefabFromMock(target, member.MemberType);
                if (realPrefab)
                {
                    member.SetValue(objectToFix, realPrefab);
                }
                else if (target is Material material)
                {
                    TryFixMaterial(material);
                }
            }
            else if (member.IsEnumeratedClass && member.IsEnumerableOfUnityObjects)
            {
                var isArray = member.MemberType.IsArray;
                var isList = member.MemberType.IsGenericType && member.MemberType.GetGenericTypeDefinition() == typeof(List<>);
                var isHashSet = member.MemberType.IsGenericType && member.MemberType.GetGenericTypeDefinition() == typeof(HashSet<>);

                if (!(isArray || isList || isHashSet))
                {
                    Logger.LogWarning($"Not fixing potential mock references for field {member.MemberType.Name} : {member.MemberType} is not supported.");
                    return;
                }

                var currentValues = (IEnumerable<Object>)member.GetValue(objectToFix);
                if (currentValues == null)
                {
                    return;
                }

                var list = new List<Object>();
                bool hasAnyMockResolved = false;

                foreach (var unityObject in currentValues)
                {
                    var realPrefab = GetRealPrefabFromMock(unityObject, member.EnumeratedType);
                    list.Add(realPrefab ? realPrefab : unityObject);
                    hasAnyMockResolved = hasAnyMockResolved || realPrefab;

                    if (!realPrefab && unityObject is Material material)
                    {
                        TryFixMaterial(material);
                    }
                }

                if (list.Count > 0 && hasAnyMockResolved)
                {
                    MethodInfo cast = ReflectionHelper.Cache.EnumerableCast;
                    MethodInfo castT = cast.MakeGenericMethod(member.EnumeratedType);
                    object correctTypeList = castT.Invoke(null, new object[] { list });

                    if (isArray)
                    {
                        var toArray = ReflectionHelper.Cache.EnumerableToArray;
                        var toArrayT = toArray.MakeGenericMethod(member.EnumeratedType);

                        var array = toArrayT.Invoke(null, new object[] { correctTypeList });
                        member.SetValue(objectToFix, array);
                    }
                    else if (isList)
                    {
                        var toList = ReflectionHelper.Cache.EnumerableToList;
                        var toListT = toList.MakeGenericMethod(member.EnumeratedType);

                        var newList = toListT.Invoke(null, new object[] { correctTypeList });
                        member.SetValue(objectToFix, newList);
                    }
                    else if (isHashSet)
                    {
                        var hash = typeof(HashSet<>).MakeGenericType(member.EnumeratedType);

                        var newHash = Activator.CreateInstance(hash, correctTypeList);
                        member.SetValue(objectToFix, newHash);
                    }
                }
            }
            else if (member.IsEnumeratedClass)
            {
                var isDict = member.MemberType.IsGenericType && member.MemberType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
                if (isDict)
                {
                    Logger.LogWarning($"Not fixing potential mock references for field {member.MemberType.Name} : Dictionary is not supported.");
                    return;
                }

                var currentValues = (IEnumerable<object>)member.GetValue(objectToFix);
                if (currentValues == null)
                {
                    return;
                }

                foreach (var value in currentValues)
                {
                    FixReferences(value, depth);
                }
            }
            else if (member.IsClass && member.HasGetMethod)
            {
                FixReferences(member.GetValue(objectToFix), depth);
            }
        }

        private static void FixDropTable(MemberBase member, object objectToFix)
        {
            var drops = ((DropTable)member.GetValue(objectToFix)).m_drops;

            for (int i = 0; i < drops.Count; i++)
            {
                var drop = drops[i];
                var realPrefab = GetRealPrefabFromMock<GameObject>(drop.m_item);
                if (realPrefab)
                {
                    drop.m_item = realPrefab;
                }

                drops[i] = drop;
            }
        }

        private static void TryFixMaterial(Material material)
        {
            if (GUIUtils.IsHeadless)
            {
                return;
            }

            if (!material || fixedMaterials.Contains(material) || queuedToFixMaterials.Contains(material))
            {
                return;
            }

            FixMaterial(material);
        }

        private static void FixQueuedMaterials()
        {
            // if the cache is already initialized, some later loaded textures are not found
            PrefabManager.Cache.Clear<Texture>();
            allVanillaObjectsAvailable = true;

            foreach (var material in new HashSet<Material>(queuedToFixMaterials))
            {
                queuedToFixMaterials.Remove(material);
                FixMaterial(material);
            }
        }

        private static void FixMaterial(Material material)
        {
            if (!material || fixedMaterials.Contains(material))
            {
                return;
            }

            bool fixedTextures = FixTextures(material);
            bool fixedShader = true;

            if (!GUIUtils.IsHeadless)
            {
                fixedShader = FixShader(material);
            }

            if (fixedTextures && fixedShader)
            {
                fixedMaterials.Add(material);
            }
            else
            {
                queuedToFixMaterials.Add(material);
            }
        }

        /// <summary>
        ///     Replaces all mock Textures with the real Texture
        /// </summary>
        /// <param name="material"></param>
        /// <returns>true if no mocks were found or all mock could be resolved</returns>
        private static bool FixTextures(Material material)
        {
            bool everythingFixed = true;

            foreach (int prop in material.GetTexturePropertyNameIDs())
            {
                Texture texture = material.GetTexture(prop);

                if (!texture)
                {
                    continue;
                }

                Texture realTexture;

                try
                {
                    realTexture = GetRealPrefabFromMock<Texture>(texture);
                }
                catch (MockResolveException ex)
                {
                    if (allVanillaObjectsAvailable)
                    {
                        Logger.LogWarning(ex.Message);
                    }

                    everythingFixed = false;
                    continue;
                }

                if (realTexture)
                {
                    material.SetTexture(prop, realTexture);
                }
            }

            return everythingFixed;
        }

        /// <summary>
        ///     Replaces a potential mock Shader with the real Shader
        /// </summary>
        /// <param name="material"></param>
        /// <returns>true if no mock was found or the mock could be resolved</returns>
        private static bool FixShader(Material material)
        {
            Shader usedShader = material.shader;

            if (!usedShader || !IsMockName(usedShader.name, out string cleanedShaderName, out List<string> childNames))
            {
                return true;
            }

            Shader realShader = PrefabManager.Cache.GetPrefab<Shader>(cleanedShaderName);

            if (realShader)
            {
                material.shader = realShader;
            }
            else
            {
                if (allVanillaObjectsAvailable)
                {
                    Logger.LogWarning($"Could not find shader {usedShader.name}");
                }

                return false;
            }

            return true;
        }

        private static bool TryFindAssetOfComponent(Component unityObject, Type objectType, out Object asset)
        {
            var type = unityObject.GetType();
            ClassMember classMember = ClassMember.GetClassMember(type);

            foreach (var member in classMember.Members)
            {
                if (member.MemberType == objectType && member.HasGetMethod)
                {
                    asset = (Object)member.GetValue(unityObject);
                    if (asset != null)
                    {
                        return asset;
                    }
                }
            }

            asset = null;
            return false;
        }

        private static bool TryFindAssetInSelfOrChildComponents(GameObject unityObject, Type objectType, out Object asset)
        {
            if (unityObject == null)
            {
                asset = null;
                return false;
            }

            foreach (var component in unityObject.GetComponents<Component>())
            {
                if (!(component is Transform))
                {
                    if (TryFindAssetOfComponent(component, objectType, out asset))
                    {
                        return (bool)asset;
                    }
                }
            }

            foreach (Transform tf in unityObject.transform)
            {
                if (TryFindAssetInSelfOrChildComponents(tf.gameObject, objectType, out asset))
                {
                    return (bool)asset;
                }
            }

            asset = null;
            return false;
        }
    }
}
