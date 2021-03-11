# Registering custom items
Creation of custom items is done through the [ObjectManager](xref:JotunnLib.Managers.ObjectManager) singleton class.

## Usage
To create a new item, you must first add a handler for the [ObjectLoad](xref:JotunnLib.Managers.ObjectManager.ObjectLoad) event

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
    // Objects
    ObjectManager.Instance.RegisterItem("Saddle");
}
```

All items will always be loaded **before** all recipes. However, items will be loaded in the order that you call the `RegisterItem` function.

**Note:** You **must** only use names of existing prefabs. This can be prefabs that have already been registered by another mod, or that already exist in the game.