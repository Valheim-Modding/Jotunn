using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace Jotunn.Utils
{
    internal class ExtEquipment : MonoBehaviour
    {
        private static bool Enabled;

        public static void Enable()
        {
            if (!Enabled)
            {
                On.VisEquipment.Awake += VisEquipment_Awake;
                Enabled = true;
            }
        }

        private static void VisEquipment_Awake(On.VisEquipment.orig_Awake orig, VisEquipment self)
        {
            orig(self);
            self.gameObject.AddComponent<ExtEquipment>();
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
            On.VisEquipment.UpdateEquipmentVisuals += VisEquipment_UpdateEquipmentVisuals;
            On.VisEquipment.SetRightItem += VisEquipment_SetRightItem;
            IL.VisEquipment.SetRightHandEquiped += VisEquipment_SetRightHandEquiped;
            On.VisEquipment.SetRightBackItem += VisEquipment_SetRightBackItem;
            IL.VisEquipment.SetBackEquiped += VisEquipment_SetBackEquiped1;
            On.VisEquipment.SetChestItem += VisEquipment_SetChestItem;
            IL.VisEquipment.SetChestEquiped += VisEquipment_SetChestEquiped;
        }

        private void OnDestroy()
        {
            On.VisEquipment.UpdateEquipmentVisuals -= VisEquipment_UpdateEquipmentVisuals;
            On.VisEquipment.SetRightItem -= VisEquipment_SetRightItem;
            IL.VisEquipment.SetRightHandEquiped -= VisEquipment_SetRightHandEquiped;
            On.VisEquipment.SetRightBackItem -= VisEquipment_SetRightBackItem;
            IL.VisEquipment.SetBackEquiped -= VisEquipment_SetBackEquiped1;
            On.VisEquipment.SetChestItem -= VisEquipment_SetChestItem;
            IL.VisEquipment.SetChestEquiped -= VisEquipment_SetChestEquiped;
        }

        /// <summary>
        ///     Get non-vanilla variant indices from the ZDO
        /// </summary>
        private void VisEquipment_UpdateEquipmentVisuals(On.VisEquipment.orig_UpdateEquipmentVisuals orig, VisEquipment self)
        {
            if (self.m_nview && self.m_nview.GetZDO() is ZDO zdo)
            {
                NewRightItemVariant = zdo.GetInt("RightItemVariant");
                NewChestVariant = zdo.GetInt("ChestItemVariant");
                NewRightBackItemVariant = zdo.GetInt("RightBackItemVariant");
            }

            orig(self);
        }

        /// <summary>
        ///     Store the variant index of the right hand item to the ZDO if the variant has changed
        /// </summary>
        private void VisEquipment_SetRightItem(On.VisEquipment.orig_SetRightItem orig, VisEquipment self, string name)
        {
            if (MyHumanoid && MyHumanoid.m_rightItem != null &&
                !(self.m_rightItem == name && MyHumanoid.m_rightItem.m_variant == CurrentRightItemVariant))
            {
                NewRightItemVariant = MyHumanoid.m_rightItem.m_variant;
                if (self.m_nview && self.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set("RightItemVariant", (!string.IsNullOrEmpty(name)) ? NewRightItemVariant : 0);
                }
            }

            orig(self, name);
        }

        /// <summary>
        ///     Check for variant changes and pass the variant to AttachItem
        /// </summary>
        private void VisEquipment_SetRightHandEquiped(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Change hash if variant is different
            c.GotoNext(MoveType.After,
                x => x.OpCode.Code == Code.Ldarg_1);
            c.EmitDelegate<Func<int, int>>((hash) =>
            {
                if (hash != 0 && CurrentRightItemVariant != NewRightItemVariant)
                {
                    CurrentRightItemVariant = NewRightItemVariant;
                    return hash + 1;
                }
                return hash;
            });

            // Pass current variant to AttachItem
            c.GotoNext(MoveType.After,
                x => x.OpCode.Code == Code.Ldarg_0,
                x => x.OpCode.Code == Code.Ldarg_0,
                x => x.OpCode.Code == Code.Ldarg_1,
                x => x.OpCode.Code == Code.Ldc_I4_0);
            c.EmitDelegate<Func<int, int>>(variant => CurrentRightItemVariant);
        }

        /// <summary>
        ///     Store the variant index of the right back item to the ZDO if the variant has changed
        /// </summary>
        private void VisEquipment_SetRightBackItem(On.VisEquipment.orig_SetRightBackItem orig, VisEquipment self, string name)
        {
            if (MyHumanoid && MyHumanoid.m_hiddenRightItem != null &&
                !(self.m_rightBackItem == name && MyHumanoid.m_hiddenRightItem.m_variant == CurrentRightBackItemVariant))
            {
                NewRightBackItemVariant = MyHumanoid.m_hiddenRightItem.m_variant;
                if (self.m_nview && self.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set("RightBackItemVariant", (!string.IsNullOrEmpty(name)) ? NewRightBackItemVariant : 0);
                }
            }

            orig(self, name);
        }

        /// <summary>
        ///     Check for variant changes and pass the variant to AttachBackItem
        /// </summary>
        private void VisEquipment_SetBackEquiped1(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Change hash if variant is different
            c.GotoNext(MoveType.After,
                x => x.OpCode.Code == Code.Ldarg_2);
            c.EmitDelegate<Func<int, int>>((hash) =>
            {
                if (hash != 0 && CurrentRightBackItemVariant != NewRightBackItemVariant)
                {
                    CurrentRightBackItemVariant = NewRightBackItemVariant;
                    return hash + 1;
                }
                return hash;
            });

            // Pass current variant to AttachItem
            c.GotoNext(MoveType.After,
                x => x.OpCode.Code == Code.Ldarg_0,
                x => x.OpCode.Code == Code.Ldarg_0,
                x => x.OpCode.Code == Code.Ldarg_2,
                x => x.OpCode.Code == Code.Ldc_I4_0);
            c.EmitDelegate<Func<int, int>>(variant => CurrentRightBackItemVariant);
        }

        /// <summary>
        ///     Store the variant index of the chest item to the ZDO if the variant has changed
        /// </summary>
        private void VisEquipment_SetChestItem(On.VisEquipment.orig_SetChestItem orig, VisEquipment self, string name)
        {
            if (MyHumanoid && MyHumanoid.m_chestItem != null &&
                !(self.m_chestItem == name && MyHumanoid.m_chestItem.m_variant == CurrentChestVariant))
            {
                NewChestVariant = MyHumanoid.m_chestItem.m_variant;
                if (self.m_nview && self.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set("ChestItemVariant", (!string.IsNullOrEmpty(name)) ? NewChestVariant : 0);
                }
            }

            orig(self, name);
        }

        /// <summary>
        ///     Check for variant changes and pass the variant to AttachArmor
        /// </summary>
        private void VisEquipment_SetChestEquiped(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Change hash if variant is different
            c.GotoNext(MoveType.After,
                x => x.OpCode.Code == Code.Ldarg_1);
            c.EmitDelegate<Func<int, int>>((hash) =>
            {
                if (hash != 0 && CurrentChestVariant != NewChestVariant)
                {
                    CurrentChestVariant = NewChestVariant;
                    return hash + 1;
                }
                return hash;
            });

            // Pass current variant to AttachArmor
            c.GotoNext(MoveType.After,
                x => x.OpCode.Code == Code.Ldarg_0,
                x => x.OpCode.Code == Code.Ldarg_0,
                x => x.OpCode.Code == Code.Ldarg_1,
                x => x.OpCode.Code == Code.Ldc_I4_M1);
            c.EmitDelegate<Func<int, int>>(variant => CurrentChestVariant);
        }
    }
}
