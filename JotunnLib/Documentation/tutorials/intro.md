# Introduction

In this tutorial, we will cover:

- Using the [ModStub](https://github.com/Valheim-Modding/JotunnModStub) template to create the [ExampleMod](https://github.com/Valheim-Modding/JotunnExampleMod)
- Creating assets with [Unity](data/unity.md)
- Importing unity [assets](data/assets.md)
- Using [mocks](data/mocks.md) inside of AssetBundles to reference native assets
- Creating [custom items](data/items.md)
- Creating [custom pieces/tables](data/pieces.md) (buildable items)
- Creating [custom Status Effects](data/status-effects.md)
- Creating [custom skills](data/skills.md)
- Creating [console commands](data/console-commands.md)
- Creating [valheim UI elements](data/gui.md) within your plugin/without custom assets
- Adding [localizations](data/localization.md)
- [Reordering mesh bones](utils/bonereorder.md) for equipable items
- Implementing [mod synchronisation](utils/networkcompatibility.md)
- Implementing [server side synchronised configurations](utils/config.md)
- Mod interoperable [inputs](data/inputs.md)


Everything that you interact with will be through the various [managers](xref:JotunnLib.Managers), most of which will be Singletons that can be accessed through their `Instance` property. There are also various [utilities](xref:JotunnLib.Utils) which can be assist in common tasks.

For information on how to setup a mod with JötunnLib, see [Getting started](getting-started.md).