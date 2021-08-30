using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

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
            On.VisEquipment.SetRightHandEquiped += VisEquipment_SetRightHandEquiped;
            On.VisEquipment.SetRightBackItem += VisEquipment_SetRightBackItem;
            On.VisEquipment.SetBackEquiped += VisEquipment_SetBackEquiped;
            On.VisEquipment.SetChestItem += VisEquipment_SetChestItem;
            On.VisEquipment.SetChestEquiped += VisEquipment_SetChestEquiped;
        }
        
        private void OnDestroy()
        {
            On.VisEquipment.UpdateEquipmentVisuals -= VisEquipment_UpdateEquipmentVisuals;
            On.VisEquipment.SetRightItem -= VisEquipment_SetRightItem;
            On.VisEquipment.SetRightHandEquiped -= VisEquipment_SetRightHandEquiped;
            On.VisEquipment.SetRightBackItem -= VisEquipment_SetRightBackItem;
            On.VisEquipment.SetBackEquiped -= VisEquipment_SetBackEquiped;
            On.VisEquipment.SetChestItem -= VisEquipment_SetChestItem;
            On.VisEquipment.SetChestEquiped -= VisEquipment_SetChestEquiped;
        }
        
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
        
        private bool VisEquipment_SetRightHandEquiped(On.VisEquipment.orig_SetRightHandEquiped orig, VisEquipment self, int hash)
        {
            //TODO: needs transpiler here
            if (self.m_currentRightItemHash == hash && CurrentRightItemVariant == NewRightItemVariant)
            {
                return orig(self, hash);
            }
            if (self.m_rightItemInstance)
            {
                Destroy(self.m_rightItemInstance);
                self.m_rightItemInstance = null;
            }
            self.m_currentRightItemHash = hash;
            CurrentRightItemVariant = NewRightItemVariant;
            if (hash != 0)
            {
                //TODO: Hook AttachItem and inject our variant
                self.m_rightItemInstance = self.AttachItem(hash, CurrentRightItemVariant, self.m_rightHand);
            }
            return true;
        }
        
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

        private bool VisEquipment_SetBackEquiped(On.VisEquipment.orig_SetBackEquiped orig, VisEquipment self, int leftItem, int rightItem, int leftVariant)
        {
            //TODO: needs transpiler here
            if (self.m_currentLeftBackItemHash == leftItem && 
                self.m_currentRightBackItemHash == rightItem && 
                self.m_currentLeftBackItemVariant == leftVariant && 
                CurrentRightBackItemVariant == NewRightBackItemVariant)
            {
                return orig(self, leftItem, rightItem, leftVariant);
            }
            if (self.m_leftBackItemInstance)
            {
                Object.Destroy(self.m_leftBackItemInstance);
                self.m_leftBackItemInstance = null;
            }
            if (self.m_rightBackItemInstance)
            {
                Object.Destroy(self.m_rightBackItemInstance);
                self.m_rightBackItemInstance = null;
            }
            self.m_currentLeftBackItemHash = leftItem;
            self.m_currentRightBackItemHash = rightItem;
            self.m_currentLeftBackItemVariant = leftVariant;
            CurrentRightBackItemVariant = NewRightBackItemVariant;
            if (self.m_currentLeftBackItemHash != 0)
            {
                self.m_leftBackItemInstance = self.AttachBackItem(leftItem, leftVariant, rightHand: false);
            }
            if (self.m_currentRightBackItemHash != 0)
            {
                self.m_rightBackItemInstance = self.AttachBackItem(rightItem, CurrentRightBackItemVariant, rightHand: true);
            }
            return true;
        }

        private void VisEquipment_SetChestItem(On.VisEquipment.orig_SetChestItem orig, VisEquipment self, string name)
        {
            if (MyHumanoid && MyHumanoid.m_chestItem != null &&
                !(self.m_chestItem == name && MyHumanoid.m_chestItem.m_variant == CurrentChestVariant))
            {
                NewChestVariant = MyHumanoid.m_chestItem.m_variant;
                if (self.m_nview && self.m_nview.GetZDO() is ZDO zdo)
                {
                    zdo.Set("ChestItemVariant", (!string.IsNullOrEmpty(name)) ? NewRightItemVariant : 0);
                }
            }

            orig(self, name);
        }
        
        private bool VisEquipment_SetChestEquiped(On.VisEquipment.orig_SetChestEquiped orig, VisEquipment self, int hash)
        {
            //TODO: needs transpiler here
            if (self.m_currentChestItemHash == hash && CurrentChestVariant == NewChestVariant)
            {
                return orig(self, hash);
            }
            self.m_currentChestItemHash = hash;
            CurrentChestVariant = NewChestVariant;
            if (self.m_bodyModel == null)
            {
                return true;
            }
            if (self.m_chestItemInstances != null)
            {
                foreach (GameObject chestItemInstance in self.m_chestItemInstances)
                {
                    if (self.m_lodGroup)
                    {
                        global::Utils.RemoveFromLodgroup(self.m_lodGroup, chestItemInstance);
                    }
                    Object.Destroy(chestItemInstance);
                }
                self.m_chestItemInstances = null;
                self.m_bodyModel.material.SetTexture("_ChestTex", self.m_emptyBodyTexture);
                self.m_bodyModel.material.SetTexture("_ChestBumpMap", null);
                self.m_bodyModel.material.SetTexture("_ChestMetal", null);
            }
            if (self.m_currentChestItemHash == 0)
            {
                return true;
            }
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
            if (itemPrefab == null)
            {
                ZLog.Log("Missing chest item " + hash);
                return true;
            }
            ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
            if ((bool)component.m_itemData.m_shared.m_armorMaterial)
            {
                self.m_bodyModel.material.SetTexture("_ChestTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestTex"));
                self.m_bodyModel.material.SetTexture("_ChestBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestBumpMap"));
                self.m_bodyModel.material.SetTexture("_ChestMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestMetal"));
            }
            //TODO: hook AttachArmor and inject our variant
            self.m_chestItemInstances = self.AttachArmor(hash, CurrentChestVariant);
            return true;
        }
    }
}
