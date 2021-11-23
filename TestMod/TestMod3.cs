// JotunnLib
// a Valheim mod
// 
// File:    TestMod3.cs
// Project: TestMod

using System.Collections.Generic;
using System.Globalization;
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
        private static MinimapManager.MapOverlay pinsoverlay;
        private static MinimapManager.MapOverlay pinsflatoverlay;
        private static MinimapManager.MapOverlay alphaoverlay;
        private static MinimapManager.MapOverlay flattenoverlay;
        private static MinimapManager.MapOverlay quadtest0;
        private static MinimapManager.MapOverlay quadtest1;
        private static MinimapManager.MapOverlay quadtest2;
        private static MinimapManager.MapOverlay quadtest3;

        //private static MinimapManager.MapOverlay ZoneOverlay;
        private static MinimapManager.MapOverlay SimpleZoneOverlay;
        private static MinimapManager.MapOverlay ZoneOverlay;

        private static Color MeadowHeight = new Color(32, 0, 0, 255);
        private static Color FilterOn = new Color(1f, 0f, 0f, 255f);
        private static Color FilterOff = new Color(0f, 0f, 0f, 255f);

        public void Awake()
        {
            CommandManager.Instance.AddConsoleCommand(new CHCommands_zones());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_pins());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_flatten());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_pinsflat());
            // CommandManager.Instance.AddConsoleCommand(new CHCommands_alpha()); // Not yet implemented.

            MinimapManager.OnVanillaMapDataLoaded += MinimapManager_OnVanillaMapDataLoaded;
        }

        private void MinimapManager_OnVanillaMapDataLoaded()
        {


            // test drawing flat squares with no fog and no forest
            //pinsflatoverlay = MinimapManager.Instance.AddMapOverlay("testsquareflatten_overlay");
            //DrawSquaresOnMapPins(Color.blue, pinsflatoverlay, extras: true);

            // test just drawing squares
            //pinsoverlay = MinimapManager.Instance.AddMapOverlay("testsquares_overlay");
            //DrawSquaresOnMapPins(Color.red, pinsoverlay);

            // test transparency
            //alphaoverlay = MinimapManager.Instance.AddMapOverlay("alpha_overlay");
            //DrawQuarterQuadrant(alphaoverlay.MainTex, semiblue);

            //DrawQuadTests();
            //SquareTest();
            CreateZoneOverlay(Color.white, 0.0f);
            //CreateSimpleZoneOverlay(Color.white);
            //DrawSquaresOnMapPins(Color.blue, SimpleZoneOverlay);
        }

        internal static void CreateSimpleZoneOverlay(Color color)
        {
            if (SimpleZoneOverlay == null)
            {
                SimpleZoneOverlay = MinimapManager.Instance.AddMapOverlay(nameof(SimpleZoneOverlay));
            }
            int mapSize = SimpleZoneOverlay.TextureSize * SimpleZoneOverlay.TextureSize;
            int zoneSize = 64;
            Color[] mainPixels = new Color[mapSize];
            int index = 0;
            for (int x = 0; x < SimpleZoneOverlay.TextureSize; ++x)
            {
                for (int y = 0; y < SimpleZoneOverlay.TextureSize; ++y, ++index)
                {
                    if (x % zoneSize == 0 || y % zoneSize == 0)
                    {
                        mainPixels[index] = color;
                    }
                }
            }
            SimpleZoneOverlay.OverlayTex.SetPixels(mainPixels);
            SimpleZoneOverlay.OverlayTex.Apply();
        }
        
        // Zone overlay
        internal static void CreateZoneOverlay(Color color, float height)
        {
            if (ZoneOverlay == null)
            {
                ZoneOverlay = MinimapManager.Instance.AddMapOverlay(nameof(ZoneOverlay));
            }
            int mapSize = ZoneOverlay.TextureSize * ZoneOverlay.TextureSize;
            int zoneSize = 64;
            Color filterOff = new Color(0f, 0f, 0f, 1f);
            Color heightFilter = new Color(height, 0f, 0f, 1f);
            Color[] mainPixels = new Color[mapSize];
            Color[] filterPixels = new Color[mapSize];
            Color[] heightPixels = new Color[mapSize];
            int index = 0;
            for (int x = 0; x < ZoneOverlay.TextureSize; ++x)
            {
                for (int y = 0; y < ZoneOverlay.TextureSize; ++y)
                {
                    if (x % zoneSize == 0 || y % zoneSize == 0)
                    {
                        mainPixels[index] = color;
                        filterPixels[index] = filterOff;
                        heightPixels[index] = heightFilter;
                    }
                    ++index;
                }
            }
            ZoneOverlay.MainTex.SetPixels(mainPixels);
            ZoneOverlay.ForestFilter.SetPixels(filterPixels);
            ZoneOverlay.HeightFilter.SetPixels(heightPixels);
            ZoneOverlay.MainTex.Apply();
            ZoneOverlay.ForestFilter.Apply();
            ZoneOverlay.HeightFilter.Apply();
        }

        // Draw one square in the center of each quadrant.
        private void SquareTest()
        {
            squarequadoverlay = MinimapManager.Instance.AddMapOverlay("QuadCenterOverlay");
            int size = 50;
            Vector3 pos = new Vector3(5000, 0, 5000);
            DrawSquare(squarequadoverlay.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), Color.blue, size);
            DrawSquare(squarequadoverlay.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), MeadowHeight, size);

            pos.x = -5000;
            DrawSquare(squarequadoverlay.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), Color.blue, size);
            DrawSquare(squarequadoverlay.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), MeadowHeight, size);

            pos.z = -5000;
            DrawSquare(squarequadoverlay.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), Color.blue, size);
            DrawSquare(squarequadoverlay.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), MeadowHeight, size);

            pos.x = 5000;
            DrawSquare(squarequadoverlay.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), Color.blue, size);
            DrawSquare(squarequadoverlay.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), FilterOff, size);
            DrawSquare(squarequadoverlay.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squarequadoverlay.TextureSize), MeadowHeight, size);

            squarequadoverlay.MainTex.Apply();
            squarequadoverlay.ForestFilter.Apply();
            squarequadoverlay.FogFilter.Apply();
            squarequadoverlay.HeightFilter.Apply();

            squarequadoverlay.Enabled = true;
        }


        // test the 4 main textures, Main, Height, Forest, Fog, in 4 different quadrants.
        private void DrawQuadTests()
        {
            quadtest0 = MinimapManager.Instance.AddMapOverlay("QuadColorOverlay");
            DrawQuadrant(quadtest0.MainTex, Color.red, 0);
            quadtest1 = MinimapManager.Instance.AddMapOverlay("QuadHeightOverlay");
            DrawQuadrant(quadtest1.HeightFilter, MeadowHeight, 1);
            quadtest2 = MinimapManager.Instance.AddMapOverlay("QuadForestOverlay");
            DrawQuadrant(quadtest2.ForestFilter, FilterOff, 2);
            DrawQuadrant(quadtest2.ForestFilter, FilterOn, 1);
            quadtest3 = MinimapManager.Instance.AddMapOverlay("QuadFogOverlay");
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
                    DrawSquare(ovl.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), MeadowHeight, 10);
                }
            }

            ovl.MainTex.Apply();
            if (extras)
            {
                ovl.FogFilter.Apply();
                ovl.ForestFilter.Apply();
                ovl.HeightFilter.Apply();
            }
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

        private class CHCommands_zones : ConsoleCommand
        {
            private readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();

            public override string Name => "map.zones";

            public override string Help => "Draw squares on map pins";

            public CHCommands_zones()
            {
                colors.Add("red", Color.red);
                colors.Add("green", Color.green);
                colors.Add("blue", Color.blue);
                colors.Add("white", Color.white);
            }

            public override void Run(string[] args)
            {
                Color color = Color.white;
                if (args.Length > 0 && !colors.TryGetValue(args[0], out color))
                {
                    Console.instance.Print($"Color {args[0]} not recognized");
                    return;
                }

                float height = 0f;
                if (args.Length > 1 && !float.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out height))
                {
                    Console.instance.Print($"Height {args[1]} not recognized");
                    return;
                }

                CreateZoneOverlay(color, height);
            }

            public override List<string> CommandOptionList() => colors.Keys.ToList();
        }

        private class CHCommands_pins : ConsoleCommand
        {
            private readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();

            public override string Name => "map.pins";

            public override string Help => "Draw squares on map pins";

            public CHCommands_pins()
            {
                colors.Add("red", Color.red);
                colors.Add("green", Color.green);
                colors.Add("blue", Color.blue);
            }

            public override void Run(string[] args)
            {
                if (pinsoverlay == null)
                {
                    pinsoverlay = MinimapManager.Instance.AddMapOverlay("pinsoverlay");
                    DrawSquaresOnMapPins(Color.green, pinsoverlay);
                    Console.instance.Print($"Created {pinsoverlay.Name}");
                    return;
                }

                if (args.Length == 1 && colors.TryGetValue(args[0], out Color color))
                {
                    DrawSquaresOnMapPins(color, pinsoverlay);
                    pinsoverlay.Enabled = true;
                    Console.instance.Print($"Setting overlay {pinsoverlay.Name} to {args[0]}");
                    return;
                }

                pinsoverlay.Enabled = !pinsoverlay.Enabled; // toggle
                Console.instance.Print($"Overlay {pinsoverlay.Name} {(pinsoverlay.Enabled ? "enabled" : "disabled")}");
            }

            public override List<string> CommandOptionList() => colors.Keys.ToList();
        }

        private class CHCommands_pinsflat : ConsoleCommand
        {
            private readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();

            public override string Name => "map.pinsflat";

            public override string Help => "Draw squares on map pins and disables other layers";

            public CHCommands_pinsflat()
            {
                colors.Add("red", Color.red);
                colors.Add("green", Color.green);
                colors.Add("blue", Color.blue);
            }

            public override void Run(string[] args)
            {
                if (pinsflatoverlay == null)
                {
                    pinsflatoverlay = MinimapManager.Instance.AddMapOverlay("pinsflatoverlay");
                    DrawSquaresOnMapPins(Color.blue, pinsflatoverlay, true);
                    Console.instance.Print($"Created {pinsflatoverlay.Name}");
                    return;
                }

                if (args.Length == 1 && colors.TryGetValue(args[0], out Color color))
                {
                    DrawSquaresOnMapPins(color, pinsflatoverlay, true);
                    pinsflatoverlay.Enabled = true;
                    Console.instance.Print($"Setting overlay {pinsflatoverlay.Name} to {args[0]}");
                    return;
                }

                pinsflatoverlay.Enabled = !pinsflatoverlay.Enabled; // toggle
                Console.instance.Print($"Overlay {pinsflatoverlay.Name} {(pinsflatoverlay.Enabled ? "enabled" : "disabled")}");
            }

            public override List<string> CommandOptionList() => colors.Keys.ToList();
        }


        private class CHCommands_flatten : ConsoleCommand
        {
            public override string Name => "map.flatten";

            public override string Help => "Change map height information";

            public override void Run(string[] args)
            {
                if (flattenoverlay == null)
                {
                    flattenoverlay = MinimapManager.Instance.AddMapOverlay("testflatten_overlay");
                    FlattenMap(33, flattenoverlay);
                    Console.instance.Print($"Created {flattenoverlay.Name}");
                    return;
                }

                if (args.Length == 1 && int.TryParse(args[0], out var height))
                {
                    FlattenMap(height, flattenoverlay);
                    flattenoverlay.Enabled = true;
                    Console.instance.Print($"Setting overlay {flattenoverlay.Name} to {height}");
                    return;
                }

                flattenoverlay.Enabled = !flattenoverlay.Enabled; // toggle
                Console.instance.Print($"Overlay {flattenoverlay.Name} {(flattenoverlay.Enabled ? "enabled" : "disabled")}");
            }
        }


        /*        private class CHCommands_alpha : ConsoleCommand
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
                }*/
    }
}
