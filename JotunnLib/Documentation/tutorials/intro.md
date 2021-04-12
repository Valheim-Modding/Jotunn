# Introduction
This section will contain tutorials on how to use the various parts of the framework to do things such as:
- Using the [ModStub]() template to create the [ExampleMod]()
- Importing unity [assets](data/assets.md))
- Creating [custom items](data/items.md)
- Creating [custom pieces](data/pieces.md) (buildable items)
- Creating [custom Status Effects](data/statuseffects.md)
- Creating [custom skills](data/skills.md)

- Creating custom [piece tables](piecetables.md)
- Using [mocks](data/mocks.md) inside of AssetBundles to reference native assets
- Adding [localisations]()


Everything that you interact with will be through the various [managers](xref:JotunnLib.Managers), most of which will be Singletons that can be accessed through their `Instance` property.

For information on how to setup a mod with JotunnLib, see [Getting started](getting-started.md). To start creating custom items, take a look at [Custom data](data/overview.md).