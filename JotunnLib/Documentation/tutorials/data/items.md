# Registering custom items
_Items_ in Valheim are anything that the player is able to hold in their inventory.  
Creation of custom items is done through the [ObjectManager](xref:JotunnLib.Managers.ObjectManager) singleton class.

All items will always be loaded **before** all recipes. However, items will be loaded in the order that you call the `RegisterItem` function.

**Note:** You **must** only use names of existing prefabs (either ones you created or default Valheim ones). This can be prefabs that have already been registered by another mod, or that already exist in the game.

## Example
To create a new item, you must first add a handler for the [ObjectRegister](xref:JotunnLib.Managers.ObjectManager.ObjectRegister) event

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
    ObjectManager.Instance.RegisterItem("TestPrefab");
}
```

That's it! Now, we can type `spawn TestPrefab` in game, and we can see our pick-up-able item!

![Our Item in Game](../../images/data/test-item.png "Our Item in Game")