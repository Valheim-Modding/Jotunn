# Custom Conversions
Custom conversions can be registered through the `AddCustomConversions(string station, string fromitem, string toitem)`, where `station = "Name of the station prefab this conversion is added to. Defaults to 'smelter'"`, `FromItem = The name of the item you need to put into the station`, `ToItem = The name of the item that FromItem turns into`

# Creating Items
Custom Items can be registered through the `AddItem(string prefabName, string name, string description)`, where `prefabName = "Name of the prefab this is adding`, `name = The name of the item you are creating`, `description = The description shown in the tooltip`
Cloned Items can be registered through the `AddClonedItem(string prefabNew, string prefabOld, string name, string description, int armor, int armorPerLevel, int maxDurability, int durabilityPerLevel, int movementModifier, StatusEffect setStatusEffect, StatusEffect equipStatusEffect, bool canBeRepaired, bool destroyBroken, string setName, string geartype, int setSize`
Custom Pieces can be registered through the `AddPiece(string prefabName, string name, string description, GameObject prefab, string pieceTable, string craftingStation, params RequirementConfig[] inputs`
Custom Stations can be registered through the `AddStation(string prefabName, string name, string description, GameObject prefab, string pieceTable, string craftingStation, bool allowedInDungeon, params RequirementConfig[] inputs)`

# Creating Recipes
There are a few ways to create recipes depending on what we want to include, the following are valid in different situations. Use whatever feels best.
1. `AddRecipe(GameObject prefabNew, string craftingStation, string repairStation, int minStationLevel, int amount, params RequirementConfig[] inputs)`
2. `AddCloneRecipe(GameObject item, int amount, string craftingStation, int minStationLevel, params RequirementConfig[] inputs)`
3. `AddPieceRecipe(GameObject pieceName, string pieceTable, string craftingStation, params RequirementConfig[] inputs`
The next are used for specifying condition sensitive recipes. Can be used if you attempted but failed implementing one of the above methods.
4. `AddOneSlotRecipe(GameObject prefabNew, bool fixRefs, string craftingStation, string repairStation, int minStationLevel, int amount, string needs1, int needsAmount1, int needsAmountPerLevel1)`
5. `AddTwoSlotRecipe(GameObject prefabNew, bool fixRefs, string craftingStation, string repairStation, int minStationLevel, int amount, string needs1, int needsAmount1, int needsAmountPerLevel1, string needs2, int needsAmount2, int needsAmountPerLevel2)`
6. `AddThreeSlotRecipe(GameObject prefabNew, bool fixRefs, string craftingStation, string repairStation, int minStationLevel, int amount, string needs1, int needsAmount1, int needsAmountPerLevel1, string needs2, int needsAmount2, int needsAmountPerLevel2, string needs3, int needsAmount3, int needsAmountPerLevel3)`
7. `AddFourSlotRecipe(GameObject prefabNew, bool fixRefs, string craftingStation, string repairStation, int minStationLevel, int amount, string needs1, int needsAmount1, int needsAmountPerLevel1, string needs2, int needsAmount2, int needsAmountPerLevel2, string needs3, int needsAmount3, int needsAmountPerLevel3, string needs4, int needsAmount4, int needsAmountPerLevel4)`