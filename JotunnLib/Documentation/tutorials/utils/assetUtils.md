# Asset Utils
Utilities for loading your custom created assets into your mod. When loading assets we differentiate between side loading and embedded resources.

Definition of side loading: Assets placed side by side with plugin in the filesystem.

```
BepInEx\
    plugins\
        MyMod\
            MyMod.dll
            MyTexture.jpg
```

Definition of embedded resource: Assets are packaged inside of the plugin.dll. To include your assets in your plugin.dll import them into the VS project and change their "Build Action" property to this.

![](../../images/data/Assets.EmbeddedResource.png)

### Textures & Sprites

Loading 2D textures at runtime can be achieved through the [AssetUtils.LoadTexture](xref:JotunnLib.Utils.AssetUtils.LoadTexture(System.String,System.Boolean)) method.
we can load a 2D texture dynamically by doing the following anywhere in our codebase. The path is always relative to the plugin.dll folder.

```cs
Texture2D texture = AssetUtils.LoadTexture("MyTexture.jpg");
```

Similarly, we can also load a sprite by using the [AssetUtils.LoadSpriteFromFile](xref:JotunnLib.Utils.AssetUtils.LoadSpriteFromFile(System.String)) method like so:

```cs
var sprite = AssetUtils.LoadSpriteFromFile("MyTexture.jpg");
```
This will invoke the LoadTexture method and then generate and return a `Sprite` using the texture, wrapping to fit the textures size.

### AssetBundles

JVL Facilitates side loaded asset bundles through the [LoadAssetBundle](xref:JotunnLib.Utils.AssetUtils.LoadAssetBundle(System.String)) method:
```cs
AssetUtils.LoadAssetBundle("JotunnModExample/Assets/blueprints");
```

We also provide a utility to load embedded assets:
```cs
AssetUtils.LoadAssetBundleFromResources("eviesbackpacks", Assembly.GetExecutingAssembly());
```
This method requires that we also pass our assembly to the asset loader so that we may reference the embedded resources which we packaged.