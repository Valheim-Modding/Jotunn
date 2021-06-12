using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    public class KitbashManager
    {   
        private static KitbashManager _instance;
        private readonly List<KitbashObject> kitBashObjects = new List<KitbashObject>();

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

        private KitbashManager()
        {  
            PieceManager.OnPiecesRegistered += ApplyKitBashes;
        }

        private void ApplyKitBashes()
        {
            try
            { 
                if (kitBashObjects.Count == 0)
                {
                    return;
                }
                Jotunn.Logger.LogDebug("Applying KitBash in " + kitBashObjects.Count + " objects");
                foreach (KitbashObject kitBashObject in kitBashObjects)
                {
                    try
                    {
                        if (kitBashObject.Config.FixReferences)
                        {
                            kitBashObject.Prefab.FixReferences();
                        }
                        ApplyKitbash(kitBashObject);
                    }
                    catch (Exception e)
                    {
                        Jotunn.Logger.LogError(e);
                    }
                }
            }
            finally
            {
                PieceManager.OnPiecesRegistered -= ApplyKitBashes;
            }
        }

        private bool ApplyKitbash(KitbashObject kitBashObject)
        {
            Jotunn.Logger.LogDebug("Applying Kitbash for " + kitBashObject.Prefab);
            foreach (KitbashSourceConfig config in kitBashObject.Config.KitbashSources)
            {
                if (!KitbashManager.Instance.Kitbash(kitBashObject.Prefab, config))
                {
                    return false;
                }
            }
            if (kitBashObject.Config.layer != null)
            {
                int layer = LayerMask.NameToLayer(kitBashObject.Config.layer);
                foreach(Transform transform in kitBashObject.Prefab.GetComponentsInChildren<Transform>())
                {
                    transform.gameObject.layer = layer;
                }
            }
            kitBashObject.KitbashApplied?.Invoke();
            return true;
        }

        public KitbashObject Kitbash(GameObject prefab, KitbashConfig kitBashConfig)
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
            kitBashObjects.Add(kitBashObject);
            return kitBashObject;
        }

        private bool Kitbash(GameObject kitbashedPrefab, KitbashSourceConfig config)
        {
            GameObject sourcePrefab = PrefabManager.Instance.GetPrefab(config.sourcePrefab);
            if (!sourcePrefab)
            {
                Jotunn.Logger.LogWarning("No prefab found for " + config);
                return false;
            }
            GameObject sourceGameObject = sourcePrefab.transform.Find(config.sourcePath).gameObject;

            Transform parentTransform = config.targetParentPath != null ? kitbashedPrefab.transform.Find(config.targetParentPath) : kitbashedPrefab.transform;
            if (parentTransform == null)
            {
                Jotunn.Logger.LogWarning("Target parent not found for " + config);
                return false;
            }
            GameObject kitBashObject = Object.Instantiate(sourceGameObject, parentTransform);

            kitBashObject.name = config.name ?? sourceGameObject.name;
            kitBashObject.transform.localPosition = config.position;
            kitBashObject.transform.localRotation = config.rotation;
            kitBashObject.transform.localScale = config.scale;

            if (config.materials != null)
            {
                Material[] sourceMaterials = GetSourceMaterials(config);

                if (sourceMaterials == null)
                {
                    Jotunn.Logger.LogWarning("No materials for " + config);
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
            Material[] result = new Material[config.materials.Length];
            for (int i = 0; i < config.materials.Length; i++)
            {
                result[i] = (Material) PrefabManager.Cache.GetPrefab(typeof(Material), config.materials[i]);
            }
            return result;
        }
    }
}
