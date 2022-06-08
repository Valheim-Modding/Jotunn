# Creatures

A _Creature_ is a prefab representing an enemy, animal or NPC in Valheim.
Creatures can be used in various spawning classes where certain conditions have to be fulfilled to instantiate those prefabs into the game.
Jötunn provides an API for validating and adding custom creature prefabs to the game via the [CustomCreature](xref:Jotunn.Entities.CustomCreature) class using the [CreatureManager](xref:Jotunn.Managers.CreatureManager).
Basic spawn conditions can be defined by adding [spawn configurations](xref:Jotunn.Configs.SpawnConfig) to those custom creature prefabs as well as drop lists using [drop configs](xref:Jotunn.Configs.DropConfig).

**Note**: The Unity screens and code snippets are taken from our [example mod](https://github.com/Valheim-Modding/JotunnModExample).

## Creating Creature Prefabs

A prefab has to match certain criteria and has to follow a certain layout to be recognized as a creature prefab by Valheim.
Those requirements are absoultely mandatory as part of the game code. A minimal creature prefab layout looks like this:

![creature prefab](../images/data/creaturePrefab.png)

The base prefab consists of the prefab root GameObject and at least two child objects for the eye position and the visual part of the prefab (the armature and model).

### Base Components

The root GameObject of the prefab has several mandatory components attached:

![creature base components](../images/data/creatureComponentsBase.png)

Instead of using the `Character` and `AnimalAI` components you can also use a combination of `Humanoid` and `MonsterAI`.
The first combination is used for "passive mobs" like deers or other animals.
The latter combination is used for mobs which can attack the player like Boars or Skeletons.

### EyePos

The first child GO of the creature prefab is `EyePos` and does not have any mandatory components itself but is a mandatory reference used in the `Character` component:

![creature eye pos child](../images/data/creatureComponentsEyePos.png)

### Visual

The second child GO `Visual` also has some mandatory components.
Those are normally not attached at this level of the prefab structure as the `Visual` part is usually holding the armature and model of a prefab which each consist of further GOs when using more complex creature prefabs.
To keep it simple for this minimal example, the mandatory components as well as the model components are attached to this single child GO as follows:

![creature visual child](../images/data/creatureComponentsVisual.png)

When validating the creature prefab the game looks for the `Animator` and `CharacterAnimEvent` component at any level of the prefab's GO hierarchy.

### Additional Considerations

There are some "gotcha"'s when creating more complex creatures.

* Certain child GameObjects have to be added to the `character` layer for the game's collision and attack calculations (and probably more use cases). Jötunn will set the base GO and the first level of child GOs to this layer automatically for you. There is also the static [CreatureManager.CharacterLayer](xref:Jotunn.Managers.CreatureManager.CharacterLayer) field for mods to assign this layer in code.
* Creatures wanting to use Valheim's `Tameable` or `Procreation` components must have `Humanoid` and `MonsterAI` components attached.

## Adding Custom Creature Prefabs

After preparing the creature prefab using the minimal requirements, we will load and import the prefab as a [CustomCreature](xref:Jotunn.Entities.CustomCreature).
This can be done as early as the mods Awake() call. All loaded creature prefabs added to the [CreatureManager](xref:Jotunn.Managers.CreatureManager) are kept between world loads.
The most basic use case is to only add a prefab using as a custom creature and define if Jötunn should [resolve mock references](asset-mocking.md).
In our example we opted to have more advanced uses to show all of Jötunn's possibilities. This example does:
* Create a new custom item to be used as the drop and consume item (see the [item tutorial](items.md) for more information on how to do that)
* Load an "animal" creature and add it to the game using drop and spawn configs by providing a [CreatureConfig](xref:Jotunn.Configs.CreatureConfig)
* Load an "enemy" creature and add it to the game using consume and spawn configs by providing a [CreatureConfig](xref:Jotunn.Configs.CreatureConfig).
* Add localization for all added entities (see the [localization tutorial](localization.md) for more information on that topic)

```cs
private void Awake()
{
    // Create custom creatures and spawns
    AddCustomCreaturesAndSpawns();
}

// Add custom made creatures using world spawns and drop lists
private void AddCustomCreaturesAndSpawns()
{
    AssetBundle creaturesAssetBundle = AssetUtils.LoadAssetBundleFromResources("creatures");
    try
    {
        // Load LulzCube test texture and sprite
        var lulztex = AssetUtils.LoadTexture("JotunnModExample/Assets/test_tex.jpg");
        var lulzsprite = Sprite.Create(lulztex, new Rect(0f, 0f, lulztex.width, lulztex.height), Vector2.zero);

        // Create an optional drop/consume item for this creature
        CreateDropConsumeItem(lulzsprite, lulztex);

        // Load and create a custom animal creature
        CreateAnimalCreature(creaturesAssetBundle, lulztex);
                
        // Load and create a custom monster creature
        CreateMonsterCreature(creaturesAssetBundle, lulztex);
                
        // Add localization for all stuff added
        Localization.AddTranslation("English", new Dictionary<string, string>
        {
            {"item_lulzanimalparts", "Parts of a Lulz Animal"},
            {"item_lulzanimalparts_desc", "Remains of a LulzAnimal. It still giggles when touched."},
            {"creature_lulzanimal", "Lulz Animal"},
            {"creature_lulzmonster", "Lulz Monster"}
        });
    }
    catch (Exception ex)
    {
        Logger.LogWarning($"Exception caught while adding custom creatures: {ex}");
    }
    finally
    {
        creaturesAssetBundle.Unload(false);
    }
}

private void CreateDropConsumeItem(Sprite lulzsprite, Texture2D lulztex)
{
    // Create a little lulz cube as the drop and consume item for both creatures
    ItemConfig lulzCubeConfig = new ItemConfig();
    lulzCubeConfig.Name = "$item_lulzanimalparts";
    lulzCubeConfig.Description = "$item_lulzanimalparts_desc";
    lulzCubeConfig.Icons = new[] {lulzsprite};

    var lulzItem = new CustomItem("item_lul", true, lulzCubeConfig);
    lulzItem.ItemDrop.m_itemData.m_shared.m_maxStackSize = 20;
    lulzItem.ItemPrefab.AddComponent<Rigidbody>();

    // Set our lulzcube test texture on the first material found
    lulzItem.ItemPrefab.GetComponentInChildren<MeshRenderer>().material.mainTexture = lulztex;

    // Make it smol
    lulzItem.ItemPrefab.GetComponent<ZNetView>().m_syncInitialScale = true;
    lulzItem.ItemPrefab.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);

    // Add to the ItemManager
    ItemManager.Instance.AddItem(lulzItem);
}
        
private void CreateAnimalCreature(AssetBundle creaturesAssetBundle, Texture2D lulztex)
{
    // Load creature prefab from AssetBundle
    var lulzAnimalPrefab = creaturesAssetBundle.LoadAsset<GameObject>("LulzAnimal");

    // Set our lulzcube test texture on the first material found
    lulzAnimalPrefab.GetComponentInChildren<MeshRenderer>().material.mainTexture = lulztex;

    // Create a custom creature using our drop item and spawn configs
    var lulzAnimalConfig = new CreatureConfig();
    lulzAnimalConfig.Name = "$creature_lulzanimal";
    lulzAnimalConfig.Faction = Character.Faction.AnimalsVeg;
    lulzAnimalConfig.AddDropConfig(new DropConfig
    {
        Item = "item_lul",
        Chance = 100f,
        LevelMultiplier = false,
        MinAmount = 1,
        MaxAmount = 3,
        //OnePerPlayer = true
    });
    lulzAnimalConfig.AddSpawnConfig(new SpawnConfig
    {
        Name = "Jotunn_LulzAnimalSpawn1",
        SpawnChance = 100f,
        SpawnInterval = 1f,
        SpawnDistance = 1f,
        MaxSpawned = 10,
        Biome = Heightmap.Biome.Meadows
    });
    lulzAnimalConfig.AddSpawnConfig(new SpawnConfig
    {
        Name = "Jotunn_LulzAnimalSpawn2",
        SpawnChance = 50f,
        SpawnInterval = 2f,
        SpawnDistance = 2f,
        MaxSpawned = 5,
        Biome = ZoneManager.AnyBiomeOf(Heightmap.Biome.BlackForest, Heightmap.Biome.Plains)
    });

    // Add it to the manager
    CreatureManager.Instance.AddCreature(new CustomCreature(lulzAnimalPrefab, false, lulzAnimalConfig));
}

private void CreateMonsterCreature(AssetBundle creaturesAssetBundle, Texture2D lulztex)
{
    // Load creature prefab from AssetBundle
    var lulzMonsterPrefab = creaturesAssetBundle.LoadAsset<GameObject>("LulzMonster");

    // Set our lulzcube test texture on the first material found
    lulzMonsterPrefab.GetComponentInChildren<MeshRenderer>().material.mainTexture = lulztex;

    // Create a custom creature using our consume item and spawn configs
    var lulzMonsterConfig = new CreatureConfig();
    lulzMonsterConfig.Name = "$creature_lulzmonster";
    lulzMonsterConfig.Faction = Character.Faction.ForestMonsters;
    lulzMonsterConfig.UseCumulativeLevelEffects = true;
    lulzMonsterConfig.AddConsumable("item_lul");
    lulzMonsterConfig.AddSpawnConfig(new SpawnConfig
    {
        Name = "Jotunn_LulzMonsterSpawn1",
        SpawnChance = 100f,
        MaxSpawned = 1,
        Biome = Heightmap.Biome.Meadows
    });
    lulzMonsterConfig.AddSpawnConfig(new SpawnConfig
    {
        Name = "Jotunn_LulzMonsterSpawn2",
        SpawnChance = 50f,
        MaxSpawned = 1,
        Biome = ZoneManager.AnyBiomeOf(Heightmap.Biome.BlackForest, Heightmap.Biome.Plains)
    });

    // Add it to the manager
    CreatureManager.Instance.AddCreature(new CustomCreature(lulzMonsterPrefab, true, lulzMonsterConfig));
}
```

## Modifying and Cloning Vanilla Creatures

You can get vanilla creatures and either clone them or modify the original to change any aspect of that creature you want.
To make sure all vanilla creature prefabs are loaded, use the provided event [OnVanillaCreaturesAvailable](xref:Jotunn.Managers.CreatureManager.OnVanillaCreaturesAvailable).

Cloned creatures keep all of their vanilla components except for the spawn configurations.
This means unless you provide new spawn configurations, a cloned creature does not spawn at all.
If you don't provide a new DropConfig for the cloned creature, all vanilla drops are kept. Providing a new DropConfig completely overrides all vanilla drops.

Since creature prefabs are loaded globally and Jötunn keeps all added creatures for the game session after adding them once, you can unsubscribe from the event after its first execution.
Keep in mind that other mods could alter vanilla cratures, too, so it might be required to modify vanilla creatures on every event execution.

```cs
private void Awake()
{
    // Hook creature manager to get access to vanilla creature prefabs
    CreatureManager.OnVanillaCreaturesAvailable += ModifyAndCloneVanillaCreatures;
}

// Modify and clone vanilla creatures
private void ModifyAndCloneVanillaCreatures()
{
    // Clone a vanilla creature with and add new spawn information
    var lulzetonConfig = new CreatureConfig();
    lulzetonConfig.AddSpawnConfig(new SpawnConfig
    {
        Name = "Jotunn_SkelSpawn1",
        SpawnChance = 100,
        SpawnInterval = 20f,
        SpawnDistance = 1f,
        Biome = Heightmap.Biome.Meadows,
        MinLevel = 3
    });

    var lulzeton = new CustomCreature("Lulzeton", "Skeleton_NoArcher", lulzetonConfig);
    var lulzoid = lulzeton.Prefab.GetComponent<Humanoid>();
    lulzoid.m_walkSpeed = 0.1f;
    CreatureManager.Instance.AddCreature(lulzeton);

    // Get a vanilla creature prefab and change some values
    var skeleton = CreatureManager.Instance.GetCreaturePrefab("Skeleton_NoArcher");
    var humanoid = skeleton.GetComponent<Humanoid>();
    humanoid.m_walkSpeed = 2;

    // Unregister the hook, modified and cloned creatures are kept over the whole game session
    CreatureManager.OnVanillaCreaturesAvailable -= ModifyAndCloneVanillaCreatures;
}
```

## Drop Configurations

You can add one or more [Drop Configurations](xref:Jotunn.Configs.DropConfig) to your custom creature.
Drops are items a creature leaves behind on its death. You must provide a prefab name for the drop as a string, Jötunn resolves that prefab at runtime for you.

There are also additional properties to further the drops for the creature:

|Property|Effect|Default|
|---|---|---|
|MinAmount<br>MaxAmount|How many of the drop items should be spawned.|1|
|Chance|The chance of this drop in percent.|100|
|OnePerPlayer|If true, one instance per active player will be dropped regardless of min/max amount.|false|
|LevelMultiplier|Should the drop amount be multiplied by the creature level.|true|

## Spawn Configurations

While adding creatures with Jötunn you can define one or more basic [Spawn Configurations](xref:Jotunn.Configs.SpawnConfig).
Those configurations are added to the world spawn list so your creatures spawn into the world automatically.
You must at least provide an unique name for your spawn configuration and a spawning biome.

There are plenty of properties to refine your spawn configuration:

|Property|Effect|Default|
|---|---|---|
|WorldSpawnEnabled|If set to false, Jötunn creates the SpawnData for this config but disables the actual spawn.|true|
|Biome|Heightmap.Biome for your creature to spawn in. Multiple biomes per spawn config are possible.<br>To get the correct value for a multi-biome spawn, you can use [ZoneManager.AnyBiomeOf](xref:Jotunn.Managers.ZoneManager.AnyBiomeOf(Heightmap.Biome[])).|
|BiomeArea|Uses the Heightmap.BiomeArea enum to define if the spawn should be in the middle, on the edges or everywhere on the spawning biomes.|Heightmap.BiomeArea.Everywhere|
|MaxSpawned|How many instances of this creature can be active at the same time (excluding event instances).|1|
|SpawnInterval|Seconds between new spawn checks.|4f|
|SpawnChance|Spawn chance each spawn interval in percent.|100f|
|SpawnDistance|Minimum distance to another instance.|10f|
|MinSpawnRadius<br>MaxSpawnRadius|Min/max distance from player to spawn at.<br>A specific player is chosen as a target, this setting basically creates a ring around the player, in which a spawn point can be chosen|0 (equals the global default of 40)<br>0 (equals the global default of 80)|
|MinLevel<br>MaxLevel|Min/max level the creature spawns with.<br>Level is assigned by rolling levelup-chance for each level from min, until max is reached.|1<br>1|
|RequiredGlobalKey|Only spawn if this key is set.<br>See [GameConstants.GlobalKey](xref:Jotunn.Utils.GameConstants.GlobalKey) for constant values|
|RequiredEnvironments|Only spawn if one of this environments is active.<br>See [GameConstants.Weather](xref:Jotunn.Utils.GameConstants.Weather) for constant values|
|MinGroupSize<br>MaxGroupSize|Min/Max number of entities to attempt to spawn at a time.|1<br>1|
|GroupRadius|Radius of circle, in which to spawn a pack of entities<br>Eg., when group size is 3, all 3 spawns will happen inside a circle indicated by this radius.|3f|
|SpawnAtDay|Can spawn during day.<br>Note: If not true, creatures with MonsterAI will attempt to despawn at day|true|
|SpawnAtNight|Can spawn during night|true|
|MinAltitude<br>MaxAltitude|The min/max altitude (distance to water surface) for the creature to spawn|-1000f<br>1000f|
|MinTilt<br>MaxTilt|The min/max tilt of terrain required to spawn. Tested multiple times to decide where to spawn entity.|35f|
|MinOceanDepth<br>MaxOceanDepth|The min/max ocean depth for the creature to spawn. Ignored if min == max.|0<br>0|
|SpawnInForest|Spawn can happen in forest areas.|true|
|SpawnOutsideForest|Spawn can happen outside of forest areas.|true|
|HuntPlayer|Set true to let the AI hunt the player on spawn.|false|
|GroundOffset|Offset to the ground the creature spawns on|0.5f|


Jötunn's spawn configuration covers all of the game's default spawning options.
To have much tighter control of your creature spawns, we recommend using the [SpawnThat! mod](https://www.nexusmods.com/valheim/mods/453) by A Sharp Pen.

## Consumable Items

Creatures using the `MonsterAI` component can have `ItemDrop` references attached to define which items this creature can consume when it encounters it in the world.
This list is also used to define the items needed to tame the creature (if the `Tameable` component is also attached).
You can add prefab strings to the [CreatureConfig.Consumables](xref:Jotunn.Configs.CreatureConfig.Consumables) array and Jötunn will try to resolve the `ItemDrop` component for you at runtime.

## Cumulative Level Effects

When using the `LevelEffects` component on a creature prefab, the game only enables the effects for the actual creature level upon spawning a creature with a level higher than 0.
Besides some material and transform alteration, those level effects also include an additional `GameObject` you can set per level to be enabled.
Using Jötunn, you can set [CreatureConfig.UseCumulativeLevelEffects](xref:Jotunn.Configs.CreatureConfig.UseCumulativeLevelEffects) for any added custom creature to `true`.
This will enable _all_ previous levels `GamneObjects` as well as the object of your current level upon spawning the creature.
If you spawn a 3-star creature for example, the objects for level 2 and 1 will also be enabled.
