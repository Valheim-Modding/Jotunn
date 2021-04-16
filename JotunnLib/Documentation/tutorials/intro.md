# Introduction
These tutorials cover the following.

- Using the [ModStub](https://github.com/Valheim-Modding/JotunnModStub) template to create the [ExampleMod](https://github.com/Valheim-Modding/JotunnExampleMod)
- Importing unity [assets](data/assets.md)
- Creating [custom items](data/items.md)
- Creating [custom pieces/tables](data/pieces.md) (buildable items)
- Creating [custom Status Effects](data/status-effects.md)
- Creating [custom skills](data/skills.md)
- Using [mocks](data/mocks.md) inside of AssetBundles to reference native assets
- Adding [localizations](data/localization.md)
- Implementing [mod synchronisation](utils/networkcompatibility.md)


Everything that you interact with will be through the various [managers](xref:JotunnLib.Managers), most of which will be Singletons that can be accessed through their `Instance` property. There are also various [utilities](xref:JotunnLib.Utils) which can be assist in common tasks.

For information on how to setup a mod with JotunnLib, see [Getting started](getting-started.md).