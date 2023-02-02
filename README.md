# Jötunn, the Valheim Library
![Banner](https://raw.github.com/Valheim-Modding/Jotunn/prod/JotunnLib/Documentation/images/banner.png)

Jötunn (/ˈjɔːtʊn/, "giant"), the Valheim Library was created with the intent to facilitate developer creativity, unify the communities problem solving efforts, and enhance developer productivity by curating a library of common helper utilities. Additionally, it supplies specific interfaces and abstractions which aid with mod interoperability, networked gameplay consistency, and remove the need to maintain valheim version specific code by acting as an interface between the developer and the games changing internals.

This project was originally derived from the base structure of [JötunnLib](https://github.com/jotunnlib/jotunnlib), and had many entity abstractions and features from [ValheimLib](https://github.com/Valheim-Modding/ValheimLib) merged into it before we proceeded with further implementations. We have lots of features planned for the future, and we hope the community has many feature requests to suggest. I hope the features we have implemented thus far prove to be a useful base and provide an idea of the consistency we aim to deliver moving forwards.

#### Usage
Please refer to our [documentation](https://valheim-modding.github.io/Jotunn/). We have gone to great lengths to ensure there is ample documentation to facilitate the developer's learning experience.

#### Installation
_If you're using a mod installer, you can likely ignore this section._  
For a more in-depth installation guide, please check out the [manual installation guide](https://valheim-modding.github.io/Jotunn/guides/installation.html) in our documentation.  
However, here is a quick run-down:

1. **Install BepInEx:**\
Download [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/), extract everything inside `BepInEx_Valheim` into your Valheim folder (typically `C:\<PathToYourSteamLibary>\steamapps\common\Valheim`).

2. **Install Jötunn:**\
Download from either [Nexus](https://www.nexusmods.com/valheim/mods/1138) / [Thunderstore](https://valheim.thunderstore.io/package/ValheimModding/Jotunn), extract the ZIP and place all content into `BepInEx/plugins/Jotunn` of your Valheim install.

That's it, launch the game and mod away!

## Features
JVL provides three distinct groups of features. Entities, which abstract the game's own entities into easy-to-use interfaces. Managers, which act as interfaces between the entities and native collections or subsystems. Utilities, which are there to aid in generic/common functions that can span many different areas.

#### Entities
- **CustomCreature** - Represents custom animals, enemies and NPCs.
- **CustomItem** - Represents ingame items such as weapons, tools and consumables.
- **CustomItemConversion** - Represents ingame item conversions for the CookingStation, Fermenter, Smelter and Incinerator in one abstraction.
- **CustomLocalization** - Represents custom localizations for your mod.
- **CustomLocation** - Represents custom locations from simple stone circles to complete villages.
- **CustomPiece** - Represent ingame building pieces.
- **CustomPieceTable** - Represent ingame building tables. Support for custom categories included.
- **CustomRecipe** - Represents ingame recipes for managing crafting and upgrading of items.
- **CustomStatusEffect** - Represents ingame status effects from weapon hit effects to guardian powers.
- **CustomVegetation** - Represents vegetation spread throughout biomes from pickables to cosmetics.
- **KitbashObject** - Represents a custom object assembled from various other prefabs' components.
- **Mocks** - Fake any vanilla prefab and use it in your custom assets - Jötunn resolves the references to the vanilla objects at runtime.
- **Config classes** - There are many more abstractions beside the main entities which allow for easy creation of things like key bindings, custom commands, skills and more.

#### Managers
- **Command Manager** - Facilitates implementation of methods which can be registered as executable console commands.
- **CreatureManager** - Add new creatures or copy and modify vanilla ones.
- **GUI Manager** - Allows invocation of UI prefabs on the fly via code.
- **Input Manager** - Provides an interface for binding keys via ZInput in a consistent manner, facilitating custom keybind hints.
- **Item Manager** - Abstracts away implementation details of configurations applied to items/recipes to provide a consistent developer experience in adding new items. tl;dr items are easy!
- **Kitbash Manager** - Create custom assets with individual pieces from vanilla prefabs.
- **KeyHint Manager** - Create custom key hints for your weapons and tools, even down to the selected piece.
- **Localization Manager** - Provides multiple methods of loading localization data into the game, as well as exposing an interface for adding additional languages to provide localizations to unsupported languages.
- **Minimap Manager** - Alter map data or create overlays for the map.
- **Piece Manager** - Very similar to the Item Manager, abstracting implementation details of configurations for pieces/recipe's.
- **Prefab Manager** - Provides a cache of prefabs registered through other managers, mostly developers will only query the cache for prefabs added via other managers.
- **Render Manager** - Provides a custom render queue to render visual GameObjects into a Sprite - Useful to generate icons for your custom items.
- **Skill Manager** - Facilitates additional custom skills.
- **Undo Manager** - Provides global undo/redo queues for mods to revert and replay any actions in the game.
- **Zone Manager** - Create custom locations and vegetation to add in the world generation.

#### Utilities
- **Asset Helpers** - Methods to facilitate referencing and loading of assets.
- **Bone Reorderer** - Fixes bone ordering issues on `SkinnedMeshRenderer`'s that have been ripped and imported into unity.
- **Network Compatibility** - Allows plugins to define their own version requirements for clients connected to the server. Ensures a customisable level of interoperability with clients of differing mod configurations on a plugin-by-plugin basis.
- **Config Synchronisation** - Allows administrators to adjust configuration values via an in game menu. Config setting is synced to connected clients.
- **Mod Registry** - Query added content per Mod.
- **SimpleJSON** - We have imported SimpleJSON into our library at the request of developers who would simply prefer to have this dependency taken care of already. We use the MIT Licensed [SimpleJSON](https://simplejson.readthedocs.io/en/latest/)

## Bugs, Support, Contributions
Please refer to our [documentation](https://valheim-modding.github.io/Jotunn/) before requesting [support via discord](https://discord.gg/DdUt6g7gyA). If there are any mod interoperability issues developers experience (not just exclusive JVL issues), we would like to hear from you! If we can facilitate better mod interoperability by providing a common interface, or exposing native valheim objects, including a utility which you have created, then please feel free to create a new [feature request](https://github.com/Valheim-Modding/Jotunn/issues/new?assignees=&labels=&template=feature_request.md&title=%5BFEATURE%5D) or [pull request](https://github.com/Valheim-Modding/Jotunn/pulls).

## Roadmap
Check our [projects](https://github.com/Valheim-Modding/Jotunn/projects) for a more up to date list of features currently in development, or suggest your own features for inclusion by creating a new [feature request](https://github.com/Valheim-Modding/Jotunn/issues/new?assignees=&labels=&template=feature_request.md&title=%5BFEATURE%5D)

## Changelog

See the full [Changelog](https://github.com/Valheim-Modding/Jotunn/blob/prod/CHANGELOG.md).

## Contributors to Jötunn, the Valheim Library

These people have been integral to pushing JVL out of the door, and without them we could not have achieved nearly as much. Please give them some love on github, thunderstore, and nexus.

#### Core:

*Jules#7950*: [github](https://github.com/sirskunkalot)

*Margmas#9562*: [github](https://github.com/MSchmoecker), [thunderstore](https://valheim.thunderstore.io/package/MSchmoecker/), [nexus](https://www.nexusmods.com/users/111418768)

*iDeathHD#7866*: [github](https://github.com/xiaoxiao921), [thunderstore](https://valheim.thunderstore.io/package/xiaoxiao921/)

*Algorithman#6741*: [github](https://github.com/Algorithman)

*Quaesar#5604*: [github](https://github.com/RatikKapoor)

*radu#0571*: [github](https://github.com/raduschirliu), [thunderstore](https://valheim.thunderstore.io/package/radu/), [nexus](https://www.nexusmods.com/users/112072898)

*paddy#1337*: [github](https://github.com/paddywaan), [thunderstore](https://valheim.thunderstore.io/package/paddywan/), [nexus](https://valheim.thunderstore.io/package/ValheimModding/)

#### Contributors:

*Cinnabun#0451*: [github](https://github.com/capnbubs)

*GoldenJude#8965*: [github](https://github.com/GoldenJude), [nexus](https://www.nexusmods.com/users/48864143?tab=user+files)

*zarboz#7828*: [github](https://github.com/sbtoonz), [thunderstore](https://valheim.thunderstore.io/package/sbtoonz/), [nexus](https://www.nexusmods.com/users/4057483)

*MarcoPogo#6095*: [github](https://github.com/MathiasDecrock), [nexus](https://www.nexusmods.com/users/3030830?tab=user+files)

*blaxxun#9098*: [github](https://github.com/blaxxun-boop)

*Tekla#1012*: [github](https://github.com/T3kla/ValMods/wiki)

*JoeyParrish#8644*: [github](https://github.com/joeyparrish), [thunderstore](https://valheim.thunderstore.io/package/joeyparrish/), [nexus](https://www.nexusmods.com/users/128211453)

*Nosirrom#2626*: [github](https://github.com/donchad)

*Jere#0989*: [github](https://github.com/JereKuusela), [thunderstore](https://valheim.thunderstore.io/package/JereKuusela/), [nexus](https://www.nexusmods.com/valheim/users/117845818)