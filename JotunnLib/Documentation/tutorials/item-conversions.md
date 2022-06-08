# Item Conversions

An _item conversion_ in Valheim is the process of turning one or more items into another item using a specific station.
For example the cooking of raw meat into cooked meat on the cooking station.
Or the creation of ingots from ores in the furnace.
Jötunn provides a common interface for adding custom conversions with vanilla items or your own custom items.
Adding of custom item conversions is done through the [ItemManager](xref:Jotunn.Managers.ItemManager) singleton class and JVLs abstraction [CustomItemConversion](xref:Jotunn.Entities.CustomItemConversion).

All item conversions will always be loaded **after** all items.
However, item conversions will be loaded in the order that you call the `AddItemConversion` function.

## Valheim's builtin conversion system

Valheim has four different types of conversion components, which are used in one or more pieces:

|Component|Piece|
|----|----|
|CookingStation|piece_cookingstation|
|CookingStation|piece_cookingstation_iron|
|CookingStation|piece_oven|
|Fermenter|fermenter|
|Incinerator|incinerator|
|Smelter|smelter|
|Smelter|blastfurnace|
|Smelter|charcoal_kiln|
|Smelter|windmill|
|Smelter|piece_spinningwheel|

Each of these components can do a conversion from one item to another, some with additional "fuel" items.
The Jötunn [CustomItemConversion](xref:Jotunn.Entities.CustomItemConversion) can be used for either of the conversions.
But since Valheim has three completely different base classes for all conversions, we had to make three different config classes for it, which can be used to construct the CustomItemConversion.

## CookingStation Conversion Example

To add a conversion for a piece with the CookingStation component use the [CookingConversionConfig](xref:Jotunn.Configs.CookingConversionConfig).
You can specify the time needed to cook the item with that config.

**Note**: The `FromItem` prefab needs to have an attach point. Adding prefabs without it result in errors in the game.

```cs
// Add an item conversion for the CookingStation. The items must have an "attach" child GameObject to display it on the station.
var cookConfig = new CookingConversionConfig();
cookConfig.FromItem = "CookedMeat";
cookConfig.ToItem = "CookedLoxMeat";
cookConfig.CookTime = 2f;
ItemManager.Instance.AddItemConversion(new CustomItemConversion(cookConfig));
```

## Fermenter Conversion Example

To add a conversion for a piece with the Fermenter component use the [FermenterConversionConfig](xref:Jotunn.Configs.FermenterConversionConfig).
You can specify the amount of items gained from a single conversion.

```cs
// Add an item conversion for the Fermenter. You can specify how much new items the conversion yields.
var fermentConfig = new FermenterConversionConfig();
fermentConfig.ToItem = "CookedLoxMeat";
fermentConfig.FromItem = "Coal";
fermentConfig.ProducedItems = 10;
ItemManager.Instance.AddItemConversion(new CustomItemConversion(fermentConfig));
```

## Incinerator Conversion Example

To add a conversion for a piece with the Incinerator component use the [IncineratorConversionConfig](xref:Jotunn.Configs.IncineratorConversionConfig)
This one takes one or more items with varying amounts as requirements.
These lists are created using the [IncineratorRequirementConfig](xref:Jotunn.Configs.IncineratorRequirementConfig).
You can also specify the resulting item as well as a priority if more than one of all conversion's requirements are met.

```cs
// Add an incinerator conversion. This one is special since the incinerator conversion script 
// takes one or more items to produce any amount of a new item
var incineratorConfig = new IncineratorConversionConfig();
incineratorConfig.Requirements.Add(new IncineratorRequirementConfig("Wood", 1));
incineratorConfig.Requirements.Add(new IncineratorRequirementConfig("Stone", 1));
incineratorConfig.ToItem = "Coins";
incineratorConfig.ProducedItems = 20;
incineratorConfig.RequireOnlyOneIngredient = false;  // true = only one of the requirements is needed to produce the output
incineratorConfig.Priority = 5;                      // Higher priorities get preferred when multiple requirements are met
ItemManager.Instance.AddItemConversion(new CustomItemConversion(incineratorConfig));
```

## Smelter Conversion Example

To add a conversion for a piece with the Smelter component use the [SmelterConversionConfig](xref:Jotunn.Configs.SmelterConversionConfig).
There is more than one piece in vanilla Valheim with this type of conversion.
The default for the "Station" property in the SmelterConversionConfig is the basic "smelter". To add a conversion for it simply define FromItem and ToItem.

```cs
var smelterConfig = new SmelterConversionConfig();
smelterConfig.FromItem = "Stone";
smelterConfig.ToItem = "CookedLoxMeat";
ItemManager.Instance.AddItemConversion(new CustomItemConversion(smelterConfig));
```

You can also override the default "Station" property with another station string to add a smelter conversion for this station.
Please note that in this example a custom item is used.
Take a look at the [custom item guide](items.md) on how to add own items to the game.

```cs
// Load and create a custom item to use in another conversion
var steelPrefab = Steelingot.LoadAsset<GameObject>("Steel");
var ingot = new CustomItem(steelPrefab, fixReference: false);
ItemManager.Instance.AddItem(ingot);

// Create a conversion for the blastfurnace, the custom item is the new outcome
var blastConfig = new SmelterConversionConfig();
blastConfig.Station = "blastfurnace"; // let's specify something other than default here
blastConfig.FromItem = "Iron";
blastConfig.ToItem = "Steel"; // this is our custom prefabs name we have loaded just above

ItemManager.Instance.AddItemConversion(new CustomItemConversion(blastConfig));
```