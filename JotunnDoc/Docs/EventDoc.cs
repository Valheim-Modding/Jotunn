using System;
using System.Collections.Generic;
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
        private static EventDoc Instance { get; set; }
        private static event Action ZNetSceneAwake;

        private static List<object> events = new List<object>();

        public EventDoc() : base("flow", "puml")
        {
            Instance = this;
            Rewrite();

            ZNetSceneAwake += () =>
            {
                events.Add("note over Valheim #lightblue: Main menu interactable\n\n== Loading Scene ==\n== Game Scene  ==\n");
                Rewrite();
            };

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

            ZoneManager.OnVanillaLocationsAvailable += () => AddEvent(nameof(ZoneManager), nameof(ZoneManager.OnVanillaLocationsAvailable));
            ZoneManager.OnLocationsRegistered += () => AddEvent(nameof(ZoneManager), nameof(ZoneManager.OnLocationsRegistered));
            ZoneManager.OnVanillaVegetationAvailable += () => AddEvent(nameof(ZoneManager), nameof(ZoneManager.OnVanillaVegetationAvailable));
            ZoneManager.OnVegetationRegistered += () => AddEvent(nameof(ZoneManager), nameof(ZoneManager.OnVegetationRegistered));
            ZoneManager.OnVanillaClutterAvailable += () => AddEvent(nameof(ZoneManager), nameof(ZoneManager.OnVanillaClutterAvailable));
            ZoneManager.OnClutterRegistered += () => AddEvent(nameof(ZoneManager), nameof(ZoneManager.OnClutterRegistered));

            PieceManager.OnPiecesRegistered += () => AddEvent("PieceManager", "OnPiecesRegistered");
        }

        private void AddEvent(string manager, string eventname)
        {
            events.Add(new EventInvoke(manager, eventname));
            Rewrite();
        }

        private void Rewrite()
        {
            Clear();
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

            for (var i = 0; i < events.Count; i++)
            {
                if (events[i] is string message)
                {
                    AddText(message);
                }

                if (events[i] is EventInvoke eventInvoke)
                {
                    if (i == 0 || !(events[i - 1] is EventInvoke prevEventInvoke) || $"{prevEventInvoke.type}.{prevEventInvoke.method}" != $"{eventInvoke.type}.{eventInvoke.method}")
                    {
                        Instance.AddText($"Valheim -> Valheim++: {eventInvoke.type}.{eventInvoke.method}");
                    }

                    Instance.AddText($"    hnote over {eventInvoke.manager}: {eventInvoke.name}");

                    if (i == events.Count - 1 || !(events[i + 1] is EventInvoke nextEventInvoke) || $"{nextEventInvoke.type}.{nextEventInvoke.method}" != $"{eventInvoke.type}.{eventInvoke.method}")
                    {
                        Instance.AddText($"deactivate Valheim\n");
                    }
                }
            }

            AddText(@"
note over Valheim #lightblue: Game interactable

@enduml");
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

        private class EventInvoke
        {
            public string type;
            public string method;
            public string manager;
            public string name;

            public EventInvoke(string manager, string name)
            {
                this.manager = manager;
                this.name = name;

                MethodBase valheimMethod = new StackTrace().GetFrames()
                    .First(x =>
                        x.GetMethod().ReflectedType?.Assembly != typeof(Main).Assembly &&
                        x.GetMethod().ReflectedType?.Assembly != typeof(JotunnDoc).Assembly
                    ).GetMethod();

                type = valheimMethod.DeclaringType.Name;
                method = valheimMethod.Name.Replace($"DMD<{type}::", "").Replace(">", "");
            }
        }
    }
}
