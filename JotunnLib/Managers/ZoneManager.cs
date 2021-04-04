using System;
using System.Collections.Generic;

namespace JotunnLib.Managers
{
    internal class ZoneManager : Manager
    {
        public static ZoneManager Instance { get; private set; }

        public event EventHandler ZoneLoad;
        internal List<ZoneSystem.ZoneVegetation> Vegetation = new List<ZoneSystem.ZoneVegetation>();

        internal override void Init()
        {
            On.ZNetScene.Awake += RegisterAllToZNetScene;
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Two instances of singleton {GetType()}");
                return;
            }

            Instance = this;
        }

        public void RegisterAllToZNetScene(On.ZNetScene.orig_Awake orig, ZNetScene self)
        {
            orig(self);
            
            Logger.LogInfo("---- Registering custom zone data ----");

            // Call event handlers to load prefabs
            ZoneLoad?.Invoke(null, EventArgs.Empty);

            foreach (var veg in Vegetation)
            {
                ZoneSystem.instance.m_vegetation.Add(veg);
                Logger.LogInfo("Added vegetation: " + veg.m_name);
            }
        }

        public void AddVegetation(ZoneSystem.ZoneVegetation veg)
        {
            Vegetation.Add(veg);
        }
    }
}
