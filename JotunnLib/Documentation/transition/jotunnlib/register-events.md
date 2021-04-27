# Register events
In JotunnLib, the majority of custom data added to the game (prefabs, recipes, items, etc.) had to be added during one of the `Register` events.
For example, when adding recipes in JotunnLib, you would need to do something like this:

```cs
// NOTE: This is code for JotunnLib, NOT for the new Jotunn. This WILL NOT WORK in Jotunn.

private void Awake()
{
    ObjectManager.Instance.ObjectRegister += initObjects;
}

private void initObjects(object sender, EventArgs e)
{
    // Add recipes and items in here...
}
```

However, all of these `Register` events, such as `ObjectRegister`, `PrefabRegister`, etc. **have been removed**. You can now add custom data (prefabs, items, recipes, etc.) directly from your mod's `Awake()` method. For example:

```cs
private void Awake()
{
    ItemManager.Instance.AddRecipe(...);
}
```

For a more in-depth look at how to add add items, recipes, or anything else, please check out the [tutorials](../../tutorials/intro.md) section.