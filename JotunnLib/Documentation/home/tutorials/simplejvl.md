## Using SimpleJVL
# How it works
- W I P -

# Custom Conversions
- W I P -

# Changing Conversions
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

# Creating Items
- W I P -

# Cloning Items
- W I P -

# Creating Recipes
- W I P -

# Cloning Recipes
- W I P -

# Creating Pieces
- W I P -

# Cloning Pieces
- W I P -

# Creating Crafting Stations
- W I P -

# Cloning Crafting Stations
- W I P -