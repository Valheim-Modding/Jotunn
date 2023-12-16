using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Jotunn;
using Jotunn.Managers;

namespace JotunnDoc.Docs
{
    public class EventDoc : Doc
    {
        private static event Action ZNetSceneAwake;

        public EventDoc() : base("flow", "puml")
        {
            AddText(@"
@startuml
'' flow
!pragma teoz true
hide footbox

participant Valheim
participant BepInEx

box JotunnMods
    collections JotunnMod
end box

box Jotunn
    participant LocalizationManager
    participant CreatureManager
    participant PrefabManager
    participant PieceManager
    participant ItemManager
    participant ZoneManager
    participant GUIManager
    participant MinimapManager
end box

group For each mod
    ?->JotunnMod **: Loaded by\nBepInEx
    JotunnMod -> JotunnMod ++ #lightgreen: Awake
end group

== Main Menu Scene ==
".TrimStart());

            ZNetSceneAwake += () => AddText("note over Valheim #lightblue: Main menu interactable\n\n== Loading Scene ==\n== Game Scene  ==\n");

            CreatureManager.OnCreaturesRegistered += () => AddEvent("CreatureManager", "OnCreaturesRegistered");
            CreatureManager.OnVanillaCreaturesAvailable += () => AddEvent("CreatureManager", "OnVanillaCreaturesAvailable");

            GUIManager.OnCustomGUIAvailable += () => AddEvent("GUIManager", "OnCustomGUIAvailable");

            ItemManager.OnItemsRegistered += () => AddEvent("ItemManager", "OnItemsRegistered");
            ItemManager.OnItemsRegisteredFejd += () => AddEvent("ItemManager", "OnItemsRegisteredFejd");

            LocalizationManager.OnLocalizationAdded += () => AddEvent("LocalizationManager", "OnLocalizationAdded");

            MinimapManager.OnVanillaMapAvailable += () => AddEvent("MinimapManager", "OnVanillaMapAvailable");
            MinimapManager.OnVanillaMapDataLoaded += () => AddEvent("MinimapManager", "OnVanillaMapDataLoaded");

            PrefabManager.OnPrefabsRegistered += () => AddEvent("PrefabManager", "OnPrefabsRegistered");
            PrefabManager.OnVanillaPrefabsAvailable += () => AddEvent("PrefabManager", "OnVanillaPrefabsAvailable");

            ZoneManager.OnVanillaClutterAvailable += () => AddEvent("ZoneManager", "OnVanillaClutterAvailable");
            ZoneManager.OnVanillaLocationsAvailable += () => AddEvent("ZoneManager", "OnVanillaZonesAvailable");

            PieceManager.OnPiecesRegistered += () => AddEvent("PieceManager", "OnPiecesRegistered");
        }

        private void AddEvent(string manager, string eventname)
        {
            MethodBase valheimMethod = new StackTrace().GetFrames()
                .First(x =>
                    x.GetMethod().ReflectedType?.Assembly != typeof(Main).Assembly &&
                    x.GetMethod().ReflectedType?.Assembly != typeof(JotunnDoc).Assembly
                ).GetMethod();

            string type = valheimMethod.DeclaringType.Name;
            string method = valheimMethod.Name.Replace($"DMD<{type}::", "").Replace(">", "");

            AddText($"Valheim -> Valheim++: {type}.{method}");
            AddText($"    hnote over {manager}: {eventname}");
            AddText($"deactivate Valheim\n");
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
