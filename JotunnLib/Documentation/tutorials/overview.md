# Tutorials

This section covers the main interfaces we provide with the library, enabling developers to easily interact with items, recipes, pieces, skills, UI, entities, and much more. If you don't already have a development environment setup, start with our [Step-by-Step Guide](../guides/guide.md) before proceeding with the tutorials.

> [!NOTE]
> All custom data that is registered (prefabs, items, recipes) will automatically be saved and loaded by the game on logout/reload, and will persist across game sessions as long as the mods are still installed.  

> [!WARNING]
> If either modded characters or worlds are loaded _**without**_ the mods installed, you may lose your modded items on that character/world, or it may produce undefined behaviour. I wouldn't recommend trying this on a character/world you care deeply about.

Each section will have examples showing how this is done. All of the examples shown are published in the [Example Mod](https://github.com/Valheim-Modding/JotunnModExample), available in our [GitHub repo](https://github.com/Valheim-Modding).

## Sections

### Creating Assets

* [Asset Creation](asset-creation.md): Create new Assets with Unity and prepare them to be imported into Valheim using Jötunn.
* [Asset Loading](asset-loading.md): Load Assets into your plugin using Jötunn.
* [Asset Mocking](asset-mocking.md): Duplicate and modify Assets without the need to include copyrighted content in your plugin.
* [Kitbashing](kitbash.md): Use individual parts of vanilla prefabs to assemble your custom assets.

### Adding Content

* [Console Commands](console-commands.md): Add custom commands to the Console that can execute your methods.
* [Creatures](creatures.md): Add custom enemies, animals and NPCs and provide basic spawn data.
* [GUI](gui.md): Add custom windows and UI elements.
* [Inputs](inputs.md): Register custom inputs and add key hints to your custom items.
* [Items](items.md): Create equipment and resources.
* [Item Conversions](item-conversions.md): Add custom item conversions like the cooking of meat or the smelting of ores.
* [Item Variants](item-variants.md): Create multiple variants of the same items.
* [Localizations](localization.md): Create language tokens that are replaced at runtime by their specified localization.
* [Map Overlays & Drawings](map.md): Add custom overlays to the game's Map/Minimap or draw directly on the Map's different layers.
* [Pieces and Piece Tables](pieces.md): Create building pieces and use custom categories.
* [Recipes](recipes.md): Add requirements for items and tie them to crafting stations.
* [Status Effects](status-effects.md): Add custom Status Effects.
* [Skills](skills.md): Add custom trainable Skills.
* [Zones / Locations / Vegetation / Clutter](zones.md): Add to or change Valheim's world generation using your custom assets.

### Utilities

* [Bone Reordering](bonereorder.md): Reoder and preserve bone order so player attached objects preserve their position.
* [Config and Synchronization](config.md): Ensures the synchronization of plugin config files between server and client as well as admin states on dedicated servers.
* [Mod Registry & Mod Query](mod-registry.md): Access other mod's prefabs without directly referencing them.
* [Network Compatibility](networkcompatibility.md): Make sure that clients are running compatible versions of the plugin and it's assets.
* [Rendering](renderqueue.md): Create rendered Sprites from GameObjects with visual components at runtime.
* [RPCs](rpcs.md): Add Client/Server process communications and transfer data with automatic slicing and compression.
* [JSON](https://github.com/mhallin/SimpleJSON.NET): JVL integrates the MIT licensed SimpleJSON, accessible via its namespace.