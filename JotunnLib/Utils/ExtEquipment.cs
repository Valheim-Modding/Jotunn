using System;
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

        private void Awake()
        {
            MyHumanoid = gameObject.GetComponent<Humanoid>();
            On.VisEquipment.SetRightItem += VisEquipment_SetRightItem;
            On.VisEquipment.UpdateEquipmentVisuals += VisEquipment_UpdateEquipmentVisuals;
            On.VisEquipment.SetRightHandEquiped += VisEquipment_SetRightHandEquiped;
        }

        private void OnDestroy()
        {
            On.VisEquipment.SetRightItem -= VisEquipment_SetRightItem;
            On.VisEquipment.UpdateEquipmentVisuals -= VisEquipment_UpdateEquipmentVisuals;
            On.VisEquipment.SetRightHandEquiped -= VisEquipment_SetRightHandEquiped;
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
        
        private void VisEquipment_UpdateEquipmentVisuals(On.VisEquipment.orig_UpdateEquipmentVisuals orig, VisEquipment self)
        {
            if (self.m_nview && self.m_nview.GetZDO() is ZDO zdo)
            {
                NewRightItemVariant = zdo.GetInt("RightItemVariant");
            }

            orig(self);
        }
    
        private bool VisEquipment_SetRightHandEquiped(On.VisEquipment.orig_SetRightHandEquiped orig, VisEquipment self, int hash)
        {
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
                self.m_rightItemInstance = self.AttachItem(hash, CurrentRightItemVariant, self.m_rightHand);
            }
            return true;
        }
    }
}
