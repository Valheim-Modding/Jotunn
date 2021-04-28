# Directions

### If you already have a mod
And just want to use JotunnLib features:
- Install the latest release of Jötunn from [nuget.org](https://www.nuget.org/packages/JotunnLib) or [JotunnLib Releases](https://github.com/Valheim-Modding/Jotunn/releases).
- Jötunn uses [MMHooks](https://github.com/MonoMod/MonoMod), so make sure you have build the detour dlls and referenced them in your project.
- Learn about automations in the sections [PreBuild Automations](guide.md) and [PostBuild Automations](guide.md) of the Step-by-Step Guide.

### If you are transitioning from JötunnLib or ValheimLib
Read the [transition documentation](transition/overview.md).

### If you are starting from scratch
And want to be guided all the way:
- Read the [Step-by-Step Guide](getting-started.md).
- Download the Mod Stub

# Features
- Loading assets into the game, including the process of mocking to avoid including copyrighted content in your plugin.
- Custom items, pieces, recipes, status effects and skills.
- Easy declaration of console commands, localization and custom inputs.
- UI system to create panels with interactable content.
- Network synchronization of assets and configurations.
- Reordering of bones for character wearable assets reposition.
- JSON integration with SimpleJSON.

# Tutorials
Click the [Tutorials](tutorials/overview.md) button on the left to access a list of all the tutorials we offer, or expand it to travel directly to the one you are interested in.

# Downloadables

### [Mod Stub](https://github.com/Valheim-Modding/JotunnModStub): This is a bare-bones JotunnLib plugin which include automation and debugging tools.

### [Example Mod](https://github.com/Valheim-Modding/JotunnModExample): Example mod that implements many, if not all, of the features JotunnLib has.
