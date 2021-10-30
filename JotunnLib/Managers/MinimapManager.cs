using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
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

        private GameObject ScrollView;
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

        private Texture2D TransparentTex;

        /// <summary>
        ///     Creates the Overlays and registers hooks.
        /// </summary>
        public void Init()
        {
            On.Minimap.Start += Minimap_Start;
            On.Minimap.LoadMapData += Minimap_LoadMapData;
            On.Minimap.Explore_int_int += Minimap_Explore;
            On.Minimap.ExploreOthers += Minimap_ExploreOthers;
            On.Minimap.Reset += Minimap_Reset;

            SceneManager.activeSceneChanged += (current, next) => Instance.Overlays.Clear();

            Harmony.CreateAndPatchAll(typeof(Texture2D_Apply));
        }

        private void StartWatchdog()
        {
            IEnumerator watchdog()
            {
                while (true)
                {
                    yield return null;

                    if (Overlays.Values.Any(x => x.Dirty))
                    {
                        Logger.LogInfo("Redraw dirty");
                        var watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        DrawMain();
                        DrawHeight();
                        foreach (var overlay in Overlays.Values)
                        {
                            overlay.Dirty = false;
                        }
                        if (Minimap.instance.m_smallRoot.activeSelf)
                        {
                            Minimap.instance.m_smallRoot.SetActive(false);
                            Minimap.instance.m_smallRoot.SetActive(true);
                        }
                        watch.Stop();
                        Logger.LogInfo($"Drawing took {watch.ElapsedMilliseconds}ms time");
                    }
                }
            }
            Minimap.instance.StartCoroutine(watchdog());
        }

        private void DrawMain()
        {
            if (!Overlays.Values.Any(x => x.MainDirty))
            {
                return;
            }

            Logger.LogInfo("Redraw Main");

            Texture2D texture =
                new Texture2D(MainTexVanilla.width, MainTexVanilla.height, MainTexVanilla.format, false);
            Graphics.CopyTexture(MainTexVanilla, texture);
            foreach (var overlay in Overlays.Values.Where(x => x.MainDirty && x.Enabled))
            {
                texture.SetPixels32(overlay.MainTex.GetPixels32());
            }
            texture.Apply();
            Minimap.instance.m_mapImageSmall.material.SetTexture("_MainTex", texture);
            Minimap.instance.m_mapImageLarge.material.SetTexture("_MainTex", texture);
        }

        private void DrawHeight()
        {
            if (!Overlays.Values.Any(x => x.HeightDirty))
            {
                return;
            }

            Logger.LogInfo("Redraw Height");

            Texture2D texture =
                new Texture2D(HeightFilterVanilla.width, HeightFilterVanilla.height, HeightFilterVanilla.format, false);
            Graphics.CopyTexture(HeightFilterVanilla, texture);
            foreach (var overlay in Overlays.Values.Where(x => x.HeightDirty && x.Enabled))
            {
                texture.SetPixels(overlay.HeightFilter.GetPixels());
            }
            texture.Apply();
            Minimap.instance.m_mapImageSmall.material.SetTexture("_HeightTex", texture);
            Minimap.instance.m_mapImageLarge.material.SetTexture("_HeightTex", texture);
        }

        private void DrawOverlay(MapOverlay overlay)
        {
            // use rendertexture
            RenderTexture tmp = RenderTexture.GetTemporary(DefaultOverlaySize, DefaultOverlaySize, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(overlay.MainTex, tmp);

            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;

            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;

            // Copy the pixels from the RenderTexture to the new Texture
            //tex.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            //tex.Apply();
            Minimap.instance.m_mapTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            Minimap.instance.m_mapTexture.Apply();
            // Reset the active RenderTexture
            RenderTexture.active = previous;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);

            // clean the overlay
            overlay.Dirty = false;
        }

        private void InitializeTextures()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

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

            TransparentTex = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            TransparentTex.wrapMode = TextureWrapMode.Clamp;

            watch.Stop();
            Logger.LogInfo($"Init took {watch.ElapsedMilliseconds}ms time");
        }
        private void SetupTextures()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            Logger.LogInfo("Setting up MinimapOverlay Textures");

            // copy instance textures
            Graphics.CopyTexture(Minimap.instance.m_forestMaskTexture, ForestFilterVanilla);
            Graphics.CopyTexture(Minimap.instance.m_heightTexture, HeightFilterVanilla);
            Graphics.CopyTexture(Minimap.instance.m_fogTexture, FogFilterVanilla);
            Graphics.CopyTexture(Minimap.instance.m_mapTexture, MainTexVanilla);

            // copy unreadable textures.
            BackupTexture(BackgroundTexVanilla, "_BackgroundTex");
            BackupTexture(WaterTexVanilla, "_WaterTex");
            BackupTexture(MountainTexVanilla, "_MountainTex");

            for (int i = 0; i < DefaultOverlaySize; i++)
            {
                for (int j = 0; j < DefaultOverlaySize; j++)
                {
                    TransparentTex.SetPixel(i, j, Color.clear);
                }
            }
            TransparentTex.Apply();

            watch.Stop();
            Logger.LogInfo($"Setup took {watch.ElapsedMilliseconds}ms time");
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

            MapOverlay ret = new MapOverlay();
            ret.Name = name;
            ret.Enabled = true;
            ret.TextureSize = DefaultOverlaySize;
            Overlays.Add(name, ret);
            AddOverlayToGUI(ret);
            return ret;
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
        /// 
        public Vector3 OverlayToWorldCoords(Vector2 input, int texSize)
        {
            input.x /= texSize;
            input.y /= texSize;
            return Minimap.instance.MapPointToWorld(input.x, input.y);
        }
        /*        public Vector3 OverlayToWorldCoords(Vector2 input, int texSize)
                {
                    input.x /= texSize;
                    input.y /= texSize;
                    return Minimap.instance.MapPointToWorld(input.x, input.y);
                }*/

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
        
        /// <summary>
        ///     Initialize our local textures from vanilla on <see cref="Minimap.Start"/>
        /// </summary>
        private void Minimap_Start(On.Minimap.orig_Start orig, Minimap self)
        {
            InitializeTextures();
            orig(self);
            StartWatchdog();
            InvokeOnVanillaMapAvailable();
        }
        
        /// <summary>
        ///     Safely invoke OnVanillaMapAvailable event.
        /// </summary>
        private void InvokeOnVanillaMapAvailable()
        {
            OnVanillaMapAvailable?.SafeInvoke();
        }

        /// <summary>
        ///     Setup textures, GUI and start our watchdog on <see cref="Minimap.LoadMapData"/>
        /// </summary>
        private void Minimap_LoadMapData(On.Minimap.orig_LoadMapData orig, Minimap self)
        {
            orig(self);
            SetupTextures();
            MapGUICreate();
            InvokeOnVanillaMapDataLoaded();
        }

        /// <summary>
        ///     Safely invoke InvokeOnVanillaMapDataLoaded event.
        /// </summary>
        private void InvokeOnVanillaMapDataLoaded()
        {
            OnVanillaMapDataLoaded?.SafeInvoke();
        }

        private bool Minimap_Explore(On.Minimap.orig_Explore_int_int orig, Minimap self, int x, int y)
        {
            if (!self.m_explored[y * self.m_textureSize + x])
            {
                FogFilterVanilla.SetPixel(x, y, new Color(0, 0, 0));
            }

            return orig(self, x, y);
        }

        private bool Minimap_ExploreOthers(On.Minimap.orig_ExploreOthers orig, Minimap self, int x, int y)
        {
            if (!self.m_explored[y * self.m_textureSize + x])
            {
                FogFilterVanilla.SetPixel(x, y, new Color(0, 0, 0));
            }

            return orig(self, x, y);
        }
        
        private void Minimap_Reset(On.Minimap.orig_Reset orig, Minimap self)
        {
            orig(self);
            FogFilterVanilla.SetPixels(self.m_fogTexture.GetPixels());
        }
        
        private void MapGUICreate()
        {
            Jotunn.Logger.LogInfo("Creating wood panels and shizz");
            int pWidth = 300;
            int pHeight = 400;
            int toggleHeight = 30;

            var wp = GUIManager.Instance.CreateWoodpanel(
                //Minimap.instance.m_pinRootLarge,
                Minimap.instance.m_largeRoot.transform,
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                new Vector2(-pWidth / 2, -pHeight / 2),
                /*                anchorMin: new Vector2(0.5f, 0.5f),
                                anchorMax: new Vector2(0.5f, 0.5f),
                                new Vector2(500, 500),*/
                pWidth,
                pHeight
            );

            GUIManager.Instance.CreateText(
                "Hide/Show Overlay Layers",
                wp.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                new Vector2(20, -50),
                GUIManager.Instance.AveriaSerifBold,
                fontSize: 16,
                GUIManager.Instance.ValheimOrange,
                outline: true,
                Color.black,
                width: pWidth,
                height: 50,
                false
            );


            ScrollView = GUIManager.Instance.CreateScrollView(
               wp.transform,
               //Minimap.instance.m_pinRootLarge,
               true,
               true,
               10,
               5,
               GUIManager.Instance.ValheimScrollbarHandleColorBlock,
               Color.black,
               //pWidth-150, 
               //pHeight -200
               pWidth - 50,
               pHeight - 100

           );

            List<string> testnames = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                testnames.Add("test" + i);
            }

            foreach (var n in testnames)
            {
                RectTransform viewport = ScrollView.transform.Find("Scroll View/Viewport/Content") as RectTransform;
                //GameObject go = new GameObject();
                //RectTransform rt = go.AddComponent<RectTransform>();
                //rt. = pWidth - 20;
                //rt.SetParent(viewport);
                //GUIManager.Instance.CreateToggle(rt, 10, 10);
                /*                var txt = GUIManager.Instance.CreateText(n,
                                    viewport, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                                    GUIManager.Instance.AveriaSerifBold, fontSize: 16, GUIManager.Instance.ValheimOrange,
                                    true, Color.black, 650f, 40f, false);*/

                //var tg = GUIManager.Instance.CreateToggle(txt.transform, 10, 10);
                //var tg = GUIManager.Instance.CreateToggle(viewport, 10, 10);
                //tg.GetComponent<Text>();
                //tg.GetComponent<Text>()
                //var tg = GUIManager.Instance.CreateButton();

                //tg.GetComponentInChildren<Text>().text = "test";
            }

        }

        private void AddOverlayToGUI(MapOverlay ovl)
        {
            int pWidth = 300;
            int pHeight = 400;
            Logger.LogInfo($"Adding overlay to GUI with name {ovl.Name}");

            RectTransform viewport = ScrollView.transform.Find("Scroll View/Viewport/Content") as RectTransform;
            //GameObject go = new GameObject();
            //RectTransform rt =  go.AddComponent<RectTransform>();
            //rt. = pWidth - 20;
            //rt.SetParent(viewport);
            GUIManager.Instance.CreateToggle(viewport, 20, 20);
            GUIManager.Instance.CreateText(ovl.Name,
                viewport, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                GUIManager.Instance.AveriaSerifBold, fontSize: 16, GUIManager.Instance.ValheimOrange,
                true, Color.black, 650f, 40f, false);
            //GUIManager.Instance.CreateDropDown;
        }

        /// <summary>
        ///     Object for modders to use to access and modify their Overlay.
        ///     Modders should modify the texture directly.
        ///     
        ///     Although fog gets updated on the vanilla texture, it is possible for the MapOverlay to get a snapshot of old Fog data, then continuously apply that outdated info.
        /// </summary>
        public class MapOverlay
        {
            /// <summary>
            ///     Unique Name per overlay
            /// </summary>
            public string Name { get; internal set; }

            /// <summary>
            ///     Initial texture size to calculate the relative drawing position
            /// </summary>
            public int TextureSize { get; internal set; }

            /// <summary>
            ///     Set true to render this overlay, false to hide
            /// </summary>
            public bool Enabled
            {
                get
                {
                    return _enabled;
                }
                set
                {
                    if (_enabled != value)
                    {
                        _enabled = value;
                        Dirty = true;
                    }
                }
            }
            
            /// <summary>
            ///     Texture components holding the overlay texture data
            /// </summary>
            public Texture2D ForestFilter => _forestFilter ??= Create(Instance.ForestFilterVanilla);

            public Texture2D FogFilter => _fogFilter ??= Create(Instance.FogFilterVanilla);

            public Texture2D HeightFilter => _heightFilter ??= Create(Instance.HeightFilterVanilla);

            public Texture2D MainTex => _mainTex ??= Create(Instance.MainTexVanilla);

            /*public Texture2D WaterTex { get; internal set; }
            public Texture2D MountainTex { get; internal set; }*/

            public Texture2D BackgroundTex => _backgroundTex ??= Create(Instance.BackgroundTexVanilla);

            /// <summary>
            ///     Flag to determine if this overlay had changes since its last draw
            /// </summary>
            internal bool Dirty
            {
                get
                {
                    return ForestDirty | FogDirty | HeightDirty | MainDirty | BackgroundDirty;
                }
                set
                {
                    _forestDirty = value;
                    _fogDirty = value;
                    _heightDirty = value;
                    _mainDirty = value;
                    _backgroundDirty = value;
                }
            }

            internal bool ForestDirty => _forestFilter != null && _forestDirty;

            internal bool FogDirty => _fogFilter != null && _fogDirty;

            internal bool HeightDirty => _heightFilter != null && _heightDirty;

            internal bool MainDirty => _mainTex != null && _mainDirty;

            /*public bool WaterFlag { get; set; }
            public bool MountainFlag { get; set; }*/

            internal bool BackgroundDirty => _backgroundTex != null && _backgroundDirty;

            private bool _enabled;
            
            private bool _forestDirty;
            private bool _fogDirty;
            private bool _heightDirty;
            private bool _mainDirty;
            private bool _backgroundDirty;

            private Texture2D _forestFilter;
            private Texture2D _fogFilter;
            private Texture2D _heightFilter;
            private Texture2D _mainTex;
            private Texture2D _backgroundTex;
            
            /// <summary>
            ///     Helper function to create and copy overlay texture instances
            /// </summary>
            private Texture2D Create(Texture2D van)
            {
                var t = new Texture2D(van.width, van.height, van.format, mipChain: false);
                t.wrapMode = TextureWrapMode.Clamp;
                t.name = Name;
                Graphics.CopyTexture(van, t);
                return t;
            }

            /// <summary>
            ///     Function called on Texture2D.Apply to check if one of our member textures was changed
            /// </summary>
            /// <param name="tex"></param>
            internal void SetTextureDirty(Texture2D tex)
            {
                if (tex.name != Name)
                {
                    return;
                }

                _forestDirty = tex == _forestFilter;
                _fogDirty = tex == _fogFilter;
                _heightDirty = tex == _heightFilter;
                _mainDirty = tex == _mainTex;
                _backgroundDirty = tex == _backgroundTex;
            }
        }

        /// <summary>
        ///     Set an overlay texture dirty on Apply
        /// </summary>
        [HarmonyPatch(typeof(Texture2D), "Apply", typeof(Boolean), typeof(Boolean))]
        private static class Texture2D_Apply
        {
            private static void Postfix(Texture2D __instance)
            {
                foreach (var overlay in Instance.Overlays.Values)
                {
                    overlay.SetTextureDirty(__instance);
                }
            }
        }
    }
}
