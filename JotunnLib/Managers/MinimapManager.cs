using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.GUI;
using Jotunn.Utils;
using UnityEngine;
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
        ///     Hide .ctor
        /// </summary>
        private MinimapManager() { }

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

        /// <summary>
        ///     Height "Colour" used for the base height of "Meadows"
        /// </summary>
        public static Color MeadowHeight = new Color(32, 0, 0, 255);

        private const int TextureSize = 2048;
        private const string OverlayNamePrefix = "custom_map_overlay_";

        /// <summary>
        ///     Container to hold all live Overlays.
        /// </summary>
        private Dictionary<string, MapOverlay> Overlays = new Dictionary<string, MapOverlay>();
        /// <summary>
        ///     Container to hold all live Drawings.
        /// </summary>
        private Dictionary<string, MapDrawing> Drawings = new Dictionary<string, MapDrawing>();
        private int OverlayID;

        // Transparent base texture
        private Texture2D TransparentTex;
        // Mask for the Overlay
        private Sprite CircleMask;

        // Intermediate textures for the manager to draw on
        private Texture2D OverlayTex;
        private Texture2D MainTex;
        private Texture2D HeightFilter;
        private Texture2D ForestFilter;
        private Texture2D FogFilter;

        // Materials that have shaders used to blit overlays onto the minimap.
        private Material ComposeOverlayMaterial;
        private Material ComposeMainMaterial;
        private Material ComposeHeightMaterial;
        private Material ComposeForestMaterial;
        private Material ComposeFogMaterial;

        // Current component for the MinimapOverlayPanel
        private MinimapOverlayPanel OverlayPanel;

        // Overlay GameObjects
        private GameObject OverlayLarge;
        private GameObject OverlaySmall;

        /// <summary>
        ///     Creates the Overlays and registers hooks.
        /// </summary>
        public void Init()
        {
            // Setup hooks
            On.Minimap.Start += Minimap_Start;
            On.Minimap.LoadMapData += Minimap_LoadMapData;
            On.Minimap.OnDestroy += Minimap_OnDestroy;
            On.Minimap.CenterMap += Minimap_CenterMap;
            
            // Setup methods to properly explore fog, and keep vanilla copy of fog properly updated.
            On.Minimap.Explore_int_int += Minimap_Explore_Point;
            On.Minimap.Explore_Vector3_float += Minimap_Explore_Radius;
            On.Minimap.ExploreOthers += Minimap_ExploreOthers;
            On.Minimap.ExploreAll += Minimap_ExploreAll;
            On.Minimap.AddSharedMapData += Minimap_AddSharedMapData;
            On.Minimap.Reset += Minimap_Reset;

            // Load texture with all pixels (RGBA) set to 0f and our overlay panel.
            var bundle = AssetUtils.LoadAssetBundleFromResources("minimapmanager", typeof(MinimapManager).Assembly);

            TransparentTex = bundle.LoadAsset<Texture2D>("2048x2048_clear");
            
            var panelGameObject = bundle.LoadAsset<GameObject>("MinimapOverlayPanel");
            PrefabManager.Instance.AddPrefab(new CustomPrefab(panelGameObject, false));

            bundle.Unload(false);

            // Create a harmony patch to watch Texture2D.Apply calls
            Harmony.CreateAndPatchAll(typeof(Texture2D_Apply));
        }

        /// <summary>
        ///     Create a new MapOverlay with a default overlay name
        /// </summary>
        /// <returns>Reference to MapOverlay for modder to edit</returns>
        public MapOverlay GetMapOverlay()
        {
            return GetMapOverlay(OverlayNamePrefix + OverlayID++);
        }

        /// <summary>
        ///     Create a new MapOverlay with a custom overlay name
        /// </summary>
        /// <param name="name">Custom name for the MapOverlay</param>
        /// <returns>Reference to MapOverlay for modder to edit</returns>
        public MapOverlay GetMapOverlay(string name)
        {
            if (Overlays.ContainsKey(name))
            {
                Logger.LogDebug($"Returning existing overlay with name {name}");
                return Overlays[name];
            }

            // if this is the first MapOverlay we are adding, then we initialize all variables.
            if (Overlays.Count == 0)
            {
                SetupOverlays();
            }

            MapOverlay ret = new MapOverlay
            {
                Name = name,
                Enabled = true,
                TextureSize = TextureSize
            };
            Overlays.Add(name, ret);
            AddOverlayToGUI(ret);
            return ret;
        }
        
        /// <summary>
        ///     Create a new MapDrawing with a default overlay name
        /// </summary>
        /// <returns>Reference to MapDrawing for modder to edit</returns>
        public MapDrawing GetMapDrawing()
        {
            return GetMapDrawing(OverlayNamePrefix + OverlayID++);
        }

        /// <summary>
        ///     Create a new MapDrawing with a custom overlay name
        /// </summary>
        /// <param name="name">Custom name for the MapDrawing</param>
        /// <returns>Reference to MapDrawing for modder to edit</returns>
        public MapDrawing GetMapDrawing(string name)
        {
            if (Drawings.ContainsKey(name))
            {
                Logger.LogDebug($"Returning existing overlay with name {name}");
                return Drawings[name];
            }

            // if this is the first MapDrawing we are adding, then we initialize all variables.
            if (Drawings.Count == 0)
            {
                SetupDrawings();
            }

            MapDrawing ret = new MapDrawing
            {
                Name = name,
                Enabled = true,
                TextureSize = TextureSize
            };
            Drawings.Add(name, ret);
            AddDrawingToGUI(ret);
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
        ///     Causes MapManager to stop updating the MapDrawing object and removes this Manager's reference to that drawing.
        ///     A mod could still hold references and keep the object alive.
        /// </summary>
        /// <param name="name">The name of the MapDrawing to be removed</param>
        /// <returns>True if removal was successful. False if there was an error removing the object from the internal dict.</returns>
        public bool RemoveMapDrawing(string name)
        {
            return Drawings.Remove(name);
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
        ///     Return a list of all current MapDrawing names
        /// </summary>
        /// <returns>List of names</returns>
        public List<string> GetDrawingNames()
        {
            List<string> res = new List<string>();
            foreach (var ovl in Drawings)
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
        ///     Start our watchdog on <see cref="Minimap.Start"/>
        /// </summary>
        private void Minimap_Start(On.Minimap.orig_Start orig, Minimap self)
        {
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
        ///     Setup textures and GUI on <see cref="Minimap.LoadMapData"/>
        /// </summary>
        private void Minimap_LoadMapData(On.Minimap.orig_LoadMapData orig, Minimap self)
        {
            orig(self);
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

        private void StartWatchdog()
        {
            IEnumerator watchdog()
            {
                while (true)
                {
                    yield return null;
                    if (!Overlays.Values.Any(x => x.Dirty) && !Drawings.Values.Any(x => x.Dirty))
                    {
                        continue;
                    }
                    if (Overlays.Values.Any(x => x.OverlayDirty))
                    {
                        yield return DrawOverlay(OverlayTex);
                    }
                    foreach (var overlay in Overlays.Values)
                    {
                        overlay.Dirty = false;
                    }

                    // Do not try to update drawings if there are none.
                    if (Drawings.Count == 0)
                    {
                        continue;
                    }

                    if (Drawings.Values.Any(x => x.MainDirty))
                    {
                        yield return DrawMain(MainTex);
                    }
                    if (Drawings.Values.Any(x => x.HeightDirty))
                    {
                        yield return DrawHeight(HeightFilter);
                    }
                    if (Drawings.Values.Any(x => x.ForestDirty))
                    {
                        yield return DrawForestFilter(ForestFilter);
                    }
                    if (Drawings.Values.Any(x => x.FogDirty))
                    {
                        yield return DrawFogFilter(FogFilter);
                    }

                    foreach (var overlay in Drawings.Values)
                    {
                        overlay.Dirty = false;
                    }
                }
            }
            Minimap.instance.StartCoroutine(watchdog());
        }

        private IEnumerator DrawOverlay(Texture2D intermediate)
        {
            Logger.LogDebug("Redraw Overlay");
            var watch = new System.Diagnostics.Stopwatch(); watch.Start();

            Graphics.CopyTexture(TransparentTex, intermediate); // Reset intermediate texture
            foreach (var overlay in Overlays.Values.Where(x => x.Enabled))
            {
                DrawLayer(overlay.OverlayTex, intermediate, ComposeOverlayMaterial);
            }
            watch.Stop();
            Logger.LogDebug($"DrawOverlay loop took {watch.ElapsedMilliseconds}ms time");

            yield return null;
        }

        private IEnumerator DrawMain(Texture2D intermediate)
        {
            Logger.LogDebug("Redraw Main");
            var watch = new System.Diagnostics.Stopwatch(); watch.Start();

            Graphics.CopyTexture(Minimap.instance.m_mapTexture, intermediate); // Reset vanilla texture to backup
            foreach (var overlay in Drawings.Values.Where(x => x.Enabled && x.MainEnabled))
            {
                DrawLayer(overlay.MainTex, intermediate, ComposeMainMaterial);
            }
            watch.Stop();
            Logger.LogDebug($"DrawMain loop took {watch.ElapsedMilliseconds}ms time");

            yield return null;
        }

        private IEnumerator DrawHeight(Texture2D intermediate)
        {
            Logger.LogDebug("Redraw Height");
            var watch = new System.Diagnostics.Stopwatch(); watch.Start();

            Graphics.CopyTexture(Minimap.instance.m_heightTexture, intermediate); // Reset vanilla texture to backup
            foreach (var overlay in Drawings.Values.Where(x => x.Enabled && x.HeightEnabled))
            {
                DrawLayer(overlay.HeightFilter, intermediate, ComposeHeightMaterial,
                    RenderTextureFormat.RFloat);
            }
            watch.Stop();
            Logger.LogDebug($"DrawHeight loop took {watch.ElapsedMilliseconds}ms time");

            yield return null;
        }

        private IEnumerator DrawForestFilter(Texture2D intermediate)
        {
            Logger.LogDebug("Redraw Forest");
            var watch = new System.Diagnostics.Stopwatch(); watch.Start();

            Graphics.CopyTexture(Minimap.instance.m_forestMaskTexture, intermediate); // Reset vanilla texture to backup
            foreach (var overlay in Drawings.Values.Where(x => x.Enabled && x.ForestEnabled))
            {
                DrawLayer(overlay.ForestFilter, intermediate, ComposeForestMaterial);
            }
            watch.Stop();
            Logger.LogDebug($"DrawForest loop took {watch.ElapsedMilliseconds}ms time");

            yield return null;
        }

        private IEnumerator DrawFogFilter(Texture2D intermediate)
        {
            Logger.LogDebug("Redraw Fog");
            var watch = new System.Diagnostics.Stopwatch(); watch.Start();

            Graphics.CopyTexture(Minimap.instance.m_fogTexture, intermediate); // Reset vanilla texture to backup
            foreach (var overlay in Drawings.Values.Where(x => x.Enabled && x.FogEnabled))
            {
                DrawLayer(overlay.FogFilter, intermediate, ComposeFogMaterial);
            }
            watch.Stop();
            Logger.LogDebug($"DrawFog loop took {watch.ElapsedMilliseconds}ms time");

            yield return null;
        }

        private void DrawLayer(Texture2D layer, Texture2D dest, Material mat, RenderTextureFormat format = RenderTextureFormat.Default)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(TextureSize, TextureSize, 0, format, RenderTextureReadWrite.Default);

            // Blit sets the overlay texture as _Maintex on the shader then computes the frag function of the shader, setting the result to tmp.
            if (mat != null)
            {
                Graphics.Blit(layer, tmp, mat);
            }
            else
            {
                Graphics.Blit(layer, tmp);
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

        /// <summary>
        ///     Return whether the Drawings functionality is active.
        /// </summary>
        /// <returns></returns>
        private bool DrawingsActive()
        {
            return Drawings.Count > 0;
        }

        /// <summary>
        ///     Called when the first MapDraw object is created.
        ///     Initializes all variables required for MapDraw objects to be rendered.
        ///     Changes the behaviour of some vanilla shaders.
        /// </summary>
        private void SetupDrawings()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            Logger.LogDebug("Setting up MapDrawings");

            MainTex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: false);
            HeightFilter = new Texture2D(TextureSize, TextureSize, TextureFormat.RFloat, mipChain: false);
            ForestFilter = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: false);
            FogFilter = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: false);

            var bundle = AssetUtils.LoadAssetBundleFromResources("minimapmanager", typeof(MinimapManager).Assembly);

            var composeMainShader = bundle.LoadAsset<Shader>("MinimapComposeMain");
            var composeHeightShader = bundle.LoadAsset<Shader>("MinimapComposeHeight");
            var composeForestShader = bundle.LoadAsset<Shader>("MinimapComposeForest");
            var composeFogShader = bundle.LoadAsset<Shader>("MinimapComposeFog");

            bundle.Unload(false);

            ComposeMainMaterial = new Material(composeMainShader);
            ComposeHeightMaterial = new Material(composeHeightShader);
            ComposeForestMaterial = new Material(composeForestShader);
            ComposeFogMaterial = new Material(composeFogShader);

            ComposeMainMaterial.SetTexture("_VanillaTex", MainTex);
            ComposeHeightMaterial.SetTexture("_VanillaTex", HeightFilter);
            ComposeForestMaterial.SetTexture("_VanillaTex", ForestFilter);
            ComposeFogMaterial.SetTexture("_VanillaTex", FogFilter);

            Graphics.CopyTexture(Minimap.instance.m_mapTexture, MainTex);
            Graphics.CopyTexture(Minimap.instance.m_heightTexture, HeightFilter);
            Graphics.CopyTexture(Minimap.instance.m_forestMaskTexture, ForestFilter);
            Graphics.CopyTexture(Minimap.instance.m_fogTexture, FogFilter);

            // Set own textures to the vanilla materials
            Minimap.instance.m_mapLargeShader.SetTexture("_MainTex", MainTex);
            Minimap.instance.m_mapSmallShader.SetTexture("_MainTex", MainTex);
            Minimap.instance.m_mapLargeShader.SetTexture("_HeightTex", HeightFilter);
            Minimap.instance.m_mapSmallShader.SetTexture("_HeightTex", HeightFilter);
            Minimap.instance.m_mapLargeShader.SetTexture("_MaskTex", ForestFilter);
            Minimap.instance.m_mapSmallShader.SetTexture("_MaskTex", ForestFilter);
            Minimap.instance.m_mapLargeShader.SetTexture("_FogTex", FogFilter);
            Minimap.instance.m_mapSmallShader.SetTexture("_FogTex", FogFilter);
            
            watch.Stop();
            Logger.LogDebug($"Setup took {watch.ElapsedMilliseconds}ms time");
        }

        private void SetupOverlays()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            Logger.LogDebug("Setting up MapOverlays");

            // Create intermediate textures to draw on
            OverlayTex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: false);

            var bundle = AssetUtils.LoadAssetBundleFromResources("minimapmanager", typeof(MinimapManager).Assembly);

            // Load mask image for the map overlay
            CircleMask = bundle.LoadAsset<Sprite>("CircleMask");

            // Create overlay material and shader
            var composeOverlayShader = bundle.LoadAsset<Shader>("MinimapComposeOverlay");
            ComposeOverlayMaterial = new Material(composeOverlayShader);
            ComposeOverlayMaterial.SetTexture("_VanillaTex", OverlayTex);

            bundle.Unload(false);

            // Copy vanilla textures
            Graphics.CopyTexture(TransparentTex, OverlayTex);

            // Create custom overlay GOs
            OverlayLarge = new GameObject("CustomLayerLarge");
            var rectLarge = OverlayLarge.AddComponent<RectTransform>();
            rectLarge.SetParent(Minimap.instance.m_mapImageLarge.transform, false);
            rectLarge.SetAsFirstSibling();
            rectLarge.anchorMin = Vector2.zero;
            rectLarge.anchorMax = Vector2.one;
            rectLarge.offsetMin = Vector2.zero;
            rectLarge.offsetMax = Vector2.zero;
            var maskImage = OverlayLarge.AddComponent<Image>();
            maskImage.sprite = CircleMask;
            maskImage.preserveAspect = true;
            maskImage.raycastTarget = false;
            var mask = OverlayLarge.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var rawLarge = new GameObject("RawImageLarge");
            var rawRectLarge = rawLarge.AddComponent<RectTransform>();
            rawRectLarge.SetParent(rectLarge, false);
            rawRectLarge.anchorMin = Vector2.zero;
            rawRectLarge.anchorMax = Vector2.one;
            rawRectLarge.offsetMin = Vector2.zero;
            rawRectLarge.offsetMax = Vector2.zero;
            var imageLarge = rawLarge.AddComponent<RawImage>();
            imageLarge.texture = OverlayTex;
            imageLarge.material = null;
            imageLarge.raycastTarget = false;
            imageLarge.maskable = true;
            Rect imageLargeRect = imageLarge.uvRect;
            imageLargeRect.width = rectLarge.rect.width / rectLarge.rect.height;
            imageLargeRect.height = 1;
            imageLargeRect.center = new Vector2(0.5f, 0.5f);
            imageLarge.uvRect = imageLargeRect;

            OverlaySmall = new GameObject("CustomLayerSmall");
            var rectSmall = OverlaySmall.AddComponent<RectTransform>();
            rectSmall.SetParent(Minimap.instance.m_mapImageSmall.transform, false);
            rectSmall.SetAsFirstSibling();
            rectSmall.anchorMin = Vector2.zero;
            rectSmall.anchorMax = Vector2.one;
            var imageSmall = OverlaySmall.AddComponent<RawImage>();
            imageSmall.texture = OverlayTex;
            imageSmall.material = null;
            imageSmall.raycastTarget = false;

            watch.Stop();
            Logger.LogDebug($"Setup took {watch.ElapsedMilliseconds}ms time");
        }

        private void SetupGUI()
        {
            var basePanel = PrefabManager.Instance.GetPrefab("MinimapOverlayPanel");
            var panel = Object.Instantiate(basePanel, Minimap.instance.m_mapLarge.transform, false);
            panel.SetActive(false);
            OverlayPanel = panel.GetComponent<MinimapOverlayPanel>();
            GUIManager.Instance.ApplyButtonStyle(OverlayPanel.Button);
            GUIManager.Instance.ApplyToogleStyle(OverlayPanel.BaseToggle);
            GUIManager.Instance.ApplyTextStyle(OverlayPanel.BaseModText);
        }

        private void AddOverlayToGUI(MapOverlay ovl)
        {
            var toggle = OverlayPanel?.AddOverlayToggle(ovl.SourceMod.Name, ovl.Name);
            toggle?.onValueChanged.AddListener(active =>
            {
                ovl.Enabled = active;
            });
            OverlayPanel?.gameObject.SetActive(true);
        }

        private void AddDrawingToGUI(MapDrawing ovl)
        {
            var toggle = OverlayPanel?.AddOverlayToggle(ovl.SourceMod.Name, ovl.Name);
            toggle?.onValueChanged.AddListener(active =>
            {
                ovl.Enabled = active;
            });
            OverlayPanel?.gameObject.SetActive(true);
        }

        private void Minimap_CenterMap(On.Minimap.orig_CenterMap orig, Minimap self, Vector3 centerpoint)
        {
            orig(self, centerpoint);

            if (!OverlayLarge)
            {
                return;
            }

            self.WorldToMapPoint(centerpoint, out var mx, out var my);
            RectTransform rectTransform = OverlayLarge.transform as RectTransform;
            float aspect = rectTransform.rect.width / rectTransform.rect.height;

            float localZoom = 1 / self.m_largeZoom;

            OverlayLarge.transform.localScale = new Vector2(localZoom, localZoom);

            // WorldToPixel code without the Int conversions.
            int num = self.m_textureSize / 2;
            float ix = (centerpoint.x / self.m_pixelSize + (float)num);
            float iy = (centerpoint.z / self.m_pixelSize + (float)num);


            var offset = new Vector2();
            offset.x = TextureSize / 2f - ix;
            offset.x *= rectTransform.rect.width / TextureSize / aspect;
            offset.x *= localZoom;
            offset.y = TextureSize / 2f - iy;
            offset.y *= rectTransform.rect.height / TextureSize;
            offset.y *= localZoom;
            rectTransform.anchoredPosition = offset;

            Rect smallRect = self.m_mapImageSmall.uvRect;
            smallRect.width += self.m_smallZoom / 2f;
            smallRect.height += self.m_smallZoom / 2f;
            smallRect.center = new Vector2(mx, my);
            OverlaySmall.GetComponent<RawImage>().uvRect = smallRect;
        }

        private void Minimap_OnDestroy(On.Minimap.orig_OnDestroy orig, Minimap self)
        {
            orig(self);

            foreach (var overlay in Overlays.Values)
            {
                overlay.Destroy();
            }
            Instance.Overlays.Clear();

            foreach (var drawing in Drawings.Values)
            {
                drawing.Destroy();
            }
            Instance.Drawings.Clear();
        }

        private bool Minimap_AddSharedMapData(On.Minimap.orig_AddSharedMapData orig, Minimap self, byte[] dataArray)
        {
            bool t = orig(self, dataArray);
            if (MinimapManager.Instance.DrawingsActive())
            {
                FogFilter.Apply();
            }
            return t;
        }

        private bool Minimap_Explore_Point(On.Minimap.orig_Explore_int_int orig, Minimap self, int x, int y)
        {
            if (MinimapManager.Instance.DrawingsActive())
            {
                if (!self.m_explored[y * self.m_textureSize + x])
                {
                    FogFilter.SetPixel(x, y, FilterOff);
                }
            }
            return orig(self, x, y);
        }

        private void Minimap_Explore_Radius(On.Minimap.orig_Explore_Vector3_float orig, Minimap self, Vector3 v, float f)
        {
            orig(self, v, f);
            if (MinimapManager.Instance.DrawingsActive())
            {
                FogFilter.Apply();
            }
        }

        private void Minimap_ExploreAll(On.Minimap.orig_ExploreAll orig, Minimap self)
        {
            orig(self);
            if (MinimapManager.Instance.DrawingsActive())
            {
                FogFilter.Apply();
            }
        }

        private bool Minimap_ExploreOthers(On.Minimap.orig_ExploreOthers orig, Minimap self, int x, int y)
        {
            if (MinimapManager.Instance.DrawingsActive())
            {
                if (!self.m_explored[y * self.m_textureSize + x])
                {
                    FogFilter.SetPixel(x, y, FilterOff);
                }
            }

            return orig(self, x, y);
        }

        private void Minimap_Reset(On.Minimap.orig_Reset orig, Minimap self)
        {
            orig(self);
            if (MinimapManager.Instance.DrawingsActive())
            {
                FogFilter.SetPixels(self.m_fogTexture.GetPixels());
                FogFilter.Apply();
            }

        }


        /// <summary>
        ///     Object for modders to use to access and modify their Overlay.
        ///     Modders should modify the texture directly.
        ///     
        /// </summary>
        public class MapOverlay : MapOverlayBase
        {
            /// <summary>
            ///     Texture to draw overlay texture information to
            /// </summary>
            public Texture2D OverlayTex => _overlayTex ??= Create(Instance.TransparentTex);

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
            ///     Flag to determine if this overlay had changes since its last draw
            /// </summary>
            internal bool Dirty
            {
                get
                {
                    return OverlayDirty;
                }
                set
                {
                    _overlayDirty = value;
                }
            }

            internal bool OverlayDirty => _overlayTex != null && _overlayDirty;

            private Texture2D _overlayTex;
            private bool _enabled;
            internal bool _overlayDirty;

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
            }
            internal void Destroy()
            {
                if (_overlayTex != null)
                {
                    Object.DestroyImmediate(_overlayTex);
                }
            }
        }

        /// <summary>
        ///     Object for modders to use to access and modify their Overlay.
        ///     Modders should modify the texture directly.
        ///     
        /// </summary>
        public class MapDrawing : MapOverlayBase
        {
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
                    return MainDirty || HeightDirty || ForestDirty || FogDirty;
                }
                set
                {
                    _mainDirty = value;
                    _heightDirty = value;
                    _forestDirty = value;
                    _fogDirty = value;
                }
            }

            internal bool MainDirty => _mainTex != null && _mainDirty;
            internal bool HeightDirty => _heightFilter != null && _heightDirty;
            internal bool ForestDirty => _forestFilter != null && _forestDirty;
            internal bool FogDirty => _fogFilter != null && _fogDirty;

            private Texture2D _mainTex;
            private Texture2D _heightFilter;
            private Texture2D _forestFilter;
            private Texture2D _fogFilter;

            private bool _enabled;

            internal bool _mainDirty;
            internal bool _heightDirty;
            internal bool _forestDirty;
            internal bool _fogDirty;

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

            internal void Destroy()
            {
                if (_mainTex != null)
                {
                    Object.DestroyImmediate(_mainTex);
                }
                if (_heightFilter != null)
                {
                    Object.DestroyImmediate(_heightFilter);
                }
                if (_forestFilter != null)
                {
                    Object.DestroyImmediate(_forestFilter);
                }
                if (_fogFilter != null)
                {
                    Object.DestroyImmediate(_fogFilter);
                }
            }
        }

        /// <summary>
        ///     Overlay Base to inherit from
        ///     
        /// </summary>
        public class MapOverlayBase : CustomEntity
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
            ///     Helper function to create and copy overlay texture instances
            /// </summary>
            internal Texture2D Create(Texture2D van)
            {
                var t = new Texture2D(van.width, van.height, van.format, mipChain: false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    name = Name
                };
                Graphics.CopyTexture(Instance.TransparentTex, t);
                return t;
            }
        }

        /// <summary>
        ///     Set an overlay texture dirty on Apply
        /// </summary>
        [HarmonyPatch(typeof(Texture2D), "Apply", typeof(bool), typeof(bool))]
        private static class Texture2D_Apply
        {
            private static void Postfix(Texture2D __instance)
            {
                foreach (var overlay in Instance.Overlays.Values)
                {
                    overlay.SetTextureDirty(__instance);
                }
                foreach (var overlay in Instance.Drawings.Values)
                {
                    overlay.SetTextureDirty(__instance);
                }
            }
        }
    }
}
