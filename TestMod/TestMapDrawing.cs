﻿// JotunnLib
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

namespace TestMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.Patch)]
    internal class TestMapDrawing : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.testmapdrawing";
        private const string ModName = "Jotunn Test Map Overlays";
        private const string ModVersion = "0.1.0";
        
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
            // Initial overlays / drawings go here
        }

        /// <summary>
        ///     Map overlay showing the zone boundaries
        /// </summary>
        internal static void CreateZoneOverlay(Color color)
        {
            // Get or create a map overlay instance by name
            var zoneOverlay = MinimapManager.Instance.GetMapOverlay("ZoneOverlay");

            // Create a Color array with space for every pixel of the map
            int mapSize = zoneOverlay.TextureSize * zoneOverlay.TextureSize;
            Color[] mainPixels = new Color[mapSize];
            
            // Iterate and set a color for every pixel you want to draw
            int zoneSize = 64;
            int index = 0;
            for (int x = 0; x < zoneOverlay.TextureSize; ++x)
            {
                for (int y = 0; y < zoneOverlay.TextureSize; ++y, ++index)
                {
                    if (x % zoneSize == 0 || y % zoneSize == 0)
                    {
                        mainPixels[index] = color;
                    }
                }
            }

            // Set the pixel array on the overlay texture
            // This is much faster than setting every pixel individually
            zoneOverlay.OverlayTex.SetPixels(mainPixels);

            // Apply the changes on the overlay texture
            // Only applied changes will actually be drawn on the map
            zoneOverlay.OverlayTex.Apply();
        }

        /// <summary>
        ///     Zone drawing
        /// </summary>
        /// <param name="color"></param>
        /// <param name="height"></param>
        internal static void CreateZoneDrawing(Color color, float height)
        {
            var zones = MinimapManager.Instance.GetMapDrawing("ZoneDrawing");

            int mapSize = zones.TextureSize * zones.TextureSize;
            int zoneSize = 64;
            Color filterOff = new Color(0f, 0f, 0f, 1f);
            Color heightFilter = new Color(height, 0f, 0f, 1f);
            Color[] mainPixels = new Color[mapSize];
            Color[] filterPixels = new Color[mapSize];
            Color[] heightPixels = new Color[mapSize];
            int index = 0;
            for (int x = 0; x < zones.TextureSize; ++x)
            {
                for (int y = 0; y < zones.TextureSize; ++y)
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
            zones.MainTex.SetPixels(mainPixels);
            zones.ForestFilter.SetPixels(filterPixels);
            zones.HeightFilter.SetPixels(heightPixels);
            zones.MainTex.Apply();
            zones.ForestFilter.Apply();
            zones.HeightFilter.Apply();
        }

        /// <summary>
        ///     Set height information
        /// </summary>
        /// <param name="height"></param>
        /// <param name="ovl"></param>
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

        /// <summary>
        ///     Draw one square in the center of each quadrant.
        /// </summary>
        private static void DrawSquares()
        {
            var squares = MinimapManager.Instance.GetMapDrawing("SquaresOverlay");
            int size = 50;
            Vector3 pos = new Vector3(5000, 0, 5000);
            DrawSquare(squares.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), Color.blue, size);
            DrawSquare(squares.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(squares.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(squares.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.MeadowHeight, size);

            pos.x = -5000;
            DrawSquare(squares.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), Color.blue, size);
            DrawSquare(squares.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(squares.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(squares.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.MeadowHeight, size);

            pos.z = -5000;
            DrawSquare(squares.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), Color.blue, size);
            DrawSquare(squares.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(squares.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(squares.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.MeadowHeight, size);

            pos.x = 5000;
            DrawSquare(squares.MainTex, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), Color.blue, size);
            DrawSquare(squares.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(squares.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.FilterOff, size);
            DrawSquare(squares.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(pos, squares.TextureSize), MinimapManager.MeadowHeight, size);

            squares.MainTex.Apply();
            squares.ForestFilter.Apply();
            squares.FogFilter.Apply();
            squares.HeightFilter.Apply();

            squares.Enabled = true;
        }

        /// <summary>
        ///     Draw a square starting at every map pin
        /// </summary>
        /// <param name="color"></param>
        /// <param name="ovl"></param>
        /// <param name="extras"></param>
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

        /// <summary>
        ///     Draw single square
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="start"></param>
        /// <param name="col"></param>
        /// <param name="square_size"></param>
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

        /// <summary>
        ///     Test the 4 main textures, Main, Height, Forest, Fog, in 4 different quadrants.
        /// </summary>
        private static void DrawQuadrants()
        {
            var quadTest0 = MinimapManager.Instance.GetMapDrawing("QuadColorOverlay");
            DrawQuadrant(quadTest0.MainTex, Color.red, 0);
            DrawQuadrant(quadTest0.FogFilter, MinimapManager.FilterOff, 0);
            var quadTest1 = MinimapManager.Instance.GetMapDrawing("QuadHeightOverlay");
            DrawQuadrant(quadTest1.HeightFilter, MinimapManager.MeadowHeight, 1);
            DrawQuadrant(quadTest1.FogFilter, MinimapManager.FilterOff, 1);
            var quadTest2 = MinimapManager.Instance.GetMapDrawing("QuadForestOverlay");
            DrawQuadrant(quadTest2.ForestFilter, MinimapManager.FilterOff, 2);
            DrawQuadrant(quadTest2.FogFilter, MinimapManager.FilterOff, 2);
            DrawQuadrant(quadTest2.ForestFilter, MinimapManager.FilterOn, 1);
            DrawQuadrant(quadTest2.FogFilter, MinimapManager.FilterOff, 1);
            var quadTest3 = MinimapManager.Instance.GetMapDrawing("QuadFogOverlay");
            DrawQuadrant(quadTest3.FogFilter, MinimapManager.FilterOn, 3);
        }

        /// <summary>
        ///     Quadrants ordered CCW starting top right, 0 indexed.
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="col"></param>
        /// <param name="quadrant"></param>
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

                CreateZoneOverlay(color);
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

                CreateZoneDrawing(color, height);
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

                var flatten = MinimapManager.Instance.GetMapDrawing("Flatten");

                if (args.Length == 1 && int.TryParse(args[0], out var height))
                {
                    FlattenMap(height, flatten);
                    flatten.Enabled = true;
                    Console.instance.Print($"Setting overlay {flatten.Name} to {height}");
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

                var pinsDrawing = MinimapManager.Instance.GetMapDrawing("PinsOverlay");

                if (args.Length == 1 && colors.TryGetValue(args[0], out Color color))
                {
                    DrawSquaresOnMapPins(color, pinsDrawing);
                    pinsDrawing.Enabled = true;
                    Console.instance.Print($"Setting overlay {pinsDrawing.Name} to {args[0]}");
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

                var pinsOverlay = MinimapManager.Instance.GetMapOverlay("PinsFlatOverlay");

                if (args.Length == 1 && colors.TryGetValue(args[0], out Color color))
                {

                    foreach (var p in Minimap.instance.m_pins)
                    {
                        DrawSquare(pinsOverlay.OverlayTex, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, pinsOverlay.TextureSize), color, 40);
                    }

                    pinsOverlay.OverlayTex.Apply();
                    pinsOverlay.Enabled = true;
                    Console.instance.Print($"Setting overlay {pinsOverlay.Name} to {args[0]}");
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
            public override string Name => "map.reee";

            public override string Help => "reee on the map";

            private Sprite Ree => AssetUtils.LoadSpriteFromFile("TestMod/Assets/reee.png");

            public override void Run(string[] args)
            {
                if (args.Length != 0)
                {
                    Console.instance.Print($"Usage: {Name}");
                    return;
                }

                var reeOverlay = MinimapManager.Instance.GetMapOverlay("ReeeOverlay", true);

                var pixels = Ree.texture.GetPixels();
                var pos = MinimapManager.Instance.WorldToOverlayCoords(Player.m_localPlayer.transform.position,
                    reeOverlay.TextureSize);
                reeOverlay.OverlayTex.SetPixels((int)pos.x, (int)pos.y, Ree.texture.width, Ree.texture.height, pixels);

                reeOverlay.OverlayTex.Apply();
                reeOverlay.Enabled = true;
                Console.instance.Print($"Setting :reee: at {Player.m_localPlayer.transform.position}");
            }
        }

        private class CHCommands_reemain : ConsoleCommand
        {
            public override string Name => "map.reeemain";

            public override string Help => "reee on the map texture";

            private Sprite Ree => AssetUtils.LoadSpriteFromFile("TestMod/Assets/reee.png");

            public override void Run(string[] args)
            {
                if (args.Length != 0)
                {
                    Console.instance.Print($"Usage: {Name}");
                    return;
                }

                var reeDrawing = MinimapManager.Instance.GetMapDrawing("ReeeDrawing");

                var pixels = Ree.texture.GetPixels();
                var filterpixels = new Color[pixels.Length].Populate(MinimapManager.FilterOff);
                var pos = MinimapManager.Instance.WorldToOverlayCoords(Player.m_localPlayer.transform.position,
                    reeDrawing.TextureSize);

                reeDrawing.MainTex.SetPixels((int)pos.x, (int)pos.y, Ree.texture.width, Ree.texture.height, pixels);
                reeDrawing.ForestFilter.SetPixels((int)pos.x, (int)pos.y, Ree.texture.width, Ree.texture.height, filterpixels);
                reeDrawing.FogFilter.SetPixels((int)pos.x, (int)pos.y, Ree.texture.width, Ree.texture.height, filterpixels);

                reeDrawing.MainTex.Apply();
                reeDrawing.ForestFilter.Apply();
                reeDrawing.FogFilter.Apply();

                reeDrawing.Enabled = true;
                Console.instance.Print($"Setting :reee: at {Player.m_localPlayer.transform.position}");
            }
        }
    }
}
