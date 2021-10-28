using System;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for adding custom Map Overlays to the game.
    /// </summary>
    public class MinimapManager : IManager
    {
        private static MinimapManager _instance;

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static MinimapManager Instance
        {
            get
            {
                if (_instance == null) { _instance = new MinimapManager(); }
                return _instance;
            }
        }

        /// <summary>
        ///     Event that gets fired once the Map for a World has started and Mods can begin to draw.
        /// </summary>
        public static event Action OnVanillaMapAvailable;

        /// <summary>
        ///     Event that gets fired once data for a specific Map for a world has been loaded. Eg, Pins are available after this has fired.
        /// </summary>
        public static event Action OnVanillaMapDataLoaded;

        private Color MainTexHeightColour = new Color(10f, 0f, 0f);
        private const int DefaultOverlaySize = 2048;
        private const string OverlayNamePrefix = "custom_map_overlay_";

        private Dictionary<string, MapOverlay> Overlays = new Dictionary<string, MapOverlay>();
        private int OverlayID;

        // vanilla backups of textures
        private Texture2D SpaceTexVanilla;
        private Texture2D CloudTexVanilla;
        private Texture2D FogTexVanilla; // fog texture, not the filter.


        private Texture2D FogFilterVanilla;
        private Texture2D HeightFilterVanilla;
        private Texture2D ForestFilterVanilla;
        private Texture2D BackgroundTexVanilla;
        private Texture2D WaterTexVanilla;
        private Texture2D MountainTexVanilla;
        private Texture2D MainTexVanilla;

        private Texture2D MountainTexRef;
        

        // working copies of vanilla textures
/*
        private Texture2D SpaceTex;
        private Texture2D CloudTex;
        private Texture2D FogTex;

        private Texture2D FogFilter;
        private Texture2D HeightFilter;
        private Texture2D ForestFilter;
        private Texture2D BackgroundTex;
        private Texture2D MountainTex;
        private Texture2D MainTex;
        private Texture2D WaterTex;
*/

        /// <summary>
        ///     Object for modders to use to access and modify their Overlay.
        ///     Modders should modify the texture directly.
        ///     MapName and TextureSize should not be modified.
        ///     
        ///     Although fog gets updated on the vanilla texture, it is possible for the MapOverlay to get a snapshot of old Fog data, then continuously apply that outdated info.
        /// </summary>
        public class MapOverlay
        {
            /// <summary>
            ///     Unique ID per overlay
            /// </summary>
            internal string MapName;

            /// <summary>
            ///     Initial texture size to calculate the relative drawing position
            /// </summary>
            public int TextureSize { get; internal set; }

            public bool ForestFlag { get; set; } = false;
            public bool FogFlag { get; set; } = false;
            public bool HeightFlag { get; set; } = false;
            public bool MainFlag { get; set; } = false;
            public bool WaterFlag { get; set; } = false;
            public bool MountainFlag { get; set; } = false;
            public bool BackgroundFlag { get; set; } = false;

            /// <summary>
            ///     Image component holding the overlay texture data
            /// </summary>
            public Texture2D ForestFilter { get; internal set; }
            public Texture2D FogFilter { get; internal set; }
            public Texture2D HeightFilter { get; internal set; }
            public Texture2D MainImg { get; internal set; }
            public Texture2D WaterImg { get; internal set; }
            public Texture2D MountainImg { get; internal set; }
            public Texture2D BackgroundImg { get; internal set; }

            /// <summary>
            ///     Set true to render this overlay, false to hide
            /// </summary>
            public bool Enabled { get; set; }
        }
        
        /// <summary>
        ///     Creates the Overlays and registers hooks.
        /// </summary>
        public void Init()
        {

            using (new DetourContext(int.MaxValue - 1000))
            {
                //On.Minimap.Start += Minimap_Start;
                On.Minimap.Start += InvokeOnVanillaMapAvailable;
                On.Minimap.LoadMapData += InvokeOnVanillaMapDataLoaded;
                On.Minimap.Explore_int_int += InvokeOnVanillaMapExplore;
                On.Minimap.ExploreOthers += InvokeOnVanillaMapExploreOthers;
                On.Minimap.Reset += InvokeOnVanillaMapReset;
            }

            
            SceneManager.activeSceneChanged += (current, next) => Instance.Overlays.Clear();
            //OnVanillaMapAvailable += InitializeTextures;
            OnVanillaMapDataLoaded += SetupTextures;
            
        }

        private void InitializeTextures()
        {
            Logger.LogInfo("Initializing MinimapOverlay Textures");
            ForestFilterVanilla = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            ForestFilterVanilla.wrapMode = TextureWrapMode.Clamp;
            HeightFilterVanilla = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RFloat, mipChain: false);
            HeightFilterVanilla.wrapMode = TextureWrapMode.Clamp;
            FogFilterVanilla = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            FogFilterVanilla.wrapMode = TextureWrapMode.Clamp;

            MainTexVanilla = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            MainTexVanilla.wrapMode = TextureWrapMode.Clamp;
            BackgroundTexVanilla = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            BackgroundTexVanilla.wrapMode = TextureWrapMode.Clamp;
            WaterTexVanilla = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            WaterTexVanilla.wrapMode = TextureWrapMode.Clamp;
            MountainTexVanilla = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            MountainTexVanilla.wrapMode = TextureWrapMode.Clamp;
        }
        private void SetupTextures()
        {
            Logger.LogInfo("Setting up MinimapOverlay Textures");
            ForestFilterVanilla.SetPixels(Minimap.instance.m_forestMaskTexture.GetPixels());
            HeightFilterVanilla.SetPixels(Minimap.instance.m_heightTexture.GetPixels());
            FogFilterVanilla.SetPixels(Minimap.instance.m_fogTexture.GetPixels());

            MainTexVanilla.SetPixels(Minimap.instance.m_mapTexture.GetPixels());
            MountainTexRef = (Texture2D)Minimap.instance.m_mapImageLarge.material.GetTexture("_MountainTex");
            /*
                        BackgroundTex = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
                        BackgroundTex.wrapMode = TextureWrapMode.Clamp;
                        MainTex = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
                        MainTex.wrapMode = TextureWrapMode.Clamp;
                        FogFilter = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
                        FogFilter.wrapMode = TextureWrapMode.Clamp;
                        HeightFilter = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RFloat, mipChain: false);
                        HeightFilter.wrapMode = TextureWrapMode.Clamp;
                        ForestFilter = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
                        ForestFilter.wrapMode = TextureWrapMode.Clamp;
                        WaterTex = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
                        WaterTex.wrapMode = TextureWrapMode.Clamp;
                        MountainTex = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
                        MountainTex.wrapMode = TextureWrapMode.Clamp;
            */
            // copy unreadable textures.
            BackupTexture(WaterTexVanilla, "_WaterTex");
            BackupTexture(BackgroundTexVanilla, "_BackgroundTex");
            BackupTexture(MountainTexVanilla, "_MountainTex");

            //typeof(Texture2D).GetField("isReadable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(MountainTexRef, true);

            Logger.LogInfo($"is readable mountaintex? {MountainTexRef.isReadable}");
            Logger.LogInfo($"is readable maintex? {Minimap.instance.m_mapTexture.isReadable}");

        }


        /// <summary>
        ///     Read pixels from a texture where import settings do not allow for read.
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="name"></param>
        private void BackupTexture(Texture2D tex, string name)
        {
            var backupTex2d = (Texture2D)Minimap.instance.m_mapImageLarge.material.GetTexture(name);

            RenderTexture tmp = RenderTexture.GetTemporary(backupTex2d.width, backupTex2d.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(backupTex2d, tmp);

            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;

            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;

            // Copy the pixels from the RenderTexture to the new Texture
            tex.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            tex.Apply();

            // Reset the active RenderTexture
            RenderTexture.active = previous;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);
        }

        /// <summary>
        ///     Create a new mapoverlay with a default overlay name
        /// </summary>
        /// <returns>Reference to MapOverlay for modder to edit</returns>
        public MapOverlay AddMapOverlay()
        {
            return AddMapOverlay(OverlayNamePrefix + OverlayID++);
        }


        /// <summary>
        ///     Create a new mapoverlay with a custom overlay name
        /// </summary>
        /// <param name="name">Custom name for the MapOverlay</param>
        /// <returns>Reference to MapOverlay for modder to edit</returns>
        public MapOverlay AddMapOverlay(string name)
        {
            if (Overlays.ContainsKey(name))
            {
                Logger.LogDebug($"Returning existing overlay with name {name}");
                return Overlays[name];
            }

            MapOverlay res = new MapOverlay();
            res.MapName = name;
            AddOverlay(res);
            return res;
        }

        // 
        /// <summary>
        ///     If a mod already has an instance of MapOverlay ready, they can simply call this function to register to the MapOverlayManager.
        ///     This Manager will then ensure the image is updated.
        /// </summary>
        /// <param name="ovl">The MapOverlay to be registered</param>
        public void AddMapOverlay(MapOverlay ovl)
        {
            Overlays.Add(ovl.MapName, ovl);
        }


        /// <summary>
        ///     Causes MapManager to stop updating the MapOverlay object and removes this Manager's reference to that overlay.
        ///     A mod could still hold references and keep the object alive.
        /// </summary>
        /// <param name="name">The name of the MapOverlay to be removed</param>
        /// <returns>True if removal was successful. False if there was an error removing the object from the internal dict.</returns>
        public bool RemoveMapOverlay(string name)
        {
            return Overlays.Remove(name);
        }

        /// <summary>
        ///     Returns a reference to a currently registered MapOverlay
        /// </summary>
        /// <param name="name">The name of the MapOverlay to retrieve</param>
        /// <returns>The MapOverlay if it exists.</returns>
        public MapOverlay GetMapOverlay(string name)
        {
            return Overlays[name];
        }

        /// <summary>
        ///     Input a World Coordinate and the size of the overlay texture to retrieve the translated overlay coordinates. 
        /// </summary>
        /// <param name="input">World Coordinates</param>
        /// <param name="texSize">Size of the image from your MapOverlay</param>
        /// <returns>The 2D coordinate space on the MapOverlay</returns>
        public Vector2 WorldToOverlayCoords(Vector3 input, int texSize)
        {
            Minimap.instance.WorldToMapPoint(input, out var mx, out var my);
            return new Vector2(mx * texSize, my * texSize);
        }


        /// <summary>
        ///     Input a MapOverlay Coordinate and the size of the overlay texture to retrieve the translated World coordinates.
        /// </summary>
        /// <param name="input">The 2D Overlay coordinate</param>
        /// <param name="texSize">The size of the Overlay</param>
        /// <returns>The 3D World coordinate that corresponds to the input Vector</returns>
        public Vector3 OverlayToWorldCoords(Vector2 input, int texSize)
        {
            input.x /= texSize;
            input.y /= texSize;
            return Minimap.instance.MapPointToWorld(input.x, input.y);
        }

        /// <summary>
        ///     Reset all vanilla textures to their original values by using our stored backups
        /// </summary>
        public void ResetVanillaMap()
        {
            Logger.LogInfo("Resetting vanilla maps");
            Minimap.instance.m_fogTexture.SetPixels(FogFilterVanilla.GetPixels());
            Minimap.instance.m_mapTexture.SetPixels(MainTexVanilla.GetPixels());
            Minimap.instance.m_forestMaskTexture.SetPixels(ForestFilterVanilla.GetPixels());
            Minimap.instance.m_heightTexture.SetPixels(HeightFilterVanilla.GetPixels());
            //MountainTexRef.SetPixels(MountainTexVanilla.GetPixels());
        }

        public void ComposeOverlays()
        {
            ResetVanillaMap();

            foreach (var m in Overlays)
            {
                if (!m.Value.Enabled) { continue; }
                Logger.LogInfo("Drawing on map");

                for (int i = 0; i < DefaultOverlaySize; i++)
                {
                    for (int j = 0; j < DefaultOverlaySize; j++)
                    {
                        // Note: if this function is too slow try iterating over each texture by itself. 

                        var p = m.Value.MainImg.GetPixel(i, j);
                        if (m.Value.MainFlag)
                        {
                            Minimap.instance.m_mapTexture.SetPixel(i, j, p);
                        }

                        p = m.Value.FogFilter.GetPixel(i, j);
                        if (m.Value.FogFlag)
                        {
                            Minimap.instance.m_fogTexture.SetPixel(i, j, p);
                        }

                        p = m.Value.ForestFilter.GetPixel(i, j);
                        if (m.Value.ForestFlag)
                        {
                            Minimap.instance.m_forestMaskTexture.SetPixel(i, j, p);
                        }

                        p = m.Value.HeightFilter.GetPixel(i, j);
                        if (m.Value.HeightFlag)
                        {
                            Minimap.instance.m_heightTexture.SetPixel(i, j, p);
                        }
                    }
                }
            }
            Minimap.instance.m_mapTexture.Apply();
            Minimap.instance.m_forestMaskTexture.Apply();
            Minimap.instance.m_heightTexture.Apply();
            Minimap.instance.m_fogTexture.Apply();
        }

        /// <summary>
        ///     Helper function to set default properties of a MapOverlay.
        ///     Create a new image of our custom default size and set its anchor min/max to the bottom left.
        ///     Then add a reference of the mapoverlay to our dict so we can update it later.
        /// </summary>
        /// <param name="ovl">The overlay to be added</param>
        private void AddOverlay(MapOverlay ovl)
        {
            ovl.Enabled = true;
            ovl.TextureSize = DefaultOverlaySize;

            Func<TextureFormat, Texture2D, Texture2D> Create = (fmt, van) =>
            {
                var t = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, fmt, mipChain: false);
                t.wrapMode = TextureWrapMode.Clamp;
                t.SetPixels(van.GetPixels());
                return t;
            };

            ovl.MainImg = Create(TextureFormat.RGBA32, MainTexVanilla);
            ovl.MountainImg = Create(TextureFormat.RGBA32, MountainTexVanilla);
            ovl.BackgroundImg = Create(TextureFormat.RGBA32, BackgroundTexVanilla);
            ovl.WaterImg = Create(TextureFormat.RGBA32, WaterTexVanilla);
            ovl.FogFilter = Create(TextureFormat.RGBA32, FogFilterVanilla);
            ovl.ForestFilter = Create(TextureFormat.RGBA32, ForestFilterVanilla);
            ovl.HeightFilter = Create(TextureFormat.RFloat, HeightFilterVanilla);

            Overlays.Add(ovl.MapName, ovl);
        }

        /// <summary>
        ///     Safely invoke OnVanillaMapAvailable event.
        /// </summary>
        private void InvokeOnVanillaMapAvailable(On.Minimap.orig_Start orig, Minimap self)
        {
            InitializeTextures();
            orig(self);
            OnVanillaMapAvailable?.SafeInvoke();
        }


        /// <summary>
        ///     Safely invoke InvokeOnVanillaMapDataLoaded event.
        /// </summary>
        private void InvokeOnVanillaMapDataLoaded(On.Minimap.orig_LoadMapData orig, Minimap self)
        {
            orig(self);
            OnVanillaMapDataLoaded?.SafeInvoke();
        }

        private bool InvokeOnVanillaMapExplore(On.Minimap.orig_Explore_int_int orig, Minimap self, int x, int y)
        {
            if (!self.m_explored[y * self.m_textureSize + x])
            {
                FogFilterVanilla.SetPixel(x, y, new Color(0, 0, 0));
            }

            return orig(self, x, y);

        }

        private bool InvokeOnVanillaMapExploreOthers(On.Minimap.orig_ExploreOthers orig, Minimap self, int x, int y)
        {
            if (!self.m_explored[y * self.m_textureSize + x])
            {
                FogFilterVanilla.SetPixel(x, y, new Color(0, 0, 0));
            }

            return orig(self, x, y);
        }

        
        private void InvokeOnVanillaMapReset(On.Minimap.orig_Reset orig, Minimap self)
        {
            orig(self);
            FogFilterVanilla.SetPixels(self.m_fogTexture.GetPixels());
        }

    }
}
