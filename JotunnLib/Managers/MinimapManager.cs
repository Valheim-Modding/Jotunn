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
using UnityEngine.UI;
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
        
        // Transparent base texture
        private Texture2D TransparentTex;
        
        // Intermediate textures for the manager to draw on
        private Texture2D OverlayTexIntermediate;
        private Texture2D MainTexIntermediate;
        private Texture2D HeightFilterIntermediate;
        private Texture2D ForestFilterIntermediate;
        private Texture2D FogFilterIntermediate;

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
            
            // Properly clear the Overlays when exiting a world
            SceneManager.activeSceneChanged += (current, next) => Instance.Overlays.Clear();

            // Load shaders and setup materials
            var bundle = AssetUtils.LoadAssetBundleFromResources("minimapmanager", typeof(MinimapManager).Assembly);

            // Load texture with all pixels (RGBA) set to 0f.
            TransparentTex = bundle.LoadAsset<Texture2D>("2048x2048_clear");
            Logger.LogInfo($"Transparent tex loaded with pixel values: {TransparentTex.GetPixel(0,0).r} {TransparentTex.GetPixel(0, 0).g} {TransparentTex.GetPixel(0, 0).b} {TransparentTex.GetPixel(0, 0).a}. Should be (0,0,0,0)");

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
                    if (!Overlays.Values.Any(x => x.Dirty))
                    {
                        continue;
                    }
                    if (Overlays.Values.Any(x => x.OverlayDirty))
                    {
                        yield return DrawOverlay(OverlayTexIntermediate);
                    }
                    if (Overlays.Values.Any(x => x.MainDirty))
                    {
                        yield return DrawMain(MainTexIntermediate);
                    }
                    if (Overlays.Values.Any(x => x.HeightDirty))
                    {
                        yield return DrawHeight(HeightFilterIntermediate);
                    }
                    if (Overlays.Values.Any(x => x.ForestDirty))
                    {
                        yield return DrawForestFilter(ForestFilterIntermediate);
                    }
                    if (Overlays.Values.Any(x => x.FogDirty))
                    {
                        yield return DrawFogFilter(FogFilterIntermediate);
                    }
                    foreach (var overlay in Overlays.Values)
                    {
                        overlay.Dirty = false;
                    }
                }
            }
            Minimap.instance.StartCoroutine(watchdog());
        }
        
        private IEnumerator DrawOverlay(Texture2D intermediate)
        {
            Logger.LogInfo("Redraw Overlay");
            var watch = new System.Diagnostics.Stopwatch(); watch.Start();

            Graphics.CopyTexture(TransparentTex, intermediate); // Reset intermediate texture
            foreach (var overlay in Overlays.Values.Where(x => x.Enabled && x.OverlayEnabled))
            {
                DrawLayer(overlay.OverlayTex, intermediate, null);
            }
            watch.Stop(); Logger.LogInfo($"DrawMain loop took {watch.ElapsedMilliseconds}ms time");

            yield return null;
        }

        private IEnumerator DrawMain(Texture2D intermediate)
        {
            Logger.LogInfo("Redraw Main");
            var watch = new System.Diagnostics.Stopwatch(); watch.Start();

            Graphics.CopyTexture(Minimap.instance.m_mapTexture, intermediate); // Reset vanilla texture to backup
            foreach (var overlay in Overlays.Values.Where(x => x.Enabled && x.MainEnabled))
            {
                DrawLayer(overlay.MainTex, intermediate, ComposeMainMaterial);
            }
            watch.Stop(); Logger.LogInfo($"DrawMain loop took {watch.ElapsedMilliseconds}ms time");

            yield return null;
        }
        
        private IEnumerator DrawHeight(Texture2D intermediate)
        {
            Logger.LogInfo("Redraw Height");
            var watch = new System.Diagnostics.Stopwatch(); watch.Start();

            Graphics.CopyTexture(Minimap.instance.m_heightTexture, intermediate); // Reset vanilla texture to backup
            foreach (var overlay in Overlays.Values.Where(x => x.Enabled && x.HeightEnabled))
            {
                DrawLayer(overlay.HeightFilter, intermediate, ComposeHeightMaterial,
                    RenderTextureFormat.ARGBFloat);
            }
            watch.Stop(); Logger.LogInfo($"DrawHeight loop took {watch.ElapsedMilliseconds}ms time");

            yield return null;
        }

        private IEnumerator DrawForestFilter(Texture2D intermediate)
        {
            Logger.LogInfo("Redraw Forest");
            var watch = new System.Diagnostics.Stopwatch(); watch.Start();

            Graphics.CopyTexture(Minimap.instance.m_forestMaskTexture, intermediate); // Reset vanilla texture to backup
            foreach (var overlay in Overlays.Values.Where(x => x.Enabled && x.ForestEnabled))
            {
                DrawLayer(overlay.ForestFilter, intermediate, ComposeForestMaterial);
            }
            watch.Stop(); Logger.LogInfo($"DrawForest loop took {watch.ElapsedMilliseconds}ms time");

            yield return null;
        }

        private IEnumerator DrawFogFilter(Texture2D intermediate)
        {
            Logger.LogInfo("Redraw Fog");
            var watch = new System.Diagnostics.Stopwatch(); watch.Start();

            Graphics.CopyTexture(Minimap.instance.m_fogTexture, intermediate); // Reset vanilla texture to backup
            foreach (var overlay in Overlays.Values.Where(x => x.Enabled && x.FogEnabled))
            {
                DrawLayer(overlay.FogFilter, intermediate, ComposeFogMaterial);
            }
            watch.Stop(); Logger.LogInfo($"DrawFog loop took {watch.ElapsedMilliseconds}ms time");

            yield return null;
        }

        private void DrawLayer(Texture2D overlay, Texture2D dest, Material mat, RenderTextureFormat format = RenderTextureFormat.Default)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(DefaultOverlaySize, DefaultOverlaySize, 0, format, RenderTextureReadWrite.Default);

            // Blit sets the overlay texture as _Maintex on the shader then computes the frag function of the shader, setting the result to tmp.
            if (mat != null)
            {
                Graphics.Blit(overlay, tmp, mat);
            }
            else
            {
                Graphics.Blit(overlay, tmp);
            }

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

            OverlayTexIntermediate = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            MainTexIntermediate = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            HeightFilterIntermediate = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RFloat, mipChain: false);
            ForestFilterIntermediate = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            FogFilterIntermediate = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);

            Graphics.CopyTexture(TransparentTex, OverlayTexIntermediate);
            ComposeMainMaterial.SetTexture("_VanillaTex", MainTexIntermediate);
            ComposeHeightMaterial.SetTexture("_VanillaTex", HeightFilterIntermediate);
            ComposeForestMaterial.SetTexture("_VanillaTex", ForestFilterIntermediate);
            ComposeFogMaterial.SetTexture("_VanillaTex", FogFilterIntermediate);

            watch.Stop();
            Logger.LogInfo($"Init took {watch.ElapsedMilliseconds}ms time");
        }

        private void SetupTextures()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            Logger.LogInfo("Setting up MinimapOverlay Textures");
            
            // Set own textures to the vanilla materials
            Minimap.instance.m_mapLargeShader.SetTexture("_MainTex", MainTexIntermediate);
            Minimap.instance.m_mapSmallShader.SetTexture("_MainTex", MainTexIntermediate);
            Minimap.instance.m_mapLargeShader.SetTexture("_HeightTex", HeightFilterIntermediate);
            Minimap.instance.m_mapSmallShader.SetTexture("_HeightTex", HeightFilterIntermediate);
            Minimap.instance.m_mapLargeShader.SetTexture("_MaskTex", ForestFilterIntermediate);
            Minimap.instance.m_mapSmallShader.SetTexture("_MaskTex", ForestFilterIntermediate);
            Minimap.instance.m_mapLargeShader.SetTexture("_FogTex", FogFilterIntermediate);
            Minimap.instance.m_mapSmallShader.SetTexture("_FogTex", FogFilterIntermediate);

            // create custom overlay GOs
            var custLarge = new GameObject("CustomLayerLarge");
            var rectLarge = custLarge.AddComponent<RectTransform>();
            rectLarge.anchorMin = Vector2.zero;
            rectLarge.anchorMax = Vector2.one;
            rectLarge.SetParent(Minimap.instance.m_mapImageLarge.transform, false);
            var imageLarge = custLarge.AddComponent<RawImage>();
            imageLarge.texture = OverlayTexIntermediate;
            imageLarge.material = null;
            imageLarge.raycastTarget = false;
            var custSmall = new GameObject("CustomLayerSmall");
            var rectSmall = custSmall.AddComponent<RectTransform>();
            rectSmall.anchorMin = Vector2.zero;
            rectSmall.anchorMax = Vector2.one;
            rectSmall.SetParent(Minimap.instance.m_mapImageSmall.transform, false);
            var imageSmall = custSmall.AddComponent<RawImage>();
            imageSmall.texture = OverlayTexIntermediate;
            imageSmall.material = null;
            imageSmall.raycastTarget = false;
            
            On.Minimap.CenterMap += (orig, self, point) =>
            {
                orig(self, point);
                self.WorldToMapPoint(point, out var mx, out var my);

                Rect largeRect = self.m_mapImageLarge.uvRect;
                largeRect.width += self.m_largeZoom / 8.5f;
                largeRect.height += self.m_largeZoom / 8.5f;
                largeRect.center = new Vector2(mx, my);
                custLarge.GetComponent<RawImage>().uvRect = largeRect;
                    
                Rect smallRect = self.m_mapImageSmall.uvRect;
                smallRect.width += self.m_smallZoom / 2f;
                smallRect.height += self.m_smallZoom / 2f;
                smallRect.center = new Vector2(mx, my);
                custSmall.GetComponent<RawImage>().uvRect = smallRect;
            };
            
            watch.Stop();
            Logger.LogInfo($"Setup took {watch.ElapsedMilliseconds}ms time");
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
            SetupGUI();
            InvokeOnVanillaMapDataLoaded();
        }

        /// <summary>
        ///     Safely invoke InvokeOnVanillaMapDataLoaded event.
        /// </summary>
        private void InvokeOnVanillaMapDataLoaded()
        {
            OnVanillaMapDataLoaded?.SafeInvoke();
        }
        
        private void SetupGUI()
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
            ///     Texture to draw overlay texture information to
            /// </summary>
            public Texture2D OverlayTex => _overlayTex ??= Create(Instance.TransparentTex);

            /// <summary>
            ///     Texture to draw main texture information to
            /// </summary>
            public Texture2D MainTex => _mainTex ??= Create(Minimap.instance.m_mapTexture);

            /// <summary>
            ///     Texture to draw height filter information to
            /// </summary>
            public Texture2D HeightFilter => _heightFilter ??= Create(Minimap.instance.m_heightTexture);

            /// <summary>
            ///     Texture to draw forest filter information to
            /// </summary>
            public Texture2D ForestFilter => _forestFilter ??= Create(Minimap.instance.m_forestMaskTexture);

            /// <summary>
            ///     Texture to draw fog filter information to
            /// </summary>
            public Texture2D FogFilter => _fogFilter ??= Create(Minimap.instance.m_fogTexture);

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

            internal bool OverlayEnabled => _overlayTex != null;
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
                    return OverlayDirty || MainDirty || HeightDirty || ForestDirty || FogDirty;
                }
                set
                {
                    _overlayDirty = value;
                    _mainDirty = value;
                    _heightDirty = value;
                    _forestDirty = value;
                    _fogDirty = value;
                }
            }

            internal bool OverlayDirty => _overlayTex != null && _overlayDirty;
            internal bool MainDirty => _mainTex != null && _mainDirty;
            internal bool HeightDirty => _heightFilter != null && _heightDirty;
            internal bool ForestDirty => _forestFilter != null && _forestDirty;
            internal bool FogDirty => _fogFilter != null && _fogDirty;

            private bool _enabled;

            internal bool _overlayDirty;
            internal bool _mainDirty;
            internal bool _heightDirty;
            internal bool _forestDirty;
            internal bool _fogDirty;

            private Texture2D _overlayTex;
            private Texture2D _mainTex;
            private Texture2D _heightFilter;
            private Texture2D _forestFilter;
            private Texture2D _fogFilter;

            /// <summary>
            ///     Helper function to create and copy overlay texture instances
            /// </summary>
            protected Texture2D Create(Texture2D van)
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
                if (tex == _overlayTex)
                {
                    _overlayDirty = true;
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
