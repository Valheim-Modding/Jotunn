using System;
using System.Collections.Generic;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace Jotunn.Utils
{
    internal class ExtEquipment : MonoBehaviour
    {
        private static bool Enabled;

        private static readonly Dictionary<VisEquipment, ExtEquipment> Instances =
            new Dictionary<VisEquipment, ExtEquipment>();

        public static void Enable()
        {
            if (!Enabled)
            {
                Enabled = true;
                Main.Harmony.PatchAll(typeof(ExtEquipment));
            }
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.Awake)), HarmonyPostfix]
        private static void VisEquipment_Awake(VisEquipment __instance)
        {
            if (!__instance.gameObject.TryGetComponent(out ExtEquipment _))
            {
                __instance.gameObject.AddComponent<ExtEquipment>();
            }
        }

        /// <summary>
        ///     Get non-vanilla variant indices from the ZDO
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals)), HarmonyPrefix]
        private static void VisEquipment_UpdateEquipmentVisuals(VisEquipment __instance)
        {
            if (__instance.m_nview && __instance.m_nview.GetZDO() is ZDO zdo)
            {
                if (Instances.TryGetValue(__instance, out var instance))
                {
                    instance.NewRightItemVariant = zdo.GetInt("RightItemVariant");
                    instance.NewChestVariant = zdo.GetInt("ChestItemVariant");
                    instance.NewRightBackItemVariant = zdo.GetInt("RightBackItemVariant");
                }
            }
        }

        /// <summary>
        ///     Check for variant changes and pass the variant to AttachItem
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightHandEquipped)), HarmonyILManipulator]
        private static void VisEquipment_SetRightHandEquiped(ILContext il)
        {
            ExtEquipment instance = null;

            ILCursor c = new ILCursor(il);

            // Change hash if variant is different
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(1)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, VisEquipment, int>>((hash, self) =>
                {
                    if (Instances.TryGetValue(self, out instance) && hash != 0 &&
                        instance.CurrentRightItemVariant != instance.NewRightItemVariant)
                    {
                        instance.CurrentRightItemVariant = instance.NewRightItemVariant;
                        return hash + instance.CurrentRightItemVariant;
                    }
                    return hash;
                });

            }

            // Pass current variant to AttachItem
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(1),
                x => x.MatchLdcI4(0)))
            {
                c.EmitDelegate<Func<int, int>>(variant => (instance != null) ? instance.CurrentRightItemVariant : variant);
            }
        }

        /// <summary>
        ///     Check for variant changes and pass the variant to AttachBackItem
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetBackEquipped)), HarmonyILManipulator]
        private static void VisEquipment_SetBackEquiped(ILContext il)
        {
            ExtEquipment instance = null;

            ILCursor c = new ILCursor(il);

            // Change hash if variant is different
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(2)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, VisEquipment, int>>((hash, self) =>
                {
                    if (Instances.TryGetValue(self, out instance) && hash != 0 &&
                        instance.CurrentRightBackItemVariant != instance.NewRightBackItemVariant)
                    {
                        instance.CurrentRightBackItemVariant = instance.NewRightBackItemVariant;
                        return hash + instance.CurrentRightBackItemVariant;
                    }
                    return hash;
                });
            }

            // Pass current variant to AttachItem
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(2),
                x => x.MatchLdcI4(0)))
            {
                c.EmitDelegate<Func<int, int>>(variant => (instance != null) ? instance.CurrentRightBackItemVariant : variant);
            }
        }

        /// <summary>
        ///     Check for variant changes and pass the variant to AttachArmor
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestEquipped)), HarmonyILManipulator]
        private static void VisEquipment_SetChestEquiped(ILContext il)
        {
            ExtEquipment instance = null;

            ILCursor c = new ILCursor(il);

            // Change hash if variant is different
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(1)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, VisEquipment, int>>((hash, self) =>
                {
                    if (Instances.TryGetValue(self, out instance) && hash != 0 &&
                        instance.CurrentChestVariant != instance.NewChestVariant)
                    {
                        instance.CurrentChestVariant = instance.NewChestVariant;
                        return hash + instance.CurrentChestVariant;
                    }
                    return hash;
                });
            }

            // Pass current variant to AttachArmor
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(1),
                x => x.MatchLdcI4(-1)))
            {
                c.EmitDelegate<Func<int, int>>(variant => (instance != null) ? instance.CurrentChestVariant : variant);
            }
        }

        /// <summary>
        ///     Store the variant index of the right hand item to the ZDO if the variant has changed
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightItem)), HarmonyPrefix]
        private static void VisEquipment_SetRightItem(VisEquipment __instance, string name)
        {
            if (Instances.TryGetValue(__instance, out var instance) &&
                instance.MyHumanoid && instance.MyHumanoid.m_rightItem != null &&
                !(__instance.m_rightItem == name && instance.MyHumanoid.m_rightItem.m_variant == instance.CurrentRightItemVariant))
            {
                instance.NewRightItemVariant = instance.MyHumanoid.m_rightItem.m_variant;
                if (__instance.m_nview && __instance.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set("RightItemVariant", (!string.IsNullOrEmpty(name)) ? instance.NewRightItemVariant : 0);
                }
            }
        }

        /// <summary>
        ///     Store the variant index of the right back item to the ZDO if the variant has changed
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightBackItem)), HarmonyPrefix]
        private static void VisEquipment_SetRightBackItem(VisEquipment __instance, string name)
        {
            if (Instances.TryGetValue(__instance, out var instance) &&
                instance.MyHumanoid && instance.MyHumanoid.m_hiddenRightItem != null &&
                !(__instance.m_rightBackItem == name && instance.MyHumanoid.m_hiddenRightItem.m_variant == instance.CurrentRightBackItemVariant))
            {
                instance.NewRightBackItemVariant = instance.MyHumanoid.m_hiddenRightItem.m_variant;
                if (__instance.m_nview && __instance.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set("RightBackItemVariant", (!string.IsNullOrEmpty(name)) ? instance.NewRightBackItemVariant : 0);
                }
            }
        }

        /// <summary>
        ///     Store the variant index of the chest item to the ZDO if the variant has changed
        /// </summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestItem)), HarmonyPrefix]
        private static void VisEquipment_SetChestItem(VisEquipment __instance, string name)
        {
            if (Instances.TryGetValue(__instance, out var instance) &&
                instance.MyHumanoid && instance.MyHumanoid.m_chestItem != null &&
                !(__instance.m_chestItem == name && instance.MyHumanoid.m_chestItem.m_variant == instance.CurrentChestVariant))
            {
                instance.NewChestVariant = instance.MyHumanoid.m_chestItem.m_variant;
                if (__instance.m_nview && __instance.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set("ChestItemVariant", (!string.IsNullOrEmpty(name)) ? instance.NewChestVariant : 0);
                }
            }
        }

        private Humanoid MyHumanoid;

        private int NewRightItemVariant;
        private int CurrentRightItemVariant;
        private int NewRightBackItemVariant;
        private int CurrentRightBackItemVariant;
        private int NewChestVariant;
        private int CurrentChestVariant;

        private void Awake()
        {
            MyHumanoid = gameObject.GetComponent<Humanoid>();
            Instances.Add(gameObject.GetComponent<VisEquipment>(), this);
        }

        private void OnDestroy()
        {
            Instances.Remove(gameObject.GetComponent<VisEquipment>());
        }
    }
}
