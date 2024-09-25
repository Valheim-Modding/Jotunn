using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn;
using Jotunn.Managers;
using Jotunn.Entities;
using UnityEngine;
using Jotunn.Configs;

namespace TestMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    [HarmonyPatch]
    public class TestModDungeon : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.testmoddungeon";
        private const string ModName = "Jotunn Test Mod Dungeon";
        private const string ModVersion = "0.1.0";
        private string themeName = "DolmenExample";

        internal AssetBundle assetBundle = null;

        private static Harmony harmony;

        private void Awake()
        {
            assetBundle = AssetUtils.LoadAssetBundleFromResources("dolmenexample");

            harmony = new Harmony(ModGUID);
            harmony.PatchAll();

            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            ZoneManager.OnVanillaLocationsAvailable += OnVanillaLocationsAvailable;
            DungeonManager.OnVanillaRoomsAvailable += OnVanillaRoomsAvailable;
        }

        private void OnVanillaPrefabsAvailable()
        {
            //Load any prefabs used within various rooms (doors, torches, custom enemies, etc etc)
            //PrefabManager.Instance.AddPrefab(new CustomPrefab(assetBundle, "SomePrefab", true));
            DungeonManager.Instance.RegisterEnvironment(assetBundle, "_EnvSetup");
        }

        private void OnVanillaLocationsAvailable()
        {
            // Loading a new custom location as a dungeon entrance
            var dolmenLocationPrefab = assetBundle.LoadAsset<GameObject>("DolmenCaveExample");
            DungeonManager.Instance.RegisterDungeonTheme(dolmenLocationPrefab, themeName);
            LocationConfig dolmenLocConfig = new LocationConfig();
            dolmenLocConfig.Biome = Heightmap.Biome.Meadows;
            dolmenLocConfig.Quantity = 80;
            dolmenLocConfig.Priotized = true;
            dolmenLocConfig.MinAltitude = 1f;
            dolmenLocConfig.ClearArea = true;
            ZoneManager.Instance.AddCustomLocation(new CustomLocation(dolmenLocationPrefab, fixReference: true, dolmenLocConfig));
        }


        private void OnVanillaRoomsAvailable()
        {
            // Add the various rooms used by dungeon generator.
            DungeonManager.Instance.AddCustomRoom(new CustomRoom(assetBundle, "DolmenEntrance", true, new RoomConfig(themeName) { Entrance = true }));
            DungeonManager.Instance.AddCustomRoom(new CustomRoom(assetBundle, "DolmenHall", true, new RoomConfig(themeName)));
            DungeonManager.Instance.AddCustomRoom(new CustomRoom(assetBundle, "DolmenAdjuct", true, new RoomConfig(themeName)));
            DungeonManager.Instance.AddCustomRoom(new CustomRoom(assetBundle, "DolmenRoomLarge", true, new RoomConfig(themeName)));
            DungeonManager.Instance.AddCustomRoom(new CustomRoom(assetBundle, "DolmenEndcap", true, new RoomConfig(themeName) { Endcap = true }));
        }
    }
}
