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
using On.Steamworks;
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
        
        private static MinimapManager.MapOverlay squareoverlay;
        private static MinimapManager.MapOverlay flattenoverlay;

        public void Awake()
        {
            CommandManager.Instance.AddConsoleCommand(new CHCommands_squares());
            CommandManager.Instance.AddConsoleCommand(new CHCommands_flatten());

            MinimapManager.OnVanillaMapDataLoaded += MinimapManager_OnVanillaMapDataLoaded;
        }

        private void MinimapManager_OnVanillaMapDataLoaded()
        {
            flattenoverlay = MinimapManager.Instance.AddMapOverlay("testflatten_overlay");
            FlattenMap(32);

            squareoverlay = MinimapManager.Instance.AddMapOverlay("testsquares_overlay");
            DrawSquaresOnMapPins(Color.red);
        }

        private static void FlattenMap(int height)
        {
            for (int i = 0; i < flattenoverlay.TextureSize; i++)
            {
                for (int j = 0; j < flattenoverlay.TextureSize; j++)
                {
                    flattenoverlay.HeightFilter.SetPixel(i, j, new Color(height, 0, 0));
                }
            }

            flattenoverlay.HeightFilter.Apply();
        }
        
        private static void DrawSquaresOnMapPins(Color color)
        {
            foreach (var p in Minimap.instance.m_pins)
            {
                DrawSquare(squareoverlay.MainTex,
                    MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, squareoverlay.TextureSize), color, 10);
            }

            squareoverlay.MainTex.Apply();
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

        private class CHCommands_squares : ConsoleCommand
        {
            private readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();

            public override string Name => "chsq";

            public override string Help => "run the channel helper";

            public CHCommands_squares()
            {
                colors.Add("red", Color.red);
                colors.Add("green", Color.green);
                colors.Add("blue", Color.blue);
            }

            public override void Run(string[] args)
            {
                if (args.Length == 1 && colors.TryGetValue(args[0], out Color color))
                {
                    DrawSquaresOnMapPins(color);
                    return;
                }
                squareoverlay.Enabled = !squareoverlay.Enabled; // toggle
            }
        }

        private class CHCommands_flatten : ConsoleCommand
        {
            public override string Name => "chf";

            public override string Help => "Change map height information";

            public override void Run(string[] args)
            {
                if (args.Length == 1 && int.TryParse(args[0], out int height))
                {
                    FlattenMap(height);
                    return;
                }
                flattenoverlay.Enabled = !flattenoverlay.Enabled; // toggle
            }
        }
    }
}
