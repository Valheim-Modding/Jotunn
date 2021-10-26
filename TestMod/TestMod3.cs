// JotunnLib
// a Valheim mod
// 
// File:    TestMod3.cs
// Project: TestMod

using BepInEx;
using Jotunn;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace TestMod3
{
/*    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.Patch)]
    internal class TestMod3 : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.testmod3";
        private const string ModName = "Jotunn Test Mod #3";
        private const string ModVersion = "0.1.0";

        public void Awake()
        {
            MapOverlayManager.OnVanillaMapAvailable += TestMapAvailiable;
            MapOverlayManager.OnVanillaMapDataLoaded += TestMapDataLoaded;
        }


        private void TestMapAvailiable()
        {
            Jotunn.Logger.LogInfo("Starting testMM");
            MapOverlayManager.MapOverlay t = MapOverlayManager.Instance.AddMapOverlay("test_overlay");
            Color32 c = new Color32(byte.MinValue, 50, byte.MaxValue, 100);
            FillImgWithColour(t, c);
            //MapOverlayManager.OnVanillaMapAvailable -= TestMapAvailiable;
        }

        // pins are only available once data is loaded.
        private void TestMapDataLoaded()
        {
            MapOverlayManager.MapOverlay t = MapOverlayManager.Instance.AddMapOverlay("test2_overlay");
            Color32 c = new Color32(byte.MinValue, 50, byte.MaxValue, 0);
            FillImgWithColour(t, c);
            DrawSquaresOnMarks(t);
            //MapOverlayManager.OnVanillaMapDataLoaded -= TestMapDataLoaded;
        }


        public void DrawSquaresOnMarks(MapOverlayManager.MapOverlay t)
        {
            Jotunn.Logger.LogInfo("Starting drawsquaresonmarks");
            foreach (var p in Minimap.instance.m_pins)
            {
                //Minimap.instance.pin
                if (p.m_name == "ch_test" || p.m_name == "CH_TEST")
                {
                    DrawSquare(t, MapOverlayManager.Instance.WorldToOverlayCoords(p.m_pos, t.TextureSize), Color.blue, 5);
                }
            }
            Jotunn.Logger.LogInfo("Finished drawsquaresonmarks");
        }

        public void FillImgWithColour(MapOverlayManager.MapOverlay ovl, Color32 c)
        {
            Jotunn.Logger.LogInfo("Starting fill img with colour");
            Color32[] arr = new Color32[ovl.TextureSize * ovl.TextureSize];
            for (int i = 0; i < ovl.TextureSize * ovl.TextureSize; i++)
            {
                //array2[i] = new Color32(byte.MinValue, 50, byte.MaxValue, 100);
                arr[i] = c;
            }
            ovl.Img.sprite.texture.SetPixels32(arr);
            ovl.Img.sprite.texture.Apply();
        }

        // where start coord is the bottom left coordinate
        private void DrawSquare(MapOverlayManager.MapOverlay ovl, Vector2 start, Color col, int square_size)
        {
            Jotunn.Logger.LogInfo("Starting drawsquare");
            for (float i = start.x; i < start.x + square_size; i++)
            {
                for (float j = start.y; j < start.y + square_size; j++)
                {
                    ovl.Img.sprite.texture.SetPixel((int)i, (int)j, col);
                }
            }
            ovl.Img.sprite.texture.Apply();
        }

    }*/
}
