# Mod Registry & Mod Query
Sometimes it is necessary to get prefabs from other mods.
This is where the Mod Registry and Mod Query provide useful interfaces without having to reference other mods directly.
The Mod Registry only provides options to access other objects added with Jotunn, while the Mod Query is providing prefabs added by other mods.

## Mod Registry
The [ModRegistry](xref:Jotunn.Utils.ModRegistry) is used to get Jotunn entities.
Depending on what type is wanted, the time where all entities are available may differs.
For example, console commands are normally added early at each plugins Awake or Start, while custom pieces may be added at `PrefabManager.OnVanillaPrefabsAvailable` or even later.
Thus, it is important to test different execution points and look at how other mods handle their insertion.

As an example we print all locations added by Jotunn mods:
```cs
Jotunn.Logger.LogInfo($"Jotunn Locations:");
foreach (var customLocation in ModRegistry.GetLocations())
{
   Jotunn.Logger.LogInfo($"  {customLocation.Name} added by {customLocation.SourceMod.Name}");
}
```

Alternatively it is also possible to target one mod directly:
```cs
Jotunn.Logger.LogInfo($"Jotunn OtherMod Locations:");
foreach (var customLocation in ModRegistry.GetLocations("other_mod.guid"))
{
   Jotunn.Logger.LogInfo($"  {customLocation.Name} added by {customLocation.SourceMod.Name}");
}
```

## Mod Query
The [ModQuery](xref:Jotunn.Utils.ModQuery) is a reliable way to get the source mods of modded prefabs.
Only prefabs added to the ZNetScene or ObjectDB are indexed.
Other then the Mod Registry, the Mod Query works for all mods and thus only provides access to the prefab level.
Jotunn entities cannot be looked up directly.

There is a small impact in loading time when using the Mod Query, as each non-Jotunn mod must be patched to find out which prefabs the mod adds.
For a small modpacks, the additional startup time is negligible and for larger modpacks it will be under a few seconds, which is still not much compared to the overall loading time.
The startup time doesn't increase further if more then one mod uses the Mod Query.

Nevertheless, the Mod Query needs to be enabled to not waste time if it isn't used.
This has to be called early in your plugin's Awake or Start.
```cs
ModQuery.Enable();
```

After the Mod Query is enabled, modded prefabs can be looked up. Vanilla prefabs are not indexed here.
```cs
Jotunn.Logger.LogInfo($"Modded prefabs:");
foreach (var moddedPrefab in ModQuery.GetPrefabs())
{
    Jotunn.Logger.LogInfo($"  {moddedPrefab.Prefab.name} added by {moddedPrefab.SourceMod.Name}");
}
```
Because some mods are adding prefabs really late, this call may also have to delayed, sometimes just before the Player is spawned.
