// JotunnLib
// a Valheim mod
// 
// File:    TestMod3.cs
// Project: TestMod

using System.Collections.Generic;
using BepInEx;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace TestMod3
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.Patch)]
    internal class TestMod3 : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.testmod3";
        private const string ModName = "Jotunn Test Mod #3";
        private const string ModVersion = "0.1.0";
        /*
         * 
         * 
There is still a fairly long list of features needed to be complete before this Manager is released. Eg:

- Overlay enable/disable GUI interface
- Proper documentation for methods + a bit of clean up
- Properly update / revert Fog layer to vanilla state
- Properly revert Background/Water/Mountain textures to vanilla states

Additional Future Changes which may be added later:

- A smooth brain "make an overlay" helper
- Support for space/cloud/fog colours
         */

        public void Awake()
        {
            //MapOverlayManager.OnVanillaMapAvailable += TestMapAvailiable;
            MinimapManager.OnVanillaMapDataLoaded += TestCoMapLoaded;
            CommandManager.Instance.AddConsoleCommand(new CHCommands_toggle());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_flatten());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_squares());
        }


        private void TestMapAvailiable()
        {
            Jotunn.Logger.LogInfo("Starting testMM");
            MinimapManager.MapOverlay t = MinimapManager.Instance.AddMapOverlay("test_overlay");
            Color32 c = new Color32(byte.MinValue, 50, byte.MaxValue, 100);
            FillImgWithColour(t.MainImg, c);
            MinimapManager.Instance.ComposeOverlays();
        }

        private void TestCoMapLoaded()
        {
            StartCoroutine(CoTestMapDataLoaded());
        }

        // pins are only available once data is loaded.
        private void TestMapDataLoaded()
        {
            MinimapManager.MapOverlay t = MinimapManager.Instance.AddMapOverlay("test2_overlay");
            t.MainFlag = true;
            t.FogFlag = true;
            t.ForestFlag = true;
            t.BackgroundFlag = true;
            t.HeightFlag = true;
            Color32 c = new Color32(byte.MinValue, 0, byte.MaxValue, 100);
            Color meadowColour = new Color(31, 0, 0);

            FillImgWithColour(t.MainImg, c);
            FillQuarterImgWithColour(t.FogFilter, c);
            //FillImgWithColour(t.FilterHeight, meadowColour);
            DrawSquaresOnMarks(t);
            MinimapManager.Instance.ComposeOverlays();
        }

        private IEnumerator<WaitForSeconds> CoTestMapDataLoaded()
        {
            MinimapManager.MapOverlay t = MinimapManager.Instance.AddMapOverlay("test2_overlay");
            yield return null;
            t.MainFlag = true;
            t.FogFlag = true;
            t.ForestFlag = true;
            t.BackgroundFlag = true;
            t.HeightFlag = true;
            Color32 c = new Color32(byte.MinValue, 0, byte.MaxValue, 100);
            Color meadowColour = new Color(31, 0, 0);
            //StartCoroutine(CoFillImgWithColour(t.MainImg, c));
            FillImgWithColour(t.MainImg, c);
            yield return null;
            DrawSquaresOnMarks(t);
            yield return null;
            MinimapManager.Instance.ComposeOverlays();
            yield return null;
        }


        public void DrawSquaresOnMarks(MinimapManager.MapOverlay ovl)
        {
            Jotunn.Logger.LogInfo("Starting drawsquaresonmarks");
            foreach (var p in Minimap.instance.m_pins)
            {
                    DrawSquare(ovl.MainImg, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), Color.red, 10);
                    DrawSquare(ovl.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), Color.clear, 10);
                    DrawSquare(ovl.BackgroundImg, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), Color.clear, 10);
                    DrawSquare(ovl.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), new Color(31, 0, 0), 10);
            }
            Jotunn.Logger.LogInfo("Finished drawsquaresonmarks");
        }

        public IEnumerator<WaitForSeconds> CoFillImgWithColour(Texture2D tex, Color c)
        {
            Jotunn.Logger.LogInfo("Starting fill img with colour");
            for (int i = 0; i < tex.width; i++)
            {
                for (int j = 0; j < tex.height; j++)
                {
                    tex.SetPixel(i, j, c);
                }
                yield return null;
            }
            tex.Apply();
            yield return null;
        }

        public void FillImgWithColour(Texture2D tex, Color c)
        {
            Jotunn.Logger.LogInfo("Starting fill img with colour");
            for(int i = 0; i < tex.width; i++)
            {
                for(int j = 0; j < tex.height; j++)
                {
                    tex.SetPixel(i, j, c);
                }
            }
            tex.Apply();
        }

        public void FillQuarterImgWithColour(Texture2D tex, Color c)
        {
            Jotunn.Logger.LogInfo("Starting fill quarter img with colour");
            for (int i = 0; i < tex.width/2; i++)
            {
                for (int j = 0; j < tex.height/2; j++)
                {
                    tex.SetPixel(i, j, c);
                }
            }
            tex.Apply();
        }


        // where start coord is the bottom left coordinate
        private void DrawSquare(Texture2D tex, Vector2 start, Color col, int square_size)
        {
            Jotunn.Logger.LogInfo($"Starting drawsquare at {start}");
            for (float i = start.x; i < start.x + square_size; i++)
            {
                for (float j = start.y; j < start.y + square_size; j++)
                {
                    tex.SetPixel((int)i, (int)j, col);
                }
            }
            tex.Apply();
        }

        class CHCommands_toggle : ConsoleCommand
        {
            public override string Name => "ch";

            public override string Help => "run the channel helper";

            public override void Run(string[] args)
            {
                int strategy = int.Parse(args[0]);
                Console.instance.Print($"using strategy {strategy}");
                var t = MinimapManager.Instance.GetMapOverlay("test2_overlay");
                t.Enabled = !t.Enabled; // toggle
                MinimapManager.Instance.ComposeOverlays(strategy);
            }
        }


        class CHCommands_squares : ConsoleCommand
        {
            public override string Name => "chsq";

            public override string Help => "run the channel helper";

            public override void Run(string[] args)
            {
                int strategy = int.Parse(args[0]);
                Console.instance.Print($"using strategy {strategy}");
                var ovl = MinimapManager.Instance.AddMapOverlay("testsquares_overlay");
                ovl.Enabled = !ovl.Enabled; // toggle
                if (ovl.Enabled)
                {
                    ovl.MainFlag = true;
                    foreach (var p in Minimap.instance.m_pins)
                    {
                        DrawSquare(ovl.MainImg, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), Color.red, 10);
                    }
                }
                MinimapManager.Instance.ComposeOverlays(strategy);
            }
            private void DrawSquare(Texture2D tex, Vector2 start, Color col, int square_size)
            {
                Jotunn.Logger.LogInfo($"Starting drawsquare at {start}");
                for (float i = start.x; i < start.x + square_size; i++)
                {
                    for (float j = start.y; j < start.y + square_size; j++)
                    {
                        tex.SetPixel((int)i, (int)j, col);
                    }
                }
                tex.Apply();
            }
        }


        class CHCommands_flatten : ConsoleCommand
        {
            // NOTE: call this command twice to get it to display.
            public override string Name => "chf";

            public override string Help => "run the channel helper";

            public override void Run(string[] args)
            {
                var t = MinimapManager.Instance.AddMapOverlay("testflatten");
                t.HeightFlag = true;
                if (t.Enabled)
                {
                    for(int i = 0; i < t.TextureSize; i++)
                    {
                        for(int j = 0; j < t.TextureSize; j++)
                        {
                            t.HeightFilter.SetPixel(i, j, new Color(31, 0, 0));
                        }
                    }
                    t.HeightFilter.Apply();
                }

                t.Enabled = !t.Enabled; // toggle
                MinimapManager.Instance.ComposeOverlays();
            }
        }



    }
}
