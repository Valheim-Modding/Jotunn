# Registering custom recipes
Creation of custom recipes is done through the [ObjectManager](xref:JotunnLib.Managers.ObjectManager) singleton class.

## Usage
To create a new recipe, you must first add a handler for the [ObjectLoad](xref:JotunnLib.Managers.ObjectManager.ObjectLoad) event

```cs
private void Awake()
{
    ObjectManager.Instance.ObjectRegister += initObjects;
}
```

then, create the handler. You can register custom items and recipes from this handler

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