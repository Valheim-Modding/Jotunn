using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.GUI;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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

        /// <summary>
        ///     Recommended height to set the height filter to in order to get good flat colouring on MainTex.
        ///     Red values of 0 are ignored by the shader.
        /// </summary>
        public static Color meadowHeight = new Color(32, 0, 0, 255);

        /// <summary>
        ///     Colour which sets a filter on. Used for ForestFilter and FogFilter.
        ///     A full alpha value enables this pixel, and then the red value is written to the result texture.
        /// </summary>
        public static Color FilterOn = new Color(1f, 0f, 0f, 255f);

        /// <summary>
        ///     Colour which sets a filter off. See FilterOn.
        /// </summary>
        public static Color FilterOff = new Color(0f, 0f, 0f, 255f);

        private const int DefaultOverlaySize = 2048;
        private const string OverlayNamePrefix = "custom_map_overlay_";

        private Dictionary<string, MapOverlay> Overlays = new Dictionary<string, MapOverlay>();
        private int OverlayID;

        /*
                // vanilla backups of textures
                private Texture2D SpaceTexVanilla;
                private Texture2D CloudTexVanilla;
                private Texture2D FogTexVanilla; // fog texture, not the filter.
        */

        // Keep backups of all vanilla textures in order to revert overlays.
        private Texture2D MainTexVanilla;
        private Texture2D HeightFilterVanilla;
        private Texture2D ForestFilterVanilla;
        private Texture2D FogFilterVanilla;

        // private Texture2D BackgroundTexVanilla;
        // private Texture2D WaterTexVanilla;
        // private Texture2D MountainTexVanilla;

        private Texture2D BackgroundTemp;
        // Default transparent tex to provide to modders to draw on.
        private Texture2D TransparentTex;
        //private Texture ClearTex;

        // Materials that have shaders used to blit overlays onto the minimap.
        private Material ComposeMainMaterial;
        private Material ComposeHeightMaterial;
        private Material ComposeForestMaterial;
        private Material ComposeFogMaterial;

        // Current component for the MinimapOverlayPanel
        private MinimapOverlayPanel OverlayPanel;

        /// <summary>
        ///     Creates the Overlays and registers hooks.
        /// </summary>
        public void Init()
        {
            // Provide hooks for modders for when Minimap is available.
            On.Minimap.Start += Minimap_Start;
            On.Minimap.LoadMapData += Minimap_LoadMapData;

            // Setup methods to properly explore fog, and keep vanilla copy of fog properly updated.
            On.Minimap.Explore_int_int += Minimap_Explore;
            On.Minimap.Explore_Vector3_float += Minimap_Explore_2;
            On.Minimap.ExploreOthers += Minimap_ExploreOthers;
            On.Minimap.ExploreAll += Minimap_ExploreAll;
            On.Minimap.AddSharedMapData += Minimap_AddSharedMapData;
            On.Minimap.Reset += Minimap_Reset;

            // Properly clear the Overlays when exiting a world
            SceneManager.activeSceneChanged += (current, next) => Instance.Overlays.Clear();

            // Load shaders and setup materials
            var bundle = AssetUtils.LoadAssetBundleFromResources("minimapmanager", typeof(MinimapManager).Assembly);

            // Load texture with all pixels (RGBA) set to 0f.
            TransparentTex = bundle.LoadAsset<Texture2D>("2048x2048_clear");
            Logger.LogInfo($"Transparent tex loaded with pixel values: {TransparentTex.GetPixel(0,0).r} {TransparentTex.GetPixel(0, 0).g} {TransparentTex.GetPixel(0, 0).b} {TransparentTex.GetPixel(0, 0).a}");

            // Create materials and shaders to compute overlays onto the vanilla textures
            var composeMainShader = bundle.LoadAsset<Shader>("MinimapComposeMain");
            var composeHeightShader = bundle.LoadAsset<Shader>("MinimapComposeHeight");
            var composeForestShader = bundle.LoadAsset<Shader>("MinimapComposeForest");
            var composeFogShader = bundle.LoadAsset<Shader>("MinimapComposeFog");
            ComposeMainMaterial = new Material(composeMainShader);
            ComposeHeightMaterial = new Material(composeHeightShader);
            ComposeForestMaterial = new Material(composeForestShader);
            ComposeFogMaterial = new Material(composeFogShader);

            var overlaypanel = bundle.LoadAsset<GameObject>("MinimapOverlayPanel");
            PrefabManager.Instance.AddPrefab(new CustomPrefab(overlaypanel, false));

            bundle.Unload(false);

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
                        Logger.LogInfo("Redrawing dirty layers");
                        yield return DrawMain();
                        yield return DrawHeight();
                        yield return DrawForestFilter();
                        yield return DrawFogFilter();
                        foreach (var overlay in Overlays.Values)
                        {
                            overlay.Dirty = false;
                        }
                    }
                }
            }
            Minimap.instance.StartCoroutine(watchdog());
        }

        private IEnumerator DrawMain()
        {
            if (Overlays.Values.Any(x => x.MainDirty))
            {
                Logger.LogInfo("Redraw Main");
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                Graphics.CopyTexture(MainTexVanilla, Minimap.instance.m_mapTexture); // Reset vanilla texture to backup
                foreach (var overlay in Overlays.Values.Where(x => x.Enabled && x.MainEnabled))
                {
                    DrawOverlay(overlay.MainTex, Minimap.instance.m_mapTexture, ComposeMainMaterial);
                }

                watch.Stop();
                Logger.LogInfo($"DrawMain loop took {watch.ElapsedMilliseconds}ms time");
            }

            yield return null;
        }

        private IEnumerator DrawHeight()
        {
            if (Overlays.Values.Any(x => x.HeightDirty))
            {
                Logger.LogInfo("Redraw Height");
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                Graphics.CopyTexture(HeightFilterVanilla, Minimap.instance.m_heightTexture); // Reset vanilla texture to backup
                foreach (var overlay in Overlays.Values.Where(x => x.Enabled && x.HeightEnabled))
                {
                    DrawOverlay(overlay.HeightFilter, Minimap.instance.m_heightTexture, ComposeHeightMaterial,
                        RenderTextureFormat.ARGBFloat);
                }

                watch.Stop();
                Logger.LogInfo($"DrawHeight loop took {watch.ElapsedMilliseconds}ms time");
            }

            yield return null;
        }

        private IEnumerator DrawForestFilter()
        {
            if (Overlays.Values.Any(x => x.ForestDirty))
            {
                Logger.LogInfo("Redraw Forest");
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                Graphics.CopyTexture(ForestFilterVanilla, Minimap.instance.m_forestMaskTexture); // Reset vanilla texture to backup
                foreach (var overlay in Overlays.Values.Where(x => x.Enabled && x.ForestEnabled))
                {
                    DrawOverlay(overlay.ForestFilter, Minimap.instance.m_forestMaskTexture, ComposeForestMaterial);
                }

                watch.Stop();
                Logger.LogInfo($"DrawForest loop took {watch.ElapsedMilliseconds}ms time");
            }

            yield return null;
        }

        private IEnumerator DrawFogFilter()
        {
            if (Overlays.Values.Any(x => x.FogDirty))
            {
                Logger.LogInfo("Redraw Fog");
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                Graphics.CopyTexture(FogFilterVanilla, Minimap.instance.m_fogTexture); // Reset vanilla texture to backup
                foreach (var overlay in Overlays.Values.Where(x => x.Enabled && x.FogEnabled))
                {
                    DrawOverlay(overlay.FogFilter, Minimap.instance.m_fogTexture, ComposeFogMaterial);
                }

                watch.Stop();
                Logger.LogInfo($"DrawFog loop took {watch.ElapsedMilliseconds}ms time");
            }

            yield return null;
        }

        private void DrawOverlay(Texture2D overlay, Texture2D dest, Material mat, RenderTextureFormat format = RenderTextureFormat.Default)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(DefaultOverlaySize, DefaultOverlaySize, 0, format, RenderTextureReadWrite.Default);

            // Blit sets the overlay texture as _Maintex on the shader then computes the frag function of the shader, setting the result to tmp.
            Graphics.Blit(overlay, tmp, mat);

            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;

            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;

            // Copy the pixels from the RenderTexture to the new Texture
            // This applies the pixels to the Vanilla texture.
            dest.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            dest.Apply();

            // Reset the active RenderTexture
            RenderTexture.active = previous;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);
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
            // BackgroundTexVanilla = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            // BackgroundTexVanilla.wrapMode = TextureWrapMode.Clamp;
            // WaterTexVanilla = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            // WaterTexVanilla.wrapMode = TextureWrapMode.Clamp;
            // MountainTexVanilla = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            // MountainTexVanilla.wrapMode = TextureWrapMode.Clamp;
            // FogTexVanilla = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            // FogTexVanilla.wrapMode = TextureWrapMode.Clamp;

            BackgroundTemp = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            BackgroundTemp.wrapMode = TextureWrapMode.Clamp;

            //TransparentTex = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            //TransparentTex.wrapMode = TextureWrapMode.Clamp;

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

            // Set the vanilla minimap textures in the shaders
            ComposeMainMaterial.SetTexture("_VanillaTex", Minimap.instance.m_mapTexture);
            ComposeHeightMaterial.SetTexture("_VanillaTex", Minimap.instance.m_heightTexture);
            ComposeForestMaterial.SetTexture("_VanillaTex", Minimap.instance.m_forestMaskTexture);
            ComposeFogMaterial.SetTexture("_VanillaTex", Minimap.instance.m_fogTexture);

            // copy unreadable textures.
            // BackupTexture(BackgroundTexVanilla, "_BackgroundTex");
            // BackupTexture(WaterTexVanilla, "_WaterTex");
            // BackupTexture(MountainTexVanilla, "_MountainTex");
            //BackupTexture(FogTexVanilla, "_FogLayerTex");
            //Minimap.instance.m_mapImageLarge.material.SetTexture("_FogLayerTex", FogTexVanilla);
            //Logger.LogInfo($"foglayertex width {FogTexVanilla.width} height {FogTexVanilla.height}");

/*            Color c = new Color(0, 0, 0, 0);

            // TODO: Load this texture from file, should be faster.
            for (int i = 0; i < DefaultOverlaySize; i++)
            {
                for (int j = 0; j < DefaultOverlaySize; j++)
                {
                    TransparentTex.SetPixel(i, j, c);
                }
            }
            TransparentTex.Apply();
*/
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

            RenderTexture tmp = RenderTexture.GetTemporary(backupTex2d.width, backupTex2d.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);

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
        ///     Return a list of all current overlay names
        /// </summary>
        /// <returns>List of names</returns>
        public List<string> GetOverlayNames()
        {
            List<string> res = new List<string>();
            foreach (var ovl in Overlays)
            {
                res.Add(ovl.Value.Name);
            }
            return res;
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
            return new Vector2((float)Math.Round(mx * texSize), (float)Math.Round(my * texSize));
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

        /// <summary>
        ///     Initialize our local textures from vanilla on <see cref="Minimap.Start"/>
        /// </summary>
        private void Minimap_Start(On.Minimap.orig_Start orig, Minimap self)
        {
            InitializeTextures();
            orig(self);
            StartWatchdog();
            MapGUICreate();
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
            InvokeOnVanillaMapDataLoaded();
            FogFilterVanilla.Apply(); // maybe not needed.
        }

        /// <summary>
        ///     Safely invoke InvokeOnVanillaMapDataLoaded event.
        /// </summary>
        private void InvokeOnVanillaMapDataLoaded()
        {
            OnVanillaMapDataLoaded?.SafeInvoke();
        }


        private bool Minimap_AddSharedMapData(On.Minimap.orig_AddSharedMapData orig, Minimap self, byte[] dataArray)
        {
            bool t = orig(self, dataArray);
            FogFilterVanilla.Apply(); // maybe not needed.
            return t;
        }

        private bool Minimap_Explore(On.Minimap.orig_Explore_int_int orig, Minimap self, int x, int y)
        {
            if (!self.m_explored[y * self.m_textureSize + x])
            {
                FogFilterVanilla.SetPixel(x, y, FilterOff);
            }
            return orig(self, x, y);
        }

        private void Minimap_Explore_2(On.Minimap.orig_Explore_Vector3_float orig, Minimap self, Vector3 v, float f)
        {
            orig(self, v, f);
            FogFilterVanilla.Apply();
            return;
        }

        private void Minimap_ExploreAll(On.Minimap.orig_ExploreAll orig, Minimap self)
        {
            orig(self);
            FogFilterVanilla.Apply();
            return;
        }

        private bool Minimap_ExploreOthers(On.Minimap.orig_ExploreOthers orig, Minimap self, int x, int y)
        {
            if (!self.m_explored[y * self.m_textureSize + x])
            {
                FogFilterVanilla.SetPixel(x, y, FilterOff);
            }

            return orig(self, x, y);
        }

        private void Minimap_Reset(On.Minimap.orig_Reset orig, Minimap self)
        {
            orig(self);
            FogFilterVanilla.SetPixels(self.m_fogTexture.GetPixels());
            FogFilterVanilla.Apply();
        }

        private void MapGUICreate()
        {
            var basePanel = PrefabManager.Instance.GetPrefab("MinimapOverlayPanel");
            var panel = Object.Instantiate(basePanel, Minimap.instance.m_mapLarge.transform, false);
            OverlayPanel = panel.GetComponent<MinimapOverlayPanel>();
            GUIManager.Instance.ApplyButtonStyle(OverlayPanel.GetComponent<MinimapOverlayPanel>().Button);
            GUIManager.Instance.ApplyToogleStyle(OverlayPanel.GetComponent<MinimapOverlayPanel>().BaseToggle);
            GUIManager.Instance.ApplyTextStyle(OverlayPanel.GetComponent<MinimapOverlayPanel>().BaseModText);
        }

        private void AddOverlayToGUI(MapOverlay ovl)
        {
            var toggle = OverlayPanel?.AddOverlayToggle(ovl.SourceMod.Name, ovl.Name);
            toggle?.onValueChanged.AddListener(active =>
            {
                ovl.Enabled = active;
            });
        }

        /// <summary>
        ///     Object for modders to use to access and modify their Overlay.
        ///     Modders should modify the texture directly.
        ///     
        /// </summary>
        public class MapOverlay : CustomEntity
        {
            /// <summary>
            ///     Unique name per overlay
            /// </summary>
            public string Name { get; internal set; }

            /// <summary>
            ///     Initial texture size to calculate the relative drawing position
            /// </summary>
            public int TextureSize { get; internal set; }

            /// <summary>
            ///     Texture to draw main texture information to
            /// </summary>
            public Texture2D MainTex => _mainTex ??= Create(Instance.MainTexVanilla);

            /// <summary>
            ///     Texture to draw height filter information to
            /// </summary>
            public Texture2D HeightFilter => _heightFilter ??= Create(Instance.HeightFilterVanilla);

            /// <summary>
            ///     Texture to draw forest filter information to
            /// </summary>
            public Texture2D ForestFilter => _forestFilter ??= Create(Instance.ForestFilterVanilla);

            /// <summary>
            ///     Texture to draw fog filter information to
            /// </summary>
            public Texture2D FogFilter => _fogFilter ??= Create(Instance.FogFilterVanilla);

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

            internal bool MainEnabled => _mainTex != null;
            internal bool HeightEnabled => _heightFilter != null;
            internal bool ForestEnabled => _forestFilter != null;
            internal bool FogEnabled => _fogFilter != null;

            /// <summary>
            ///     Flag to determine if this overlay had changes since its last draw
            /// </summary>
            internal bool Dirty
            {
                get
                {
                    return ForestDirty || FogDirty || HeightDirty || MainDirty;
                }
                set
                {
                    _forestDirty = value;
                    _fogDirty = value;
                    _heightDirty = value;
                    _mainDirty = value;
                }
            }

            internal bool MainDirty => _mainTex != null && _mainDirty;
            internal bool HeightDirty => _heightFilter != null && _heightDirty;
            internal bool ForestDirty => _forestFilter != null && _forestDirty;
            internal bool FogDirty => _fogFilter != null && _fogDirty;

            private bool _enabled;

            private bool _mainDirty;
            private bool _heightDirty;
            private bool _forestDirty;
            private bool _fogDirty;

            private Texture2D _mainTex;
            private Texture2D _heightFilter;
            private Texture2D _forestFilter;
            private Texture2D _fogFilter;

            /// <summary>
            ///     Helper function to create and copy overlay texture instances
            /// </summary>
            private Texture2D Create(Texture2D van)
            {
                var t = new Texture2D(van.width, van.height, van.format, mipChain: false);
                t.wrapMode = TextureWrapMode.Clamp;
                t.name = Name;
                Graphics.CopyTexture(Instance.TransparentTex, t);
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
                if (tex == _mainTex)
                {
                    _mainDirty = true;
                }
                if (tex == _heightFilter)
                {
                    _heightDirty = true;
                }
                if (tex == _forestFilter)
                {
                    _forestDirty = true;
                }
                if (tex == _fogFilter)
                {
                    _fogDirty = true;
                }
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
