# Changelog

## Version 2.2.0
* Custom items get loaded into the ObjectDB _before_ any HarmonyX hooks run (fixes compatibility with BetterTrader for example)
* Added non-server-blocking, fragemented and compressed config sync to the clients (thx to [blaxxun](https://github.com/blaxxun-boop))
* Server configs get displayed in the Settings but don't overwrite local configs when connected to a server
* Player with admin status on a server can change server config values directly in the Settings without touching the local configuration
* Compatible with reloading config changes from the filesystem at runtime, changes get propagated to the server or clients if applicable
* AdminOnly configs can be changed in the main menu for local games
* Added event to subscribe to when a players admin status changes on a server, gets also synced to the client
* Added ColorPicker in the "ModConfig" Settings tab to pick color values easily
* Added ColorPicker and GradientPicker to the GUIManager for mods to use
* Added more GUIManager stuff (apply styles for GUI elements, mod usable GUI dragging component)
* Moved most of Jötunns console output to Debug level

## Version 2.1.3
* Fixed KeyCode configs without backing ButtonConfig
* Fixed KeyCode settings display
* Fixed constant NRE when another mod throws at sceneLoaded
* Added tokens of items and pieces to the table dumps
* Added method to query registered custom piece category names

## Version 2.1.2
* Fixed first category selection on custom tables
* Fixed compatibility with mods hooking prev/next category with HarmonyX (e.g. BuildExpansion)

## Version 2.1.1
* Fixed some errors with custom piece table categories
* Custom KeyHints for "missing keys" fallback to the provided button name

## Version 2.1.0
* Added new entity CustomPieceTable and corresponding PieceTableConfig
* Added custom piece categories per table via PieceTableConfig
* Added custom piece categories per piece via PieceConfig
* Added piece table reference for custom tools via ItemConfig
* Added support for tokenized piece names and descriptions via PieceConfig
* Added item variations via ItemConfig
* Added possibility to directly link a BepInEx ConfigEntry to a ButtonConfig
* Added colored and invisible config entries
* Added KitbashManager to allow Kitbashing
* Added option to CustomButton to ignore custom inputs when a GUI is open (e.g. chat, console)
* Added event in GUIManager when the PixelFix got recreated and custom GUI can be added
* Added static method to the GUIManager to block all input except GUI
* Added static method to the GUIManager to detect a headless/dedicated server before ZNet is instantiated
* Removed game hooks on certain managers when running on a dedicated server (GUI, Input)
* Added registering of prefabs "on the fly" to the game in ItemManager and PieceManager
* SynchronizationManager.PlayerIsAdmin gets synced with the server status when changed
* Removed icon enforcement for CustomItems without a Recipe (e.g. monster drops)
* Localization falls back to english when no translation is found in the users language
* Fixed a bug in ItemConfig where the RepairStation was added as the CraftingStation
* Plenty of new and revised documentation at https://valheim-modding.github.io/Jotunn

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
