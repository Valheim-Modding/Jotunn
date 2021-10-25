using System;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for adding custom Map Overlays to the game.
    /// </summary>
    internal class MapOverlayManager : IManager
    {
        private static MapOverlayManager _instance;

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static MapOverlayManager Instance
        {
            get
            {
                if (_instance == null) { _instance = new MapOverlayManager(); }
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
        
        private const int DefaultOverlaySize = 2048;
        private const int MagicScaleFactorLarge = 434; // scale factor for large minimap
        private const int MagicScaleFactorSmall = 94;  // scale factor for small minimap
        private const string OverlayNamePrefix = "custom_map_overlay_";

        private Dictionary<string, MapOverlay> Overlays = new Dictionary<string, MapOverlay>();
        private int OverlayID;
        
        /// <summary>
        ///     Object for modders to use to access and modify their Overlay.
        ///     Modders should modify the Image's shader's texture directly.
        ///     MapName and TextureSize should not be modified.
        /// </summary>
        public class MapOverlay
        {
            /// <summary>
            ///     Internal Helper GameObject to hold the Image component
            /// </summary>
            internal GameObject Helper;

            /// <summary>
            ///     Unique ID per overlay
            /// </summary>
            internal string MapName;

            /// <summary>
            ///     Initial texture size to calculate the relative drawing position
            /// </summary>
            public int TextureSize { get; internal set; }

            /// <summary>
            ///     Image component holding the overlay texture data
            /// </summary>
            public Image Img { get; internal set; }
        }
        
        /// <summary>
        ///     Creates the Overlays and registers hooks.
        /// </summary>
        public void Init()
        {
            using (new DetourContext(int.MaxValue - 1000))
            {
                On.Minimap.Start += Minimap_Start;
                On.Minimap.Start += InvokeOnVanillaMapAvailable;
                On.Minimap.LoadMapData += InvokeOnVanillaMapDataLoaded;
            }

            SceneManager.activeSceneChanged += (current, next) => Instance.Overlays.Clear();
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
        ///     Helper function to set pixels on the overlay image completely transparent if there is map fog over that pixel.
        ///     The end effect is to "put fog over top of the overlay".
        /// </summary>
        /// <param name="ovl">The overlay to place fog on top of</param>
        public void MaskWithFog(MapOverlay ovl)
        {
            int scale = ovl.TextureSize / Minimap.instance.m_textureSize;  // 2048/256=8, 8192/256=32, so for a small overlay each fog pixel corresponds to 8 overlay pixels.

            for (int i = 0; i < Minimap.instance.m_textureSize; i++)
            {
                for (int j = 0; j < Minimap.instance.m_textureSize; j++)
                {
                    if (!Minimap.instance.m_explored[j * Minimap.instance.m_textureSize + i])
                    {
                        // once we find an unexplored pixel we translate to our overlay and draw that overlay pixel as completely transparent.
                        // multiple by scale, then set all pixels from point to point+scale.
                        for (int m = i * scale; m < (i * scale + scale); m++)
                        {
                            for (int n = j * scale; n < (j * scale + scale); n++)
                            {
                                ovl.Img.sprite.texture.SetPixel(m, n, Color.clear);
                            }
                        }
                    }
                }
            }
            ovl.Img.sprite.texture.Apply();
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
        ///     All map overlays need to be updated in this way to keep them scaled and positioned properly when the minimap itself is moved / scaled.
        ///     This function is public as it needs to be called by MapManagerHelper.
        ///     Automatically scales and positions based on whether the large or small map is active.
        /// </summary>
        private void Update()
        {
            RectTransform rect = ((Minimap.instance.m_mode == Minimap.MapMode.Large) ? Minimap.instance.m_pinRootLarge : Minimap.instance.m_pinRootSmall);
            RawImage rawImage = ((Minimap.instance.m_mode == Minimap.MapMode.Large) ? Minimap.instance.m_mapImageLarge : Minimap.instance.m_mapImageSmall);
            Minimap.instance.WorldToMapPoint(Vector3.zero, out var mx, out var my);
            Vector2 anchoredPosition = Minimap.instance.MapPointToLocalGuiPos(mx, my, rawImage);
            float zoom = ((Minimap.instance.m_mode == Minimap.MapMode.Large) ? Minimap.instance.m_largeZoom : Minimap.instance.m_smallZoom);
            int msf = (Minimap.instance.m_mode == Minimap.MapMode.Large) ? MagicScaleFactorLarge : MagicScaleFactorSmall;
            float zoomFactor = 1 / zoom;
            float size = zoomFactor * 2 * msf;

            foreach (var m in Overlays)
            {
                m.Value.Img.rectTransform.SetParent(rect);
                m.Value.Img.rectTransform.anchoredPosition = anchoredPosition;
                m.Value.Img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                m.Value.Img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            }
        }


        /// <summary>
        ///     Helper function to set default properties of a MapOverlay.
        ///     Create a new image of our custom default size and set its anchor min/max to the bottom left.
        ///     Then add a reference of the mapoverlay to our dict so we can update it later.
        /// </summary>
        /// <param name="ovl">The overlay to be added</param>
        private void AddOverlay(MapOverlay ovl)
        {
            Texture2D drawn_tex = new Texture2D(DefaultOverlaySize, DefaultOverlaySize, TextureFormat.RGBA32, mipChain: false);
            ovl.Helper = new GameObject();
            ovl.Img = ovl.Helper.AddComponent<Image>();
            ovl.TextureSize = DefaultOverlaySize;
            ovl.Img.sprite = Sprite.Create(drawn_tex, new Rect(0f, 0f, DefaultOverlaySize, DefaultOverlaySize), Vector2.zero);
            ovl.Img.rectTransform.anchorMin = new Vector2(0f, 0f);
            ovl.Img.rectTransform.anchorMax = new Vector2(0f, 0f);

            Overlays.Add(ovl.MapName, ovl);
        }

        /// <summary>
        ///     Safely invoke OnVanillaMapAvailable event.
        /// </summary>
        private void InvokeOnVanillaMapAvailable(On.Minimap.orig_Start orig, Minimap self)
        {
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
        
        /// <summary>
        ///     Monomod hook into the Minimap Awake method. MapManager adds its helper component after the Minimap does its thing.
        /// </summary>
        private void Minimap_Start(On.Minimap.orig_Start orig, Minimap self)
        {
            orig(self);
            self.gameObject.AddComponent<MapManagerHelper>();
        }
        
        /// <summary>
        ///         Helper class that gets attached to the Minimap object on Minimap.Awake()
        ///         Used to give MapManager Update() functionality
        /// </summary>
        private class MapManagerHelper : MonoBehaviour
        {
            private void Update()
            {
                Instance.Update();
            }
        }
    }
}
