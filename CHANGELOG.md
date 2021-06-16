# Changelog

## Version 2.1.0
* Added new entity CustomPieceTable
* Added custom piece table categories per table via PieceTableConfig
* Added custom piece table categories per piece via PieceConfig
* Added piece table reference for custom tools via ItemConfig
* Added support for tokenized piece names and descriptions via PieceConfig
* Added item variations via ItemConfig
* Added possibility to directly link a BepInEx ConfigEntry to a ButtonConfig
* Added colored and invisible config entries
* Added KitbashManager to allow Kitbashing (https://valheim-modding.github.io/Jotunn/tutorials/kitbash.html)
* Added option to CustomButton to ignore custom inputs when a GUI is open (e.g. chat, console)
* Added event in GUIManager when the PixelFix got recreated and custom GUI can be added
* Added static function to the GUIManager to detect a headless/dedicated server before ZNet is instantiated
* Don't create hooks on certain managers when running on a dedicated server (GUI, Input)

## Version 2.0.12
* Disabled SaveManager for now to counter lags
* Added removal of erroneous entities from managers
* Added better handling of empty strings in configs
* Removed output of managers when no custom entities got added
* Added global changelog on github (https://github.com/Valheim-Modding/Jotunn/blob/dev/CHANGELOG.md)

## Version 2.0.11
* Added support for Color type configurations in the mod settings GUI
* Added visual indicator for server side configurations in the mod settings GUI
* Added an event for mods to subscribe to when configuration got synced from the server
* Fixed KeyHints not disappearing when an item is unequipped
* Added KeyHints per Piece
* Added support for registering KeyHints "on the fly"
* Included xmldoc and debug files in releases
* Removed Jötunn version string in main menu

## Version 2.0.10
* Fixed mod compatibility window not scrollable
* Fixed "invalid command" output when using other mod's commands
* Fixed rare NRE when resolving mock refs
* Added new network compatibility mode so vanilla clients can connect if no loaded mod  *enforces*compatibility
* Changed the modified "help" output
* Restructured and extended the docs at https://valheim-modding.github.io/Jotunn/

## Version 2.0.9
* Compatible with Valheim 0.153.2
* Fixed ModCompat window showing when a wrong password was entered

## Version 2.0.8
* Mocks which can not resolve throw Exceptions now (mod devs should check if their mods *have these issues  when updating)
* Items, Pieces, Recipes etc don't get added to the game any more when Mocks are not *resolved preventing  follow-up errors
* More sanity checks for custom entities (Recipe names for example) - also preventing *follow-up errors
* ModCompatibility does not wrap the PeerInfo ZPackage any more but registers own RPC *calls
* Automatic lib refs from Jötunn now check if the Unity libs reside in *"unstripped_managed" and import  these into the project
* Custom console commands are case insensitive now
* PieceManager collects and exposes a list of all PieceTables for mods to query
* Fixed KeyHint NRE (hopefully)

## Version 2.0.7
* Fixed a rare compat issue with ZRpc calls
* Added defaults to RequirementConfig

## Version 2.0.6
* Fixed ModCompatibility mode "NoNeedForSync"
* Added "User installation" to the documentation with instructions for manual installation* of Jötunn
* Resolved an issue with Valheim's localization implementation for all mods relying on LoadLanguage/SetupLanguage hooks

## Version 2.0.5
* Fixed compatibility with RRR Monsters or any other mods, who insert into the wrong *ObjectDB on Startup
* Fixed NRE with mods who manage to have no BepInEx PluginInfo attached
* Fixed default station value for FermenterConversionConfig

## Version 2.0.4
* Fixed incompatibility with mods using ZRpc (WoV SSC for example)
* Introduce new event ItemManager.OnVanillaItemsAvailable for mods to clone vanilla assets for mod use

## Version 2.0.3
* Fixed a NRE when depending mods are null referenced

## Version 2.0.1
* Initial Release
