using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimLokiLoader.Managers
{
    public static class ZoneManager
    {
        public static event EventHandler ZoneLoad;
        internal static List<ZoneSystem.ZoneVegetation> Vegetation = new List<ZoneSystem.ZoneVegetation>();

        internal static void LoadZoneData()
        {
            Debug.Log("---- Registering custom zone data ----");

            // Call event handlers to load prefabs
            ZoneLoad?.Invoke(null, EventArgs.Empty);

            foreach (var veg in Vegetation)
            {
                ZoneSystem.instance.m_vegetation.Add(veg);
                Debug.Log("Added vegetation: " + veg.m_name);
            }
        }

        public static void AddVegetation(ZoneSystem.ZoneVegetation veg)
        {
            Vegetation.Add(veg);
        }
    }
}
