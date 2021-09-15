using System;
using System.Collections.Generic;
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
                On.VisEquipment.Awake += VisEquipment_Awake;

                On.VisEquipment.UpdateEquipmentVisuals += VisEquipment_UpdateEquipmentVisuals;
                IL.VisEquipment.SetRightHandEquiped += VisEquipment_SetRightHandEquiped;
                IL.VisEquipment.SetBackEquiped += VisEquipment_SetBackEquiped;
                IL.VisEquipment.SetChestEquiped += VisEquipment_SetChestEquiped;

                On.VisEquipment.SetRightItem += VisEquipment_SetRightItem;
                On.VisEquipment.SetRightBackItem += VisEquipment_SetRightBackItem;
                On.VisEquipment.SetChestItem += VisEquipment_SetChestItem;

                Enabled = true;
            }
        }

        private static void VisEquipment_Awake(On.VisEquipment.orig_Awake orig, VisEquipment self)
        {
            orig(self);
            self.gameObject.AddComponent<ExtEquipment>();
        }

        /// <summary>
        ///     Get non-vanilla variant indices from the ZDO
        /// </summary>
        private static void VisEquipment_UpdateEquipmentVisuals(On.VisEquipment.orig_UpdateEquipmentVisuals orig, VisEquipment self)
        {
            if (self.m_nview && self.m_nview.GetZDO() is ZDO zdo)
            {
                if (Instances.TryGetValue(self, out var instance))
                {
                    instance.NewRightItemVariant = zdo.GetInt("RightItemVariant");
                    instance.NewChestVariant = zdo.GetInt("ChestItemVariant");
                    instance.NewRightBackItemVariant = zdo.GetInt("RightBackItemVariant");
                }
            }

            orig(self);
        }

        /// <summary>
        ///     Check for variant changes and pass the variant to AttachItem
        /// </summary>
        private static void VisEquipment_SetRightHandEquiped(MonoMod.Cil.ILContext il)
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
        private static void VisEquipment_SetBackEquiped(MonoMod.Cil.ILContext il)
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
        private static void VisEquipment_SetRightItem(On.VisEquipment.orig_SetRightItem orig, VisEquipment self, string name)
        {
            if (Instances.TryGetValue(self, out var instance) &&
                instance.MyHumanoid && instance.MyHumanoid.m_rightItem != null &&
                !(self.m_rightItem == name && instance.MyHumanoid.m_rightItem.m_variant == instance.CurrentRightItemVariant))
            {
                instance.NewRightItemVariant = instance.MyHumanoid.m_rightItem.m_variant;
                if (self.m_nview && self.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set("RightItemVariant", (!string.IsNullOrEmpty(name)) ? instance.NewRightItemVariant : 0);
                }
            }

            orig(self, name);
        }

        /// <summary>
        ///     Store the variant index of the right back item to the ZDO if the variant has changed
        /// </summary>
        private static void VisEquipment_SetRightBackItem(On.VisEquipment.orig_SetRightBackItem orig, VisEquipment self, string name)
        {
            if (Instances.TryGetValue(self, out var instance) &&
                instance.MyHumanoid && instance.MyHumanoid.m_hiddenRightItem != null &&
                !(self.m_rightBackItem == name && instance.MyHumanoid.m_hiddenRightItem.m_variant == instance.CurrentRightBackItemVariant))
            {
                instance.NewRightBackItemVariant = instance.MyHumanoid.m_hiddenRightItem.m_variant;
                if (self.m_nview && self.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set("RightBackItemVariant", (!string.IsNullOrEmpty(name)) ? instance.NewRightBackItemVariant : 0);
                }
            }

            orig(self, name);
        }

        /// <summary>
        ///     Store the variant index of the chest item to the ZDO if the variant has changed
        /// </summary>
        private static void VisEquipment_SetChestItem(On.VisEquipment.orig_SetChestItem orig, VisEquipment self, string name)
        {
            if (Instances.TryGetValue(self, out var instance) &&
                instance.MyHumanoid && instance.MyHumanoid.m_chestItem != null &&
                !(self.m_chestItem == name && instance.MyHumanoid.m_chestItem.m_variant == instance.CurrentChestVariant))
            {
                instance.NewChestVariant = instance.MyHumanoid.m_chestItem.m_variant;
                if (self.m_nview && self.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set("ChestItemVariant", (!string.IsNullOrEmpty(name)) ? instance.NewChestVariant : 0);
                }
            }

            orig(self, name);
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
