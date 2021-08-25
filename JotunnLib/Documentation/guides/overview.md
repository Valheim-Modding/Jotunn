# Guides

### [Step-by-Step Guide](guide.md) - For most users, contains information for step-by-step instruction.

### [Developer's Quickstart](quickstart.md) - For experienced user integrations, with basic setup instructions.

### [Transition Documentation](../transition/jotunnlib/overview.md) - For users who are transitioning, with guidance.

### [Manual Installation Guide](installation.md) - For users who require manual setup instructions, but cannot use other methods.

# Features
Jötunn is continually improving and we have lots of features planned for the future, however we hope the ones we have implemented thus far prove to be a useful base and provide an idea of the consistency we aim to deliver moving forwards:
- Loading assets into the game, including the process of mocking, to avoid including copyrighted content in generated plugins.
- Custom items, pieces, recipes, status effects and skills, along with a framework for justifying your own asset types in C#.
- Easy declaration of console commands, localization tokens, and custom inputs.
- Reordering of bones for character wearable assets reposition.
- Network synchronization of assets and configurations.
- UI system to create panels with interactable content.
- JSON integration with SimpleJSON.

# Example and Stub

[Example Mod](https://github.com/Valheim-Modding/JotunnModExample): Example mod that implements many, if not all, of the features Jötunn has.

[Mod Stub](https://github.com/Valheim-Modding/JotunnModStub): This is a bare-bones Jötunn plugin which include automation and debugging tools.
