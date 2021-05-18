# SimpleJVL Utility Class
## Introduction
SimpleJVL shortens language use for front end modders who merely want a few of their assets loaded in, or perhaps simply wish to clone items or add recipes for vanilla items. I included 10 public methods with 2 overloads. I am happy to continue this.

## Exposed Methods
### Item Creation
```cs
SimpleJVL(string assetBundleName, Assembly assembly)
```
Checks whether there is an assetbundle already loaded, and if not, loads defined assetbundle into cache.
```cs
AddItem(string prefabName, string name, string description) +1Overload
```
Adds item to DB based on prefab, and registers it on that name
```cs
AddClonedItem(string prefabNew, string prefabOld, string name, string description) +1Overload
```
Broad extender for cloning
### Piece Creation
```cs
AddPiece(string prefabName, string name, string description, GameObject prefab, string pieceTable = "piece_HammerPieceTable", string craftingStation = "", bool isAllowedInDungeons = false, params RequirementConfig[] inputs)
```
Adds piece to registry based on asset prefab, used by AddStationPiece()
```cs
AddStation(string prefabName, string name, string description, GameObject prefab, string pieceTable, string craftingStation, bool isAllowedInDungeons, params RequirementConfig[] inputs)
```
Adds crafting station from assets, should _allow_ cloning
```cs
AddStationPiece(string prefabName, string name, string description)
```
A constructor for reformatting a Piece into a CraftingStation, regardless of whether it was one before. It defaults to an identical CraftingStation component in the Workbench being added to your piece, if the piece does not have one already.
### Recipe Creation
```cs
AddConversion(string station, string fromitem, string toitem)
```
Shorthand extender for doing what it says it does.
```cs
AddRecipe(GameObject prefabNew, string craftingStation, string repairStation, int minStationLevel, int amount, params RequirementConfig[] inputs)
```
Adds recipe based on prefab, meant to be hooked by one of the previous methods
```cs
AddCloneRecipe(GameObject item, int amount, string craftingStation, int minStationLevel, params RequirementConfig[] inputs)
```
Shorthand extender for doing what it says it does, but for AddClonedItem()
```cs
AddPieceRecipe(GameObject pieceName, string pieceTable, string craftingStation, params RequirementConfig[] inputs)
```
Shorthand extender for doing what it says it does, but for AddPiece()
### Mob Creation
```cs
LoadMob(string prefabName, string mobName)
```
Registers any GameObject as a prefab, but does not add it to any spawn tables. Only to be used with prebuilt assets... for now.

### Overloads
There are also 2 overload constructors, for usage in fine tuning your items.
```cs
    AddItem(
            string prefabName, string name, string description,
            string ammoType = "arrow", int armor = 0, Material armorMaterial = null,
            int armorPerLevel = 0, StatusEffect attackStatusEffect = null, int backstabBonus = 2,
            PieceTable buildPieces = null, bool canBeRepaired = true, bool destroyBroken = false,
            bool centerCamera = false,
            int blunt = 0, int chop = 0, int fire = 0, int frost = 0, int lightning = 0,
            int pickaxe = 0, int pierce = 0, int poison = 0, int slash = 0, int spirit = 0,
            int bluntPerLevel = 0, int chopPerLevel = 0, int firePerLevel = 0, int frostPerLevel = 0, int lightningPerLevel = 0,
            int pickaxePerLevel = 0, int piercePerLevel = 0, int poisonPerLevel = 0, int slashPerLevel = 0, int spiritPerLevel = 0,
            float secondaryAtkDmg = 1,
            int deflectionForce = 0, int deflectionForcePerLevel = 0,
            bool dodgeable = true, int holdStaminaDrain = 0, int maxDurability = 1, int maxQuality = 4,
            int maxStackSize = 1, int movementModifier = 0, bool questItem = false,
            GameObject spawnOnHit = null, GameObject spawnOnHitTerrain = null, bool teleportable = true,
            int value = 0, int variants = 0, int weight = 0, bool blockable = true, int blockPower = 0,
            int blockPowerPerLevel = 0, int equipDuration = 2, StatusEffect equipStatusEffect = null,
            StatusEffect consumeStatusEffect = null, float food = 0, int foodBurnTime = 0,
            int foodRegen = 0, int foodStamina = 0, string setName = "", int setSize = 0,
            StatusEffect setStatusEffect = null, int durabilityDrain = 1, int durabilityPerLevel = 50,
            bool useDurability = true, float useDurabilityDrain = 0
            )`
All are optional besides the first three. Target a parameter to avoid using commas., see examples below.
```cs
    AddClonedItem(
            string prefabNew, string prefabOld, string name, string description,
            string ammoType = "arrow", int armor = 0, Material armorMaterial = null,
            int armorPerLevel = 0, StatusEffect attackStatusEffect = null, int backstabBonus = 2,
            PieceTable buildPieces = null, bool canBeRepaired = true, bool destroyBroken = false,
            bool centerCamera = false, int blunt = 0, int chop = 0, int fire = 0, int frost = 0,
            int lightning = 0, int pickaxe = 0, int pierce = 0, int poison = 0, int slash = 0,
            int spirit = 0, int bluntPerLevel = 0, int chopPerLevel = 0, int firePerLevel = 0,
            int frostPerLevel = 0, int lightningPerLevel = 0, int pickaxePerLevel = 0,
            int piercePerLevel = 0, int poisonPerLevel = 0, int slashPerLevel = 0, int spiritPerLevel = 0,
            float secondaryAtkDmg = 1, int deflectionForce = 0, int deflectionForcePerLevel = 0,
            bool dodgeable = true, int holdStaminaDrain = 0, int maxDurability = 1, int maxQuality = 4,
            int maxStackSize = 1, int movementModifier = 0, bool questItem = false,
            GameObject spawnOnHit = null, GameObject spawnOnHitTerrain = null, bool teleportable = true,
            int value = 0, int variants = 0, int weight = 0, bool blockable = true, int blockPower = 0,
            int blockPowerPerLevel = 0, int equipDuration = 2, StatusEffect equipStatusEffect = null,
            StatusEffect consumeStatusEffect = null, float food = 0, int foodBurnTime = 0,
            int foodRegen = 0, int foodStamina = 0, string setName = "", int setSize = 0,
            StatusEffect setStatusEffect = null, int durabilityDrain = 1, int durabilityPerLevel = 50,
            bool useDurability = true, float useDurabilityDrain = 0
            )```
All are optional besides the first four. Target a parameter to avoid using commas., see examples below.
## Using SimpleJVL
# How it works
The methods within this librarry are almost entirely wrappers.
Use them in place of stretches of code when repeating tasks that are needed for assets.
To expose these methods, preclude your call with ```cs
simpleJVL.Method(params)
```

## Item Creation
**Example Item:**
```cs
simpleJVL.AddItem(prefabNew: "ExampleItem.prefab", name: "Example Item", description: "An Example Item");
```
**Example Item with 32 fire damage:**
```cs
simpleJVL.AddItem(prefabNew: "ExampleItem.prefab", name: "Example Item", description: "An Example Item", fire: 32);
```
**Example 2: Bone Bolt**
```cs
public void AddBolts()
{
    try
    {
        SimpleJVL.Setup.instance.AddItem("BoltBone", "Bone Bolt", "A Bolt made of Bone");
        var item = GameObject.Find("BoltBone");
        SimpleJVL.Setup.instance.AddRecipe(item, "piece_workbench", "piece_workbench", 10, 2, new RequirementConfig
        {
            Item = "BoneFragments",
            Amount = 4,
            AmountPerLevel = 0
        });
    }
    catch (Exception ex)
    {
        Jotunn.Logger.LogError($"Error while adding Bone Bolt: " + ex.Message);
    }
    finally
    {
        // continue
    }
}
```
## Creating Conversions
Instead of;
```cs
// Create a conversion for the blastfurnace, the custom item is the new outcome
var blastConversion = new CustomItemConversion(new SmelterConversionConfig
{
    Station = "blastfurnace", // Override the default "smelter" station
    FromItem = "Iron",
    ToItem = "Steel" // This is our custom prefabs name
});
ItemManager.Instance.AddItemConversion(blastConversion);
```
We can simply use;
```cs
// Notice that this method simply does the previous method for you, using parameters within the constructore rather than nesting them.
simpleJVL.AddConversion("blastfurnace", "CookedMeat", "CookedLoxMeat")
```
