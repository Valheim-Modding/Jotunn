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
        public static KitbashManager Instance
        {
            get
            {
                if (_instance == null) _instance = new KitbashManager();
                return _instance;
            }
        }

        /// <summary>
        ///     Internal list of objects to which KitBashing should be applied.
        /// </summary>
        private readonly List<KitbashObject> KitBashObjects = new List<KitbashObject>();

        /// <summary>
        ///     Registers all hooks.
        /// </summary>
        public void Init()
        {
            ItemManager.OnKitbashItemsAvailable += ApplyKitBashes;
        }

        /// <summary>
        ///     Register a prefab with a KitbashConfig to be applied when the vanilla prefabs are available
        /// </summary>
        /// <param name="prefab">Prefab to add kitbashed parts to</param>
        /// <param name="kitBashConfig">KitbashConfig to apply to the prefab</param>
        /// <returns>The KitbashObject container for this prefab</returns>
        public KitbashObject AddKitbash(GameObject prefab, KitbashConfig kitBashConfig)
        {
            if(prefab.transform.parent == null)
            {
                string name = prefab.name;
                prefab = Object.Instantiate(prefab, PrefabManager.Instance.PrefabContainer.transform);
                prefab.name = name;
            }
            KitbashObject kitBashObject = new KitbashObject
            {
                Config = kitBashConfig,
                Prefab = prefab
            };
            KitBashObjects.Add(kitBashObject);
            return kitBashObject;
        }

        /// <summary>
        ///     Apply all KitBashs to the objects registered in the manager.
        /// </summary>
        private void ApplyKitBashes()
        {
            if (KitBashObjects.Count > 0)
            {
                Logger.LogInfo($"---- Applying KitBash in {KitBashObjects.Count} objects ----");

                foreach (KitbashObject kitBashObject in KitBashObjects)
                {
                    try
                    {
                        if (kitBashObject.Config.FixReferences)
                        {
                            kitBashObject.Prefab.FixReferences();
                        }
                        ApplyKitbash(kitBashObject);
                            
                        Logger.LogInfo($"Kitbash for {kitBashObject.Prefab} applied");
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }

                ItemManager.OnKitbashItemsAvailable -= ApplyKitBashes;
            }
        }

        /// <summary>
        ///     Apply kitbash to a single object.
        /// </summary>
        /// <param name="kitBashObject"></param>
        /// <returns></returns>
        private bool ApplyKitbash(KitbashObject kitBashObject)
        {
            foreach (KitbashSourceConfig config in kitBashObject.Config.KitbashSources)
            {
                if (!Instance.Kitbash(kitBashObject.Prefab, config))
                {
                    return false;
                }
            }
            if (kitBashObject.Config.Layer != null)
            {
                int layer = LayerMask.NameToLayer(kitBashObject.Config.Layer);
                foreach (Transform transform in kitBashObject.Prefab.GetComponentsInChildren<Transform>())
                {
                    transform.gameObject.layer = layer;
                }
            }
            kitBashObject.OnKitbashApplied?.SafeInvoke();
            return true;
        }

        private bool Kitbash(GameObject kitbashedPrefab, KitbashSourceConfig config)
        {
            // Try to load a custom prefab
            GameObject sourcePrefab = PrefabManager.Instance.GetPrefab(config.SourcePrefab);

            // If not custom, try to get a vanilla prefab from the cache
            if (!sourcePrefab)
            {
                sourcePrefab = PrefabManager.Cache.GetPrefab<GameObject>(config.SourcePrefab);
            }

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
            GameObject kitBashObject = Object.Instantiate(sourceGameObject, parentTransform);

            kitBashObject.name = config.Name ?? sourceGameObject.name;
            kitBashObject.transform.localPosition = config.Position;
            kitBashObject.transform.localRotation = config.Rotation;
            kitBashObject.transform.localScale = config.Scale;

            if (config.Materials != null)
            {
                Material[] sourceMaterials = GetSourceMaterials(config);

                if (sourceMaterials == null)
                {
                    Logger.LogWarning($"No materials found for {config}");
                    return false;
                }
                   
                SkinnedMeshRenderer[] targetSkinnedMeshRenderers = kitBashObject.GetComponentsInChildren<SkinnedMeshRenderer>();

                foreach (SkinnedMeshRenderer targetSkinnedMeshRenderer in targetSkinnedMeshRenderers)
                {
                    targetSkinnedMeshRenderer.sharedMaterials = sourceMaterials;
                    targetSkinnedMeshRenderer.materials = sourceMaterials;
                }

                MeshRenderer[] targetMeshRenderers = kitBashObject.GetComponentsInChildren<MeshRenderer>();
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
