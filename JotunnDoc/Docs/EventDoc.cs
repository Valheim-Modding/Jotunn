using System;
using HarmonyLib;
using Jotunn.Managers;

namespace JotunnDoc.Docs
{
    public class EventDoc : Doc
    {
        private static event Action ZNetSceneAwake;

        public EventDoc() : base("flow.md")
        {
            ZNetSceneAwake += () => AddText("--- Game Scene ---");

            CreatureManager.OnCreaturesRegistered += () => AddText("CreatureManager.OnCreaturesRegistered");
            CreatureManager.OnVanillaCreaturesAvailable += () => AddText("CreatureManager.OnVanillaCreaturesAvailable");

            GUIManager.OnPixelFixCreated += () => AddText("GUIManager.OnPixelFixCreated");
            GUIManager.OnCustomGUIAvailable += () => AddText("GUIManager.OnCustomGUIAvailable");

            ItemManager.OnItemsRegistered += () => AddText("ItemManager.OnItemsRegistered");
            ItemManager.OnItemsRegisteredFejd += () => AddText("ItemManager.OnItemsRegisteredFejd");
            ItemManager.OnVanillaItemsAvailable += () => AddText("ItemManager.OnVanillaItemsAvailable");

            LocalizationManager.OnLocalizationAdded += () => AddText("LocalizationManager.OnLocalizationAdded");

            MinimapManager.OnVanillaMapAvailable += () => AddText("MinimapManager.OnVanillaMapAvailable");
            MinimapManager.OnVanillaMapDataLoaded += () => AddText("MinimapManager.OnVanillaMapDataLoaded");

            PrefabManager.OnPrefabsRegistered += () => AddText("PrefabManager.OnPrefabsRegistered");
            PrefabManager.OnVanillaPrefabsAvailable += () => AddText("PrefabManager.OnVanillaPrefabsAvailable");

            ZoneManager.OnVanillaClutterAvailable += () => AddText("ZoneManager.OnVanillaClutterAvailable");
            ZoneManager.OnVanillaLocationsAvailable += () => AddText("ZoneManager.OnVanillaZonesAvailable");

            PieceManager.OnPiecesRegistered += () => AddText("PieceManager.OnPiecesRegistered");
        }

        [HarmonyPatch]
        public static class EventDocPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPrefix, HarmonyPriority(5000)]
            public static void FejdStartup_Start_Postfix()
            {
                ZNetSceneAwake?.Invoke();
            }
        }
    }
}
