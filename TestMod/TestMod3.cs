// JotunnLib
// a Valheim mod
// 
// File:    TestMod3.cs
// Project: TestMod

using System.Collections.Generic;
using System.Linq;
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
            There is still a fairly long list of features needed to be complete before this Manager is released. Eg:
                - Overlay enable/disable GUI interface
                - Proper documentation for methods + a bit of clean up
         */

        private static MinimapManager.MapOverlay squarequadoverlay;
        private static MinimapManager.MapOverlay squareoverlay;
        private static MinimapManager.MapOverlay flatsquareoverlay;
        private static MinimapManager.MapOverlay alphaoverlay;
        private static MinimapManager.MapOverlay flattenoverlay;
        private static MinimapManager.MapOverlay quadtest0;
        private static MinimapManager.MapOverlay quadtest1;
        private static MinimapManager.MapOverlay quadtest2;
        private static MinimapManager.MapOverlay quadtest3;
        private static MinimapManager.MapOverlay backgroundtest;

        private Color semiblue = new Color(0, 0, 255, 50);
        private static Color meadowHeight = new Color(32, 0, 0, 255);
        private static Color FilterOn = new Color(1f, 0f, 0f, 255f);
        private static Color FilterOff = new Color(0f, 0f, 0f, 255f);

        public void Awake()
        {
            CommandManager.Instance.AddConsoleCommand(new CHCommands_squares());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_flatten());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_alpha());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_flatsquares());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_toggle());

            MinimapManager.OnVanillaMapDataLoaded += MinimapManager_OnVanillaMapDataLoaded;
        }

        private void MinimapManager_OnVanillaMapDataLoaded()
        {


            // test drawing flat squares with no fog and no forest
            //flatsquareoverlay = MinimapManager.Instance.AddMapOverlay("testsquareflatten_overlay");
            //DrawSquaresOnMapPins(Color.blue, flatsquareoverlay, extras: true);

            // test just drawing squares
            //squareoverlay = MinimapManager.Instance.AddMapOverlay("testsquares_overlay");
            //DrawSquaresOnMapPins(Color.red, squareoverlay);

            // test transparency
            //alphaoverlay = MinimapManager.Instance.AddMapOverlay("alpha_overlay");
            //DrawQuarterQuadrant(alphaoverlay.MainTex, semiblue);

            DrawQuadTests();
            SquareTest();
        }

        // test flattening the entire map
        private void FlattenTest()
        {
            flattenoverlay = MinimapManager.Instance.AddMapOverlay("testflatten_overlay");
            FlattenMap(32, flattenoverlay);
        }

        // Draw one square in the center of each quadrant.
        private void SquareTest()
        {
            squarequadoverlay = MinimapManager.Instance.AddMapOverlay("squarequad_overlay");
            int size = 50;
            Vector3 pos = new Vector3(5000, 0, 5000);
            DrawSquare(squarequadoverlay.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), Color.blue, size);
            DrawSquare(squarequadoverlay.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), meadowHeight, size);

            pos.x = -5000;
            DrawSquare(squarequadoverlay.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), Color.blue, size);
            DrawSquare(squarequadoverlay.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), meadowHeight, size);

            pos.z = -5000;
            DrawSquare(squarequadoverlay.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), Color.blue, size);
            DrawSquare(squarequadoverlay.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), meadowHeight, size);

            pos.x = 5000;
            DrawSquare(squarequadoverlay.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), Color.blue, size);
            DrawSquare(squarequadoverlay.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), meadowHeight, size);

            squarequadoverlay.MainTex.Apply();
            squarequadoverlay.ForestFilter.Apply();
            squarequadoverlay.FogFilter.Apply();
            squarequadoverlay.HeightFilter.Apply();
        }


        // test the 4 main textures, Main, Height, Forest, Fog, in 4 different quadrants.
        private void DrawQuadTests()
        {
            quadtest0 = MinimapManager.Instance.AddMapOverlay("quad_overlay0");
            DrawQuadrant(quadtest0.MainTex, Color.red, 0);
            quadtest1 = MinimapManager.Instance.AddMapOverlay("quad_overlay1");
            DrawQuadrant(quadtest1.HeightFilter, meadowHeight, 1);
            quadtest2 = MinimapManager.Instance.AddMapOverlay("quad_overlay2");
            DrawQuadrant(quadtest2.ForestFilter, FilterOff, 2);
            DrawQuadrant(quadtest2.ForestFilter, FilterOn, 1);
            quadtest3 = MinimapManager.Instance.AddMapOverlay("quad_overlay3");
            DrawQuadrant(quadtest3.FogFilter, FilterOn, 3);
        }


        private static void FlattenMap(int height, MinimapManager.MapOverlay ovl)
        {
            for (int i = 0; i < ovl.TextureSize; i++)
            {
                for (int j = 0; j < ovl.TextureSize; j++)
                {
                    ovl.HeightFilter.SetPixel(i, j, new Color(height, 0, 0));
                }
            }
            ovl.HeightFilter.Apply();
        }

        private static void DrawSquaresOnMapPins(Color color, MinimapManager.MapOverlay ovl, bool extras = false)
        {
            foreach (var p in Minimap.instance.m_pins)
            {
                DrawSquare(ovl.MainTex, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), color, 10);
                if (extras)
                {
                    DrawSquare(ovl.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), FilterOff, 10);
                    DrawSquare(ovl.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), FilterOff, 10);
                    DrawSquare(ovl.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), meadowHeight, 10);
                }
            }

            ovl.MainTex.Apply();
            ovl.FogFilter.Apply();
            ovl.ForestFilter.Apply();
            ovl.HeightFilter.Apply();
        }

        private static void DrawSquare(Texture2D tex, Vector2 start, Color col, int square_size)
        {
            Jotunn.Logger.LogInfo($"Starting drawsquare at {start}");
            for (float i = start.x; i < start.x + square_size; i++)
            {
                for (float j = start.y; j < start.y + square_size; j++)
                {
                    tex.SetPixel((int)i, (int)j, col);
                }
            }
        }

        private static void DrawQuarterQuadrant(Texture2D tex, Color col)
        {
            for (int i = 0; i < tex.width / 2; i++)
            {
                for (int j = 0; j < tex.height / 2; j++)
                {
                    tex.SetPixel(i, j, col);
                }
            }
            tex.Apply();
        }

        // Quadrants ordered CCW starting top right, 0 indexed.
        private static void DrawQuadrant(Texture2D tex, Color col, int quadrant)
        {
            int istart = 0, iend = 0, jstart = 0, jend = 0;
            if (quadrant == 0)
            {
                istart = tex.width / 2;
                iend = tex.width;
                jstart = tex.width / 2;
                jend = tex.width;
            }
            if (quadrant == 1)
            {
                istart = 0;
                iend = tex.width / 2;
                jstart = tex.width / 2;
                jend = tex.width;
            }
            if (quadrant == 2)
            {
                istart = 0;
                iend = tex.width / 2;
                jstart = 0;
                jend = tex.width / 2;
            }
            if (quadrant == 3)
            {
                istart = tex.width / 2;
                iend = tex.width;
                jstart = 0;
                jend = tex.width / 2;
            }

            for (int i = istart; i < iend; i++)
            {
                for (int j = jstart; j < jend; j++)
                {
                    tex.SetPixel(i, j, col);
                }
            }
            tex.Apply();
        }


        private class CHCommands_squares : ConsoleCommand
        {
            private readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();

            public override string Name => "map.chsq";

            public override string Help => "run the channel helper";

            public CHCommands_squares()
            {
                colors.Add("red", Color.red);
                colors.Add("green", Color.green);
                colors.Add("blue", Color.blue);
            }

            public override void Run(string[] args)
            {
                if (squareoverlay == null)
                {
                    squareoverlay = MinimapManager.Instance.AddMapOverlay("testsquares_overlay");
                }

                if (args.Length == 1 && colors.TryGetValue(args[0], out Color color))
                {
                    DrawSquaresOnMapPins(color, squareoverlay);
                    return;
                }
                squareoverlay.Enabled = !squareoverlay.Enabled; // toggle
            }

            public override List<string> CommandOptionList() => colors.Keys.ToList();
        }

        private class CHCommands_flatsquares : ConsoleCommand
        {
            private readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();

            public override string Name => "map.chfsq";

            public override string Help => "run the channel helper";

            public CHCommands_flatsquares()
            {
                colors.Add("red", Color.red);
                colors.Add("green", Color.green);
                colors.Add("blue", Color.blue);
            }

            public override void Run(string[] args)
            {
                if (flatsquareoverlay == null)
                {
                    flatsquareoverlay = MinimapManager.Instance.AddMapOverlay("testsquareflatten_overlay");
                }

                if (args.Length == 1 && colors.TryGetValue(args[0], out Color color))
                {
                    DrawSquaresOnMapPins(color, flatsquareoverlay);
                    return;
                }
                flatsquareoverlay.Enabled = !flatsquareoverlay.Enabled; // toggle
            }

            public override List<string> CommandOptionList() => colors.Keys.ToList();
        }


        private class CHCommands_flatten : ConsoleCommand
        {
            public override string Name => "map.chf";

            public override string Help => "Change map height information";

            public override void Run(string[] args)
            {
                if (flattenoverlay == null)
                {
                    flattenoverlay = MinimapManager.Instance.AddMapOverlay("testflatten_overlay");
                    FlattenMap(33, flattenoverlay);
                    return;
                }

                if (args.Length == 1 && int.TryParse(args[0], out int height))
                {
                    FlattenMap(height, flattenoverlay);
                    return;
                }
                flattenoverlay.Enabled = !flattenoverlay.Enabled; // toggle
            }
        }


        private class CHCommands_alpha : ConsoleCommand
        {
            public override string Name => "map.cha";

            public override string Help => "Change map alpha information";

            Color semiblue = new Color(0, 0, 255, 100);

            public override void Run(string[] args)
            {

                if (alphaoverlay == null)
                {
                    alphaoverlay = MinimapManager.Instance.AddMapOverlay("alpha_overlay");
                    DrawQuarterQuadrant(alphaoverlay.MainTex, semiblue);
                    return;
                }

                alphaoverlay.Enabled = !alphaoverlay.Enabled; // toggle
            }
        }

        private class CHCommands_toggle : ConsoleCommand
        {
            public override string Name => "map.toggle";

            public override string Help => "Toggle a specified map layer";

            public override void Run(string[] args)
            {
                string name = args[0];
                var ovl = MinimapManager.Instance.GetMapOverlay(name);
                if (ovl != null)
                {
                    ovl.Enabled = !ovl.Enabled;
                    Console.instance.Print($"Setting overlay {ovl.Name} to {ovl.Enabled}");
                }
            }

            public override List<string> CommandOptionList() => MinimapManager.Instance.GetOverlayNames();
        }
    }
}
