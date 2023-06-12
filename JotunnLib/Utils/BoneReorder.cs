using System;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Original code from https://github.com/GoldenJude.
    /// </summary>
    public static class BoneReorder
    {
        private static bool applied = false;

        /// <summary>
        ///     Corrects any bone disorder caused by unity incorrectly importing ripped assets.
        ///     Once enabled, bone reordering will occur whenever the equipment changes.
        ///     If one plug-in requests application of reordering, it will be applied globally for all EquipmentChanged events.
        /// </summary>
        public static void ApplyOnEquipmentChanged()
        {
            if (!applied)
            {
                Main.Harmony.PatchAll(typeof(BoneReorder));
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

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLegEquipped)), HarmonyPostfix]
        private static void VisEquipmentOnSetLegEquiped(VisEquipment __instance, int hash, ref bool __result)
        {
            if (__result && __instance.m_legItemInstances != null)
            {
                ReorderBones(__instance, hash, __instance.m_legItemInstances);
            }
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetHelmetEquipped)), HarmonyPostfix]

        private static void VisEquipmentOnSetHelmetEquiped(VisEquipment __instance, int hash, int hairHash, ref bool __result)
        {
            if (__result && __instance.m_helmetItemInstance != null)
            {
                ReorderBones(__instance, hash, new List<GameObject>{__instance.m_helmetItemInstance}); //This is a single object instead of a collection, because reasons?
            }
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestEquipped)), HarmonyPostfix]
        private static void VisEquipmentOnSetChestEquiped(VisEquipment __instance, int hash, ref bool __result)
        {
            if (__result && __instance.m_chestItemInstances != null)
            {
                ReorderBones(__instance, hash, __instance.m_chestItemInstances);
            }
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetShoulderEquipped)), HarmonyPostfix]
        private static void VisEquipmentOnSetShoulderEquiped(VisEquipment __instance, int hash, int variant, ref bool __result)
        {
            if (__result && __instance.m_shoulderItemInstances != null)
            {
                ReorderBones(__instance, hash, __instance.m_shoulderItemInstances);
            }
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetUtilityEquipped)), HarmonyPostfix]
        private static void VisEquipmentOnSetUtilityEquiped(VisEquipment __instance, int hash, ref bool __result)
        {
            if (__result && __instance.m_utilityItemInstances != null)
            {
                ReorderBones(__instance, hash, __instance.m_utilityItemInstances);
            }
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
                try
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
                            var meshRenderersToReorder = instancesToFix[num].GetComponentsInChildren<SkinnedMeshRenderer>(true);
                            foreach (var meshRenderer in itemPrefabChilds.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                            {
                                var meshRendererThatNeedFix = meshRenderersToReorder[j];
                                meshRendererThatNeedFix.SetBones(meshRenderer.GetBoneNames(), skeletonRoot);
                                j++;
                            }
                            num++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Exception caught while reordering bones: {ex}");
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
