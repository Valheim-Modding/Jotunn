using Jotunn.Configs;
using Jotunn.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for handling Kitbashed objects
    /// </summary>
    public class KitbashManager : IManager
    {   
        private static KitbashManager _instance;

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static KitbashManager Instance => _instance ??= new KitbashManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private KitbashManager() {}

        /// <summary>
        ///     Internal list of objects to which Kitbashing should be applied.
        /// </summary>
        private readonly List<KitbashObject> KitbashObjects = new List<KitbashObject>();

        /// <summary>
        ///     Registers all hooks.
        /// </summary>
        void IManager.Init()
        {
            ItemManager.OnKitbashItemsAvailable += ApplyKitbashes;
        }

        /// <summary>
        ///     Register a prefab with a KitbashConfig to be applied when the vanilla prefabs are available
        /// </summary>
        /// <param name="prefab">Prefab to add kitbashed parts to</param>
        /// <param name="kitbashConfig">KitbashConfig to apply to the prefab</param>
        /// <returns>The KitbashObject container for this prefab</returns>
        public KitbashObject AddKitbash(GameObject prefab, KitbashConfig kitbashConfig)
        {
            if(prefab.transform.parent == null)
            {
                string name = prefab.name;
                prefab = Object.Instantiate(prefab, PrefabManager.Instance.PrefabContainer.transform);
                prefab.name = name;
            }
            KitbashObject kitbashObject = new KitbashObject
            {
                Config = kitbashConfig,
                Prefab = prefab
            };
            KitbashObjects.Add(kitbashObject);
            return kitbashObject;
        }

        /// <summary>
        ///     Apply all Kitbashs to the objects registered in the manager.
        /// </summary>
        private void ApplyKitbashes()
        {
            if (KitbashObjects.Count > 0)
            {
                Logger.LogInfo($"Applying Kitbash in {KitbashObjects.Count} objects");

                foreach (KitbashObject kitbashObject in KitbashObjects)
                {
                    try
                    {
                        if (kitbashObject.Config.FixReferences)
                        {
                            kitbashObject.Prefab.FixReferences();
                        }
                        ApplyKitbash(kitbashObject);
                            
                        Logger.LogDebug($"Kitbash for {kitbashObject} applied");
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }

                ItemManager.OnKitbashItemsAvailable -= ApplyKitbashes;
            }
        }

        /// <summary>
        ///     Apply kitbash to a single object.
        /// </summary>
        /// <param name="kitbashObject"></param>
        /// <returns></returns>
        private bool ApplyKitbash(KitbashObject kitbashObject)
        {
            foreach (KitbashSourceConfig config in kitbashObject.Config.KitbashSources)
            {
                if (!Instance.Kitbash(kitbashObject.Prefab, config))
                {
                    return false;
                }
            }
            if (kitbashObject.Config.Layer != null)
            {
                int layer = LayerMask.NameToLayer(kitbashObject.Config.Layer);
                foreach (Transform transform in kitbashObject.Prefab.GetComponentsInChildren<Transform>())
                {
                    transform.gameObject.layer = layer;
                }
            }
            kitbashObject.OnKitbashApplied?.SafeInvoke();
            return true;
        }

        private bool Kitbash(GameObject kitbashedPrefab, KitbashSourceConfig config)
        {
            // Try to load a custom prefab
            GameObject sourcePrefab = PrefabManager.Instance.GetPrefab(config.SourcePrefab);

            // If no prefab is found, warn and return
            if (!sourcePrefab)
            {
                Logger.LogWarning($"No prefab found for {config}");
                return false;
            }
            GameObject sourceGameObject = sourcePrefab.transform.Find(config.SourcePath).gameObject;

            Transform parentTransform = config.TargetParentPath != null ? kitbashedPrefab.transform.Find(config.TargetParentPath) : kitbashedPrefab.transform;
            if (parentTransform == null)
            {
                Logger.LogWarning($"Target parent not found for {config}");
                return false;
            }
            GameObject kitbashObject = Object.Instantiate(sourceGameObject, parentTransform);

            kitbashObject.name = config.Name ?? sourceGameObject.name;
            kitbashObject.transform.localPosition = config.Position;
            kitbashObject.transform.localRotation = config.Rotation;
            kitbashObject.transform.localScale = config.Scale;

            if (config.Materials != null)
            {
                Material[] sourceMaterials = GetSourceMaterials(config);

                if (sourceMaterials == null)
                {
                    Logger.LogWarning($"No materials found for {config}");
                    return false;
                }
                   
                SkinnedMeshRenderer[] targetSkinnedMeshRenderers = kitbashObject.GetComponentsInChildren<SkinnedMeshRenderer>();

                foreach (SkinnedMeshRenderer targetSkinnedMeshRenderer in targetSkinnedMeshRenderers)
                {
                    targetSkinnedMeshRenderer.sharedMaterials = sourceMaterials;
                    targetSkinnedMeshRenderer.materials = sourceMaterials;
                }

                MeshRenderer[] targetMeshRenderers = kitbashObject.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer targetMeshRenderer in targetMeshRenderers)
                {
                    targetMeshRenderer.sharedMaterials = sourceMaterials;
                    targetMeshRenderer.materials = sourceMaterials;
                }

            }
            return true;
        }

        private Material[] GetSourceMaterials(KitbashSourceConfig config)
        {
            Material[] result = new Material[config.Materials.Length];
            for (int i = 0; i < config.Materials.Length; i++)
            {
                result[i] = (Material) PrefabManager.Cache.GetPrefab(typeof(Material), config.Materials[i]);
            }
            return result;
        }
    }
}
