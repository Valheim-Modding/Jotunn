﻿using UnityEngine;
using System.Collections.Generic;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Original code from https://github.com/GoldenJude.
    /// </summary>
    public static class BoneReorder
    {
        private static bool utilityChanged = false,
            chestChanged = false,
            helmetChanged = false,
            legChanged = false,
            shoulderChanged = false,
            applied = false;
        
        /// <summary>
        ///     Corrects any bone disorder caused by unity incorrectly importing ripped assets.
        ///     Once enabled, bone reordering will occur whenever the equipment changes.
        ///     If one plug-in requests application of reordering, it will be applied globally for all EquipmentChanged events.
        /// </summary>
        public static void ApplyOnEquipmentChanged()
        {
            if (!applied)
            {
                On.VisEquipment.SetUtilityEquiped += VisEquipmentOnSetUtilityEquiped;
                On.VisEquipment.SetShoulderEquiped += VisEquipmentOnSetShoulderEquiped;
                On.VisEquipment.SetChestEquiped += VisEquipmentOnSetChestEquiped;
                On.VisEquipment.SetHelmetEquiped += VisEquipmentOnSetHelmetEquiped;
                On.VisEquipment.SetLegEquiped += VisEquipmentOnSetLegEquiped;

                applied = true;
            }
        }

        /// <summary>
        ///     The state of reordering bones OnEquipmentChanged.
        /// </summary>
        /// <returns>Returns true when bone reordering is enabled.</returns>
        public static bool IsReorderingEnabled()
        {
            return applied;
        }

        private static bool VisEquipmentOnSetLegEquiped(On.VisEquipment.orig_SetLegEquiped orig, VisEquipment self, int hash)
        {
            var changed = self.m_currentLegItemHash == hash;
            if (changed) legChanged = false;
            else legChanged = true;
            var ret = orig(self, hash);
            if (legChanged && self.m_legItemInstances != null) ReorderBones(self, hash, self.m_legItemInstances);
            return ret;
        }

        private static bool VisEquipmentOnSetHelmetEquiped(On.VisEquipment.orig_SetHelmetEquiped orig, VisEquipment self, int hash, int hairhash)
        {
            var changed = self.m_currentHelmetItemHash == hash;
            if (changed) helmetChanged = false;
            else helmetChanged = true;
            var ret = orig(self, hash, hairhash);
            if (helmetChanged && self.m_helmetItemInstance != null)
                ReorderBones(self, hash, new List<GameObject>{self.m_helmetItemInstance}); //This is a single object instead of a collection, because reasons?
            return ret;
        }

        private static bool VisEquipmentOnSetChestEquiped(On.VisEquipment.orig_SetChestEquiped orig, VisEquipment self, int hash)
        {
            var changed = self.m_currentChestItemHash == hash;
            if (changed) chestChanged = false;
            else chestChanged = true;
            var ret = orig(self, hash);
            if (chestChanged && self.m_chestItemInstances != null) ReorderBones(self, hash, self.m_chestItemInstances);
            return ret;
        }

        private static bool VisEquipmentOnSetShoulderEquiped(On.VisEquipment.orig_SetShoulderEquiped orig, VisEquipment self, int hash, int variant)
        {
            var changed = self.m_currentShoulderItemHash == hash;
            if (changed) shoulderChanged = false;
            else shoulderChanged = true;
            var ret = orig(self, hash, variant);
            if(shoulderChanged && self.m_shoulderItemInstances != null) ReorderBones(self, hash, self.m_shoulderItemInstances);
            return ret;
        }
        
        private static bool VisEquipmentOnSetUtilityEquiped(On.VisEquipment.orig_SetUtilityEquiped orig, VisEquipment self, int hash)
        {
            var changed = self.m_currentUtilityItemHash == hash;
            if (changed) utilityChanged = false;
            else utilityChanged = true;
            var ret = orig(self, hash);
            if(utilityChanged && self.m_utilityItemInstances != null) ReorderBones(self, hash, self.m_utilityItemInstances);
            return ret;
        }

        /// <summary>
        ///     Reorders bone ordering caused by importing ripped assets into unity.
        ///     It effectively matches the bone ordering from the ItemPrefab (itemPrefabHash parameter).
        /// </summary>
        /// <param name="visEquipment"></param>
        /// <param name="itemPrefabHash"></param>
        /// <param name="instancesToFix">GameObjects that need to match the ordering from the ItemPrefab (itemPrefabHash parameter)</param>
        private static void ReorderBones(VisEquipment visEquipment, int itemPrefabHash, List<GameObject> instancesToFix)
        {
            if (visEquipment != null)
            {
                Transform skeletonRoot = visEquipment.transform.Find("Visual/Armature/Hips");
                GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemPrefabHash);
                if (!skeletonRoot || !itemPrefab)
                {
                    Logger.LogDebug($"Prefab missing components. Skipping {itemPrefab} {skeletonRoot}");
                    return;
                }
                Logger.LogDebug($"Reordering bones");
                int childCount = itemPrefab.transform.childCount;
                int num = 0;
                for (var i = 0; i < childCount; i++)
                {
                    var itemPrefabChilds = itemPrefab.transform.GetChild(i);
                    if (itemPrefabChilds.name.StartsWith("attach_skin"))
                    {
                        int j = 0;
                        var meshRenderersToReorder = instancesToFix[num].GetComponentsInChildren<SkinnedMeshRenderer>();
                        foreach (var meshRenderer in itemPrefabChilds.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            var meshRendererThatNeedFix = meshRenderersToReorder[j];
                            meshRendererThatNeedFix.SetBones(meshRenderer.GetBoneNames(), skeletonRoot);
                            j++;
                        }
                        num++;
                    }
                }
            }
        }

        /// <summary>
        /// Reorders incorrect bone ordering caused by importing ripped assets into unity.
        /// </summary>
        /// <param name="skinnedMeshRenderer"></param>
        /// <param name="boneNames"></param>
        /// <param name="skeletonRoot"></param>
        private static void SetBones(this SkinnedMeshRenderer skinnedMeshRenderer, string[] boneNames, Transform skeletonRoot)
        {
            var bones = new Transform[skinnedMeshRenderer.bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i] = FindInChildren(skeletonRoot, boneNames[i]);
            }

            skinnedMeshRenderer.bones = bones;
            skinnedMeshRenderer.rootBone = skeletonRoot;
        }

        /// <summary>
        ///     Returns a list of bone names, given a SkinnedMeshRenderer.
        /// </summary>
        /// <param name="skinnedMeshRenderer"></param>
        /// <returns></returns>
        private static string[] GetBoneNames(this SkinnedMeshRenderer skinnedMeshRenderer)
        {
            var list = new List<string>();
            foreach (var transform in skinnedMeshRenderer.bones)
            {
                list.Add(transform.name);
            }

            return list.ToArray();
        }

        /// <summary>
        ///     Returns a transform matching the given name within the transforms children.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Transform FindInChildren(Transform transform, string name)
        {
            Transform result;
            if (transform.name == name)
            {
                result = transform;
            }
            else
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTransform = FindInChildren(transform.GetChild(i), name);
                    if (childTransform != null)
                    {
                        return childTransform;
                    }
                }

                result = null;
            }

            return result;
        }
    }
}
