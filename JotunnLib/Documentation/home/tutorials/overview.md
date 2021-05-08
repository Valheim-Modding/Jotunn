# Tutorials

This section covers the main interfaces we provide with the library, enabling developers to easily interact with items, recipes, pieces, skills, UI, entities, and much more. It is suggested to begin with [getting started](../guide.md) before proceeding with the tutorials.

> [!NOTE]
> All custom data that is registered (prefabs, items, recipes) will automatically be saved and loaded by the game on logout/reload, and will persist across game sessions as long as the mods are still installed.  

> [!WARNING]
> If either modded characters or worlds are loaded _**without**_ the mods installed, you may lose your modded items on that character/world, or it may produce undefined behaviour. I wouldn't recommend trying this on a character/world you care deeply about.

Each section will have examples showing how this is done. All of the examples shown are published in the [Example Mod](https://github.com/Valheim-Modding/JotunnModExample), available in our [GitHub repo](https://github.com/Valheim-Modding).

## Quick guide

### [Asset Creation](asset-creation.md): Create new Assets with Unity and prepare them to be imported into Valheim using JotunnLib.

### [Asset Loading](asset-loading.md): Load Assets into your plugin using JotunnLib.

### [Asset Mocking](asset-mocking.md): Duplicate and modify Assets without the need to include copyrighted content in your plugin.

### [Custom Items](items.md): Create equipment and resources.

### [Custom Pieces](pieces.md): Create building pieces.

### [Custom Recipes](recipes.md): Create building pieces.

### [Custom Status Effects](status-effects.md): Add custom Status Effects.

### [Custom Item Conversions](item-conversions.md): Add custom item conversions like the cooking of meat or the smelting of ores.

### [Custom Skills](skills.md): Add custom trainable Skills.

### [Custom Console Commands](console-commands.md): Add custom commands to the Console that can execute your methods.

### [UI Elements](gui.md): Add custom windows and UI elements.

### [Localizations](localization.md): Create language tokens that are replaced at runtime by their specified localization.

### [Reordering Bones](bonereorder.md): Reoder and preserve bone order so player attached objects preserve their position.

### [Network Compatibility](networkcompatibility.md): Make sure that clients are running compatible versions of the plugin and it's assets.

### [Config Sync](config.md): Ensures the synchronization of plugin config files between server and client.

### [Inputs](inputs.md): Register custom inputs and add key hints to your custom items.

### [JSON](https://github.com/mhallin/SimpleJSON.NET): JVL integrates the MIT licensed SimpleJSON, accessible via its namespace.