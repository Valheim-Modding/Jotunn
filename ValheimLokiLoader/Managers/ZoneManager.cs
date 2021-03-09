using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimLokiLoader.Managers
{
    public class ZoneManager : Manager
    {
        public static ZoneManager Instance { get; private set; }

        public event EventHandler ZoneLoad;
        internal List<ZoneSystem.ZoneVegetation> Vegetation = new List<ZoneSystem.ZoneVegetation>();

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        internal override void Register()
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

        public void RegisterVegetation(ZoneSystem.ZoneVegetation veg)
        {
            Vegetation.Add(veg);
        }
    }
}
