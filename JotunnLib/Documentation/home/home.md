# Guidelines

### If you are starting from scratch read the [Step-by-Step Guide](guide.md).

### If you are transitioning from JötunnLib or ValheimLib read the [Transition Documentation](../transition/jotunnlib/overview.md).

### If you already have a mod install the latest release of Jötunn from [nuget.org](https://www.nuget.org/packages/JotunnLib) or [Jötunn Releases](https://github.com/Valheim-Modding/Jotunn/releases).

- Jötunn uses [MMHooks](https://github.com/MonoMod/MonoMod), so make sure you have build the detour dlls and referenced them in your project.
- Learn about automations and the minimal setup instructions in the [Developer's Quickstart](quickstart.md).

# Features
Jötunn is continually improving and we have lots of features planned for the future, however we hope the ones we have implemented thus far prove to be a useful base and provide an idea of the consistency we aim to deliver moving forwards:
- Loading assets into the game, including the process of mocking to avoid including copyrighted content in your plugin.
- Custom items, pieces, recipes, status effects and skills.
- Easy declaration of console commands, localization tokens and custom inputs.
- UI system to create panels with interactable content.
- Network synchronization of assets and configurations.
- Reordering of bones for character wearable assets reposition.
- JSON integration with SimpleJSON.

# Example and Stub

[Example Mod](https://github.com/Valheim-Modding/JotunnModExample): Example mod that implements many, if not all, of the features Jötunn has.

[Mod Stub](https://github.com/Valheim-Modding/JotunnModStub): This is a bare-bones Jötunn plugin which include automation and debugging tools.
