# Registering custom recipes
_Recipes_ in Valheim are anything that the player is able to hold in their inventory.  
Creation of custom recipes is done through the [ObjectManager](xref:JotunnLib.Managers.ObjectManager) singleton class.

## Usage
To create a new recipe, you must first add a handler for the [ObjectRegister](xref:JotunnLib.Managers.ObjectManager.ObjectRegister) event

```cs
private void Awake()
{
    ObjectManager.Instance.ObjectRegister += initObjects;
}
```

then, create the handler. You can register custom items and recipes from this handler. To register custom items, you must either pass a [RecipeConfig](xref:JotunnLib.Entities.RecipeConfig) instance to the function, or a Valheim `Recipe` object. The following example demonstrates how to use the [RecipeConfig](xref:JotunnLib.Entities.RecipeConfig), as it is easier and less verbose.

```cs
private void initObjects(object sender, EventArgs e)
{
    // Recipes
    ObjectManager.Instance.RegisterRecipe(new RecipeConfig()
    {
        Item = "Saddle",
        CraftingStation = "forge",
        Requirements = new PieceRequirementConfig[]
        {
            new PieceRequirementConfig()
            {
                Item = "Iron",
                Amount = 4
            },
            new PieceRequirementConfig()
            {
                Item = "DeerHide",
                Amount = 10
            }
        }
    });
}
```

All recipes will always be loaded **after** all items. However, recipes will be loaded in the order that you call the `RegisterRecipe` function.

**Note:** You **must** only use names of existing prefabs. This can be prefabs that have already been registered by another mod, or that already exist in the game.