using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MonoMod.RuntimeDetour;

namespace Jotunn.Managers
{
    class MapOverlayManager : IManager
    {
        public static event Action OnVanillaMapAvailable;

        private static MapManager _instance;
        private Dictionary<string, MapOverlay> img_dict;
        private int default_overlay_size = 2048;
        private int magic_scale_factor = 434;
        private int overlay_id = 0;
        private string overlay_name_prefix = "custom_map_overlay_";


        // Object for modders to use to access and modify their Overlay.
        //public class MapOverlay: MonoBehaviour
        public class MapOverlay
        {
            public GameObject helper;
            public Image img;
            public int texture_size;
            public string map_name;
            public bool enabled = true;
        }


        // Helper class that gets attached to the Minimap object on Minimap.Awake().
        // Used to give MapManager Update() functionality.
        private class MapManagerHelper : MonoBehaviour
        {
            public void Update()
            {
                MapManager.Instance.Update();
            }
        }

        public static MapManager Instance
        {
            get
            {
                if (_instance == null) { _instance = new MapManager(); }
                return _instance;
            }
        }


        public void Init()
        {
            //Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            img_dict = new Dictionary<string, MapOverlay>();

            using (new DetourContext(int.MaxValue - 1000))
            {
                On.Minimap.Start += Minimap_Start;
                On.Minimap.Start += InvokeOnVanillaMapAvailable;
            }
        }

        // Create a new mapoverlay with a default overlay name
        public MapOverlay AddMapOverlay()
        {
            return AddMapOverlay(overlay_name_prefix + overlay_id++);
        }

        // Create a new mapoverlay with a custom overlay name
        public MapOverlay AddMapOverlay(string name)
        {
            MapOverlay res = new MapOverlay();
            res.map_name = name;
            AddOverlay(res);
            return res;
        }

        // If a mod already has an instance of MapOverlay ready, they can simply call this function to register it and keep it updated.
        public void AddMapOverlay(MapOverlay ovl)
        {
            img_dict.Add(ovl.map_name, ovl);
        }


        /*
         *  Causes MapManager to stop updating the MapOverlay object. A mod could still hold references and keep the object alive.
         */
        public bool RemoveMapOverlay(string name)
        {
            return img_dict.Remove(name);
        }


        public MapOverlay GetMapOverlay(string name)
        {
            return img_dict[name];
        }


        // Input a World Coordinate and the size of the overlay texture to retrieve the translated overlay coordinates.
        public Vector2 WorldToOverlayCoords(Vector3 input, int text_size)
        {
            Minimap.instance.WorldToMapPoint(input, out var mx, out var my);
            return new Vector2(mx * text_size, my * text_size);
        }


        // Input an Overlay Coordinate and the size of the overlay texture to retrieve the translated World coordinates.
        public Vector3 OverlayToWorldCoords(Vector2 input, int text_size)
        {
            input.x /= text_size;
            input.y /= text_size;
            return Minimap.instance.MapPointToWorld(input.x, input.y);
        }


        /*
         *  All map overlays need to be updated in this way to keep them scaled and positioned properly when the minimap itself is moved / scaled.
         *  This function is public as it needs to be called by MapManagerHelper.
         *  Automatically scales and positions based on whether the large or small map is active.
         */
        public void Update()
        {
            RectTransform rect = ((Minimap.instance.m_mode == Minimap.MapMode.Large) ? Minimap.instance.m_pinRootLarge : Minimap.instance.m_pinRootSmall);
            RawImage rawImage = ((Minimap.instance.m_mode == Minimap.MapMode.Large) ? Minimap.instance.m_mapImageLarge : Minimap.instance.m_mapImageSmall);
            Minimap.instance.WorldToMapPoint(Vector3.zero, out var mx, out var my);
            Vector2 anchoredPosition = Minimap.instance.MapPointToLocalGuiPos(mx, my, rawImage);
            float zoom_factor = 1 / Minimap.instance.m_largeZoom; // not using m._smallZoom when using smaller map may lead to scaling bugs.
            float size = zoom_factor * 2 * magic_scale_factor;

            foreach (var m in img_dict)
            {
                m.Value.img.rectTransform.SetParent(rect);
                m.Value.img.rectTransform.anchoredPosition = anchoredPosition;
                m.Value.img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                m.Value.img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            }
        }

        // Helper function to set default properties of a MapOverlay.
        // Create a new image of our custom default size and set its anchor min/max to the bottom left.
        // Then add a reference of the mapoverlay to our dict so we can update it later.
        private void AddOverlay(MapOverlay ovl)
        {

            Texture2D drawn_tex = new Texture2D(default_overlay_size, default_overlay_size, TextureFormat.RGBA32, mipChain: false);
            ovl.helper = new GameObject();
            ovl.img = ovl.helper.AddComponent<Image>();
            ovl.texture_size = default_overlay_size;
            ovl.img.sprite = Sprite.Create(drawn_tex, new Rect(0f, 0f, default_overlay_size, default_overlay_size), Vector2.zero);
            ovl.img.rectTransform.anchorMin = new Vector2(0f, 0f);
            ovl.img.rectTransform.anchorMax = new Vector2(0f, 0f);

            img_dict.Add(ovl.map_name, ovl);
        }

        private void InvokeOnVanillaMapAvailable(On.Minimap.orig_Start orig, Minimap self)
        {
            orig(self);
            OnVanillaMapAvailable?.SafeInvoke();
        }

        // Monomod hook into the Minimap Awake method. MapManager adds its helper component after the Minimap does its thing.
        private void Minimap_Start(On.Minimap.orig_Start orig, Minimap self)
        {
            orig(self);
            self.gameObject.AddComponent<MapManagerHelper>();
        }
    }
}
