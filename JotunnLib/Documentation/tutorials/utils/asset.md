# Asset Utils
Util functions related to loading assets at runtime.

## Loading 2D textures
Loading 2D textures at runtime can be done through the [AssetUtils.LoadTexture](xref:JotunnLib.Utils.AssetUtils.LoadTexture(System.String)) static function.

### Example
For the following folder structure
```
BepInEx\
    plugins\
        MyMod.dll
        MyTexture.jpg
```

we can load a 2D texture dynamically by doing the following anywhere in our codebase

```cs
Texture2D texture = AssetUtils.LoadTexture("MyTexture.jpg");
```

this `texture` object can now be used as a sprite for an item, or anything else.

## Loading models
Loading `.obj` models at runtime can be done through the [AssetUtils.LoadMesh](xref:JotunnLib.Utils.AssetUtils.LoadMesh(System.String)) static function.

_Example WIP_