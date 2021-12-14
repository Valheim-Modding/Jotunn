// JotunnLib
// a Valheim mod
// 
// File:    TestMapDrawing.cs
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
    internal class TestMapDrawing : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.testmapdrawing";
        private const string ModName = "Jotunn Test Map Overlays and Draws";
        private const string ModVersion = "0.1.0";
        /*
            There is still a fairly long list of features needed to be complete before this Manager is released. Eg:
                - Proper documentation for methods + a bit of clean up
         */

        private static MinimapManager.MapOverlay ZoneOverlay;
        private static MinimapManager.MapDrawing Zones;
        private static MinimapManager.MapDrawing Flatten;
        private static MinimapManager.MapDrawing Squares;
        private static MinimapManager.MapOverlay PinsOverlay;
        private static MinimapManager.MapDrawing Pins;
        private static MinimapManager.MapDrawing QuadTest0;
        private static MinimapManager.MapDrawing QuadTest1;
        private static MinimapManager.MapDrawing QuadTest2;
        private static MinimapManager.MapDrawing QuadTest3;
        private static MinimapManager.MapOverlay ReeOverlay;
        private static MinimapManager.MapDrawing ReeDrawing;


        // private static Color MeadowHeight = new Color(32, 0, 0, 255);
        // private static Color FilterOn = new Color(1f, 0f, 0f, 255f);
        // private static Color FilterOff = new Color(0f, 0f, 0f, 255f);

        public void Awake()
        {
            CommandManager.Instance.AddConsoleCommand(new CHCommands_zones());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_zonesmain());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_flatten());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_square());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_pins());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_pinsflat());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_quad());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_ree());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_reemain());

            MinimapManager.OnVanillaMapDataLoaded += MinimapManager_OnVanillaMapDataLoaded;
        }

        private void MinimapManager_OnVanillaMapDataLoaded()
        {
            // Initial overlay / drawings go here
        }

        internal static void CreateSimpleZoneOverlay(Color color)
        {
            ZoneOverlay = MinimapManager.Instance.AddMapOverlay(nameof(ZoneOverlay));

            int mapSize = ZoneOverlay.TextureSize * ZoneOverlay.TextureSize;
            int zoneSize = 64;
            Color[] mainPixels = new Color[mapSize];
            int index = 0;
            for (int x = 0; x < ZoneOverlay.TextureSize; ++x)
            {
                for (int y = 0; y < ZoneOverlay.TextureSize; ++y, ++index)
                {
                    if (x % zoneSize == 0 || y % zoneSize == 0)
                    {
                        mainPixels[index] = color;
                    }
                }
            }
            ZoneOverlay.OverlayTex.SetPixels(mainPixels);
            ZoneOverlay.OverlayTex.Apply();
        }

        // Zone overlay
        internal static void CreateZoneOverlay(Color color, float height)
        {
            Zones = MinimapManager.Instance.AddMapDrawing(nameof(Zones));

            int mapSize = Zones.TextureSize * Zones.TextureSize;
            int zoneSize = 64;
            Color filterOff = new Color(0f, 0f, 0f, 1f);
            Color heightFilter = new Color(height, 0f, 0f, 1f);
            Color[] mainPixels = new Color[mapSize];
            Color[] filterPixels = new Color[mapSize];
            Color[] heightPixels = new Color[mapSize];
            int index = 0;
            for (int x = 0; x < Zones.TextureSize; ++x)
            {
                for (int y = 0; y < Zones.TextureSize; ++y)
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
            Zones.MainTex.SetPixels(mainPixels);
            Zones.ForestFilter.SetPixels(filterPixels);
            Zones.HeightFilter.SetPixels(heightPixels);
            Zones.MainTex.Apply();
            Zones.ForestFilter.Apply();
            Zones.HeightFilter.Apply();
        }

        private static void FlattenMap(int height, MinimapManager.MapDrawing ovl)
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

        // Draw one square in the center of each quadrant.
        private static void DrawSquares()
        {
            Squares = MinimapManager.Instance.AddMapDrawing("SquaresOverlay");
            int size = 50;
            Vector3 pos = new Vector3(5000, 0, 5000);
            DrawSquare(Squares.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), Color.blue, size);
            DrawSquare(Squares.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(Squares.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(Squares.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.MeadowHeight, size);

            pos.x = -5000;
            DrawSquare(Squares.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), Color.blue, size);
            DrawSquare(Squares.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(Squares.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(Squares.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.MeadowHeight, size);

            pos.z = -5000;
            DrawSquare(Squares.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), Color.blue, size);
            DrawSquare(Squares.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(Squares.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(Squares.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.MeadowHeight, size);

            pos.x = 5000;
            DrawSquare(Squares.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), Color.blue, size);
            DrawSquare(Squares.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(Squares.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(Squares.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, Squares.TextureSize), MinimapManager.MeadowHeight, size);

            Squares.MainTex.Apply();
            Squares.ForestFilter.Apply();
            Squares.FogFilter.Apply();
            Squares.HeightFilter.Apply();

            Squares.Enabled = true;
        }

        private static void DrawSquaresOnMapPins(Color color, MinimapManager.MapDrawing ovl, bool extras = false)
        {
            foreach (var p in Minimap.instance.m_pins)
            {
                DrawSquare(ovl.MainTex, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), color, 10);
                if (extras)
                {
                    DrawSquare(ovl.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), MinimapManager.FilterOff, 10);
                    DrawSquare(ovl.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), MinimapManager.FilterOff, 10);
                    DrawSquare(ovl.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), MinimapManager.MeadowHeight, 10);
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

        // test the 4 main textures, Main, Height, Forest, Fog, in 4 different quadrants.
        private static void DrawQuadrants()
        {
            QuadTest0 = MinimapManager.Instance.AddMapDrawing("QuadColorOverlay");
            DrawQuadrant(QuadTest0.MainTex, Color.red, 0);
            QuadTest1 = MinimapManager.Instance.AddMapDrawing("QuadHeightOverlay");
            DrawQuadrant(QuadTest1.HeightFilter, MinimapManager.MeadowHeight, 1);
            QuadTest2 = MinimapManager.Instance.AddMapDrawing("QuadForestOverlay");
            DrawQuadrant(QuadTest2.ForestFilter, MinimapManager.FilterOff, 2);
            DrawQuadrant(QuadTest2.ForestFilter, MinimapManager.FilterOn, 1);
            QuadTest3 = MinimapManager.Instance.AddMapDrawing("QuadFogOverlay");
            DrawQuadrant(QuadTest3.FogFilter, MinimapManager.FilterOn, 3);
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
            private readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>();

            public override string Name => "map.zones";

            public override string Help => "Draw zone borders as an overlay";

            public CHCommands_zones()
            {
                Colors.Add("red", Color.red);
                Colors.Add("green", Color.green);
                Colors.Add("blue", Color.blue);
                Colors.Add("white", Color.white);
            }

            public override void Run(string[] args)
            {
                Color color = Color.white;
                if (args.Length > 0 && !Colors.TryGetValue(args[0], out color))
                {
                    Console.instance.Print($"Color {args[0]} not recognized");
                    return;
                }

                CreateSimpleZoneOverlay(color);
            }

            public override List<string> CommandOptionList() => Colors.Keys.ToList();
        }

        private class CHCommands_zonesmain : ConsoleCommand
        {
            private readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>();

            public override string Name => "map.zonesmain";

            public override string Help => "Draw zone borders on main tex";

            public CHCommands_zonesmain()
            {
                Colors.Add("red", Color.red);
                Colors.Add("green", Color.green);
                Colors.Add("blue", Color.blue);
                Colors.Add("white", Color.white);
            }

            public override void Run(string[] args)
            {
                Color color = Color.white;
                if (args.Length > 0 && !Colors.TryGetValue(args[0], out color))
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

            public override List<string> CommandOptionList() => Colors.Keys.ToList();
        }

        private class CHCommands_flatten : ConsoleCommand
        {
            public override string Name => "map.flatten";

            public override string Help => "Change map height information";

            public override void Run(string[] args)
            {
                if (args.Length == 0)
                {
                    Console.instance.Print($"Usage: {Name} [height]");
                    return;
                }

                Flatten = MinimapManager.Instance.AddMapDrawing("testflatten_overlay");

                if (args.Length == 1 && int.TryParse(args[0], out var height))
                {
                    FlattenMap(height, Flatten);
                    Flatten.Enabled = true;
                    Console.instance.Print($"Setting overlay {Flatten.Name} to {height}");
                }
            }
        }

        private class CHCommands_square : ConsoleCommand
        {
            public override string Name => "map.square";

            public override string Help => "Draw 4 squares on the map.";

            public override void Run(string[] args)
            {
                if (args.Length != 0)
                {
                    Console.instance.Print($"Usage: {Name}");
                    return;
                }

                DrawSquares();
            }
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
                if (args.Length == 0)
                {
                    Console.instance.Print($"Usage: {Name} [color]");
                    return;
                }

                Pins = MinimapManager.Instance.AddMapDrawing("PinsOverlay");

                if (args.Length == 1 && colors.TryGetValue(args[0], out Color color))
                {
                    DrawSquaresOnMapPins(color, Pins);
                    Pins.Enabled = true;
                    Console.instance.Print($"Setting overlay {Pins.Name} to {args[0]}");
                }
            }

            public override List<string> CommandOptionList() => colors.Keys.ToList();
        }

        private class CHCommands_pinsflat : ConsoleCommand
        {
            private readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();

            public override string Name => "map.pinsflat";

            public override string Help => "Draw squares on map pins";

            public CHCommands_pinsflat()
            {
                colors.Add("red", Color.red);
                colors.Add("green", Color.green);
                colors.Add("blue", Color.blue);
            }

            public override void Run(string[] args)
            {
                if (args.Length == 0)
                {
                    Console.instance.Print($"Usage: {Name} [color]");
                    return;
                }

                PinsOverlay = MinimapManager.Instance.AddMapOverlay("PinsFlatOverlay");

                if (args.Length == 1 && colors.TryGetValue(args[0], out Color color))
                {

                    foreach (var p in Minimap.instance.m_pins)
                    {
                        DrawSquare(PinsOverlay.OverlayTex, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ZoneOverlay.TextureSize), color, 40);
                    }

                    PinsOverlay.OverlayTex.Apply();
                    PinsOverlay.Enabled = true;
                    Console.instance.Print($"Setting overlay {PinsOverlay.Name} to {args[0]}");
                }
            }

            public override List<string> CommandOptionList() => colors.Keys.ToList();
        }

        private class CHCommands_quad : ConsoleCommand
        {
            public override string Name => "map.quad";

            public override string Help => "Draw to the 4 textures (Main, Height, Forest, Fog) in 4 different quadrants.";

            public override void Run(string[] args)
            {
                if (args.Length != 0)
                {
                    Console.instance.Print($"Usage: {Name}");
                    return;
                }

                DrawQuadrants();
            }
        }

        private class CHCommands_ree : ConsoleCommand
        {
            public override string Name => "map.ree";

            public override string Help => "Draw ree on the map";

            private Sprite Ree => AssetUtils.LoadSpriteFromFile("TestMod/Assets/reee.png");

            public override void Run(string[] args)
            {
                if (args.Length != 0)
                {
                    Console.instance.Print($"Usage: {Name}");
                    return;
                }

                ReeOverlay = MinimapManager.Instance.AddMapOverlay("ReeeOverlay");

                var pixels = Ree.texture.GetPixels();
                var pos = MinimapManager.Instance.WorldToOverlayCoords(Player.m_localPlayer.transform.position,
                    ReeOverlay.TextureSize);
                ReeOverlay.OverlayTex.SetPixels((int)pos.x, (int)pos.y, Ree.texture.width, Ree.texture.height, pixels);

                ReeOverlay.OverlayTex.Apply();
                ReeOverlay.Enabled = true;
                Console.instance.Print($"Setting :reee: at {Player.m_localPlayer.transform.position}");
            }
        }

        private class CHCommands_reemain : ConsoleCommand
        {
            public override string Name => "map.reemain";

            public override string Help => "Draw ree on the map";

            private Sprite Ree => AssetUtils.LoadSpriteFromFile("TestMod/Assets/reee.png");

            public override void Run(string[] args)
            {
                if (args.Length != 0)
                {
                    Console.instance.Print($"Usage: {Name}");
                    return;
                }

                ReeDrawing = MinimapManager.Instance.AddMapDrawing("ReeeMainOverlay");

                var pixels = Ree.texture.GetPixels();
                var filterpixels = new Color[pixels.Length].Populate(MinimapManager.FilterOff);
                var pos = MinimapManager.Instance.WorldToOverlayCoords(Player.m_localPlayer.transform.position,
                    ReeDrawing.TextureSize);

                ReeDrawing.MainTex.SetPixels((int)pos.x, (int)pos.y, Ree.texture.width, Ree.texture.height, pixels);
                ReeDrawing.ForestFilter.SetPixels((int)pos.x, (int)pos.y, Ree.texture.width, Ree.texture.height, filterpixels);
                ReeDrawing.FogFilter.SetPixels((int)pos.x, (int)pos.y, Ree.texture.width, Ree.texture.height, filterpixels);

                ReeDrawing.MainTex.Apply();
                ReeDrawing.ForestFilter.Apply();
                ReeDrawing.FogFilter.Apply();

                ReeDrawing.Enabled = true;
                Console.instance.Print($"Setting :reee: at {Player.m_localPlayer.transform.position}");
            }
        }
    }
}
