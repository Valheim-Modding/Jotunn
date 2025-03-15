## Jötunn Events

Jötunn provides different events to execute the right code at the right time.

### Subscribe to Events
The provided events are direct C# events, meaning no heavy patching is needed.
If you have to choose between an event and a patch, it is recommended to go for the event if possible.
Sometimes this is not the case if you need to be compatible with certain mods.
Also note that the event execution order of subscribed methods is not guaranteed by C#.

Depending on your use case and the event, you may have to unsubscribe to only execute your code once.
For this, created a named method, subscribe in your Awake and unsubscribe inside the method again.

```cs
void Awake() {
    PrefabManager.OnVanillaPrefabsAvailable += PrefabsAvailable;
}

void PrefabsAvailable() {
    // do stuff

    // unsubscribe to only execute once
    PrefabManager.OnVanillaPrefabsAvailable -= PrefabsAvailable;
}
```

### Event Flow

The following diagram shows the order of events and when they are executed in the game:

```plantuml
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
    participant SynchronizationManager
end box

group For each mod
    ?->JotunnMod **: Loaded by\nBepInEx
    JotunnMod -> JotunnMod ++ #lightgreen: Awake
end group

== Main Menu Scene ==

Valheim -> Valheim++: ClutterSystem.Awake
    hnote over ZoneManager: OnVanillaClutterAvailable
    hnote over ZoneManager: OnClutterRegistered
deactivate Valheim

Valheim -> Valheim++: FejdStartup.SetupGui
    hnote over GUIManager: OnCustomGUIAvailable
    hnote over LocalizationManager: OnLocalizationAdded
deactivate Valheim

Valheim -> Valheim++: ObjectDB.CopyOtherDB
    hnote over CreatureManager: OnVanillaCreaturesAvailable
    hnote over PrefabManager: OnVanillaPrefabsAvailable
    hnote over ItemManager: OnItemsRegisteredFejd
deactivate Valheim

note over Valheim #lightblue: Main menu interactable

== Loading Scene ==
== Game Scene  ==

Valheim -> Valheim++: ZNetScene.Awake
    hnote over CreatureManager: OnCreaturesRegistered
    hnote over PrefabManager: OnPrefabsRegistered
deactivate Valheim

Valheim -> Valheim++: ObjectDB.Awake
    hnote over ItemManager: OnItemsRegistered
    hnote over PieceManager: OnPiecesRegistered
deactivate Valheim

Valheim -> Valheim++: ClutterSystem.Awake
    hnote over ZoneManager: OnVanillaClutterAvailable
    hnote over ZoneManager: OnClutterRegistered
deactivate Valheim

Valheim -> Valheim++: Game.Start
    hnote over GUIManager: OnCustomGUIAvailable
deactivate Valheim

Valheim -> Valheim++: ZoneSystem.SetupLocations
    hnote over ZoneManager: OnVanillaLocationsAvailable
    hnote over ZoneManager: OnLocationsRegistered
    hnote over ZoneManager: OnVanillaVegetationAvailable
    hnote over ZoneManager: OnVegetationRegistered
deactivate Valheim

Valheim -> Valheim++: Minimap.Start
    hnote over MinimapManager: OnVanillaMapAvailable
deactivate Valheim

group client (only if connecting to a server)
Valheim -> Valheim++: ZRoutedRpc.HandleRoutedRPC
    hnote over SynchronizationManager: OnAdminStatusChanged
    hnote over SynchronizationManager: OnSyncingConfiguration
    hnote over SynchronizationManager: OnConfigurationSynchronized
deactivate Valheim
end group

Valheim -> Valheim++: Minimap.LoadMapData
    hnote over MinimapManager: OnVanillaMapDataLoaded
deactivate Valheim

note over Valheim #lightblue: Game interactable
```
