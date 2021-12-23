# Changelog

## Version 2.4.6
* Fixed some skill issues

## Version 2.4.5
* Added MinimapManager, enabling mods to draw on the map or create overlays for it (see https://valheim-modding.github.io/Jotunn/tutorials/map.html for tutorials) (thx Nosirrom)
* Fixed NRE on mod compat window
* ModStub can have a different deploy path than the Valheim directory (deploying to a r2modman profile for example)

## Version 2.4.4
* Fixed in-game menu not reacting on Esc after closing mod settings
* Fixed a NRE condition on the mod settings

## Version 2.4.3
* Fixed admin config display for non-admin users

## Version 2.4.2
* Reworked the mod settings menu completely using Unity, it should be much faster now and also removed some oddities from using a vanilla Settings clone before
* Added support for Vector2 and generic enums in mod settings
* Removed necessity for key binds backed by a config to also have a button registered in ZInput to show up in the mod settings
* Added some RectTransform extensions for world positioning and overlapping
* Changed the timing of GUIManager.OnCustomGUIAvailable to execute *after* custom Localization has been loaded to ensure GUI content is properly localized
* Fixed NRE while resolving mock references for null enumerables
* Fixed the naming of location containers in ZoneManager
* ItemConfigs without requirements don't create a Recipe for that item
* Updated the asset creation guide to reflect Valheim's recent updates (https://valheim-modding.github.io/Jotunn/tutorials/asset-creation.html)
* Reworked the Asset Mocking tutorial completely (https://valheim-modding.github.io/Jotunn/tutorials/asset-mocking.html)

## Version 2.4.1
* Fixed compatibility of RenderManager by not destroying Components any more (RRR for example)

## Version 2.4.0
* Added ZoneManager for custom Locations and Vegetation (see https://valheim-modding.github.io/Jotunn/tutorials/zones.html for tutorials) (thx MarcoPogo)
* Added NetworkManager for custom RPCs with a simpler interface, automatic package compression/slicing and Coroutines (see https://valheim-modding.github.io/Jotunn/tutorials/rpcs.html for tutorials)
* Added support for mocked objects in Enumerables mixed with non-mocked objects
* Fixed mock resolution in HashSets
* Added language tokens and translations to the docs (https://valheim-modding.github.io/Jotunn/data/localization/overview.html) (thx joeyparrish)

## Version 2.3.12
* Added RenderManager.Render() which renders the given GameObject in the same frame instead of waiting for the next frame. Marked EnqueueRender obsolete.
* Force unload mod's loaded asset bundles on Game.OnApplicationQuit to prevent the Unity engine from crashing
* Depend on BepInExPack-5.4.1601 for the new Unity engine corlibs

## Version 2.3.10
* Added custom piece table category injection at runtime
* Added RenderRequest for the RenderManager to define options for the render process
* Fixed mock resolving of generic List types
* Fixed CustomGUI anchor settings
* Fixed Dropdown list sizing (thx joeyparrish)

## Version 2.3.9
* Added the possibility to define vanilla console command modifiers in ConsoleCommand (thx joeyparrish)
* Added the possibility to define command options in ConsoleCommand
* Fixed exception for mods loaded without PluginInfo.Location set
* Fixed NRE on missing KeyHint objects

## Version 2.3.8
* Added RenderManager to render Sprites from GameObjects at runtime (thx MSchmoecker)
* Added GamepadButton to the InputManager and ButtonConfig - custom inputs can now define a gamepad button corresponding to the keyboard input
* Added gamepad buttons to KeyHints for custom inputs as well as vanilla key overrides
* Gamepad buttons can be defined in the mod settings if they are bound to a config
* Refactored custom KeyHints into their own KeyHintManager and obsoleted the API in the GUIManager
* Fixed automatic mod recognition from filesystem paths (thx Digitalroot)
* Fixed duplication check on ObjectDB not always working correctly
* Fixed NRE in ModCompat for VersionCheckOnly mods
* Fixed a strange hard crash when using GUIManager.IsHeadless()

## Version 2.3.7
* Fixed translation of the custom skill raise message
* Added ZInput.GetButton support for custom buttons

## Version 2.3.6
* Added JSON helper methods to PieceConfig
* Added support for BepInEx' KeyboardShortcuts in ButtonConfig and InputManager
* Added slider for numerical values in the mod settings GUI (thx MSchmoecker)
* Item property in ItemConfig is publicly readable now so it can be serialized
* Fixed button config in mod settings for control keys
* Fixed the localization for Jötunn tokens

## Version 2.3.5
* Added support for custom Obliterator/Incinerator item conversions

## Version 2.3.4
* Fixed BoneReorder for equip with disabled attach points (thx GoldenJude)
* Fixed DragWindowCntrl not respecting the screen size sometimes (thx MSchmoecker)
* Fixed ModCompat NRE with missing "NotEnforced" mods
* Fixed double values not saving in mod settings
* Added support for KeyboardShortcuts in mod settings

## Version 2.3.3
* Reworked the mod compatibility checks
* Obsoleted and replaced some misleading named compat level
* Added two new mod compat level: ServerMustHaveMod and ClientMustHaveMod
* __Please check your mod's compat level and change appropriately__
* See https://valheim-modding.github.io/Jotunn/tutorials/networkcompatibility.html for a list of the new compat levels

## Version 2.3.2
* Adapted the custom piece categories to the new width
* Inject custom commands into the new Terminal system

## Version 2.3.1
* Added support for double values in the mod settings
* Collected new H&H dumps at https://valheim-modding.github.io/Jotunn/data/intro.html

## Version 2.3.0
* Basic H&H compatibility. Looks like everything works but problems might still arise
* __Please report any problems you encounter, preferably on our Discord__
* Added possibility to traverse child GameObjects when resolving mocks (FixReference = true)
* Mock references added via Config are resolved automatically, set FixReference = false if your actual GO does not use mocks
* __Check your mod's items and pieces if they really need FixReference set to true__
* Fundamentally refactored the Localization system - mods can and should add a CustomLocalization wrapper for all mod specific localization from now on
* Localization is stored per mod, so you can create / add only one custom localization instance, which behaves like the LocalizationManager in older releases
* Marked the old API obsolete, __existing mods are encouraged to adapt the new system__
* See https://valheim-modding.github.io/Jotunn/tutorials/localization.html for more information about the new localization system
* Added ApplyTextStyle to GUIManager (thx MSchmoecker)
* Enabled item style variants for items which do not support variants in vanilla (e.g. swords or armor)

## Version 2.2.9
* Fixed compat errors

## Version 2.2.8
* Added global mod registry, collecting added entities per mod
* Added entity CustomPrefab including the possibility to let Jötunn fix mock references
* Show color on the ColorPicker button in the mod settings
* Refactored manually built controls in GUIManager to use Unity's DefaultControls (thx redseiko)
* GUIManager.ApplyButtonStyle does not add a Text GO any more
* Added GUIManager.CreateInputField
* Fixed sprite atlas loading
* Fixed OnLocalizationAdded event timing
* Deprecated ItemManager.OnVanillaItemsAvailable, mods should use PrefabManager.OnVanillaObjectsAvailable now

## Version 2.2.7
* Added loading of dll embedded text assets (thx MSchmoecker)
* Added "ApplyStyle" methods for Scrollbars (thx MSchmoecker)
* Added material and shader dumps to the docs (https://valheim-modding.github.io/Jotunn/data/prefabs/overview.html)
* Fixed the ScrollView pivot in GUIManager.CreateScrollView
* Added removal of failed items/pieces from ZNetScene

## Version 2.2.6
* Fixed category translations with special chars in the category
* Hardened Mock creation a bit

## Version 2.2.5
* LocalizationManager.TryTranslate searches custom localization first with english fallback
* Custom piece categories and the "Mod Settings" menu entry can be translated, for token names see https://valheim-modding.github.io/Jotunn/tutorials/localization.html#localizable-content-in-jötunn
* Fixed a bug where custom piece descriptions were not imported (thx Dominowood371)
* Fixed config "Order" attribute interpretation in the "Mod Settings" (thx Digitalroot)

## Version 2.2.4
* ModCompatibility now disconnects clients from vanilla servers when the client runs enforceable mods
* Added a brand new Mod Settings GUI, accessible from the menu list
* Don't show server side configs in the GUI when a client has no admin rights
* Added two new custom GUI containers for mods to use (in front of and behind Valheim's own GUI)
* Marked the old PixelFix container as obsolete
* Added a new event after custom Localization got added
* Fixed a NRE with dynamic KeyHints
* Fixed mod compatibility window showing on version errors not related to Jötunn
* Track requests to BlockInput and release the block only after all requests are released
* Added new flag "ActiveInCustomGUI" to ButtonConfig to receive button presses while GUIManager.BlockInput is active

## Version 2.2.3
* Don't use own canvas for custom GUI (fixes compat with VHVR for example)
* Compile against BepInEx v5.4.15

## Version 2.2.2
* Fixed NREs for mods without proper BepInEx-Info
* Fixed some NREs with custom GUI components (Auga for example). Jötunn does not brake anything any more but some features won't work with a custom GUI.

## Version 2.2.1
* Fixed a bug which rendered clients unable to login to dedicated servers

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
