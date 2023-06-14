# Changelog

## Version 2.12.1
* Fixed compatibility issues with the custom hammer tabs when Auga is installed. There will be some warnings as some visual changes can't be applied but these should not affect the functionality

## Version 2.12.0
* Compatible with Valheim version 0.216.9, not working with an older version
* Added prefab name helpers for CookingStations, CraftingStations, Fermenters, Incinerators, PieceCategories, PieceTables and Smelters
* Added `CustomPiece.Category` helper property to make it easier to set the piece category at runtime.
* Added `PieceManager.Instance.GetPieceCategoriesMap` to get a complete map of all piece categories
* Reworked custom piece categories to be compatible with other mods. This changes some internal category ids
* Changed custom hammer tabs to stack instead of scroll. Tabs also no longer have a dynamic width, instead the text tires to fit the tab
* Changed `PieceManager.GetPieceCategories` to be obsolete, use `PieceManager.Instance.GetPieceCategoriesMap` instead to get a complete map of all piece categories
* Changed `PieceManager.PieceCategorySettings` to be obsolete as they are no longer used
* Changed `PieceManager.RemovePieceCategory` to no longer remove categories where a piece is still assigned to the category. This is to prevent problems with other mods that might still use this category 
* Changed Manager Init() methods to be private and not callable by mods. They were not intended to be called by mods and could cause issues
* Changed Mock resolve depth from 3 to 5, this should catch some edge cases where fields are nested deeper
* Changed empty CustomLocalization constructor to be marked obsolete, LocalizationManager.Instance.GetLocalization() should be used instead
* Fixed empty translation values where not allowed, thus a valid key can be translated to an empty string
* Fixed translations not being added to the Localization instance if it was already initialized
* Slightly improved loading time of big mod packs when creating custom entities (again)

## Version 2.11.7
* Added Valheim network version check to the disconnect window to better identify the cause of a disconnect. Only visible if both server and client are running Jotunn 2.11.7 or higher
* Removed the Valheim version string check from the disconnect window, as the network version is used for the version check

## Version 2.11.6
* Fixed an issue with custom category tabs where the hammer menu wouldn't open when using a controller

## Version 2.11.5
* Fixed custom category tabs caused errors after relogging

## Version 2.11.4
* Added the magic string `(HiddenCategory)` to disabled category tab names, so other mods can detect them easier
* Changed that vanilla tabs are always enabled for vanilla tools and only Jotunn managed and vanilla tabs can be disabled

## Version 2.11.3
* Compatible with Valheim version 0.214.300 and the upcoming 0.215.1 patch
* Fixed a client with a different Valheim version will no longer be additionally disconnected by Jötunn. If the player was disconnected, a mismatching Valheim version will still be displayed
* Fixed side-loaded localisations being loaded too early, causing translations to be added to the internal Jötunn localisation instead of the respective mod
* Fixed custom hammer tabs had the wrong size when being localised
* Fixed scroll of last custom hammer tab was not correct
* Fixed a compatibility issue with Auga because of the changed hammer tabs
* Slightly improved loading time of big mod packs by caching mod info used when creating custom entities

## Version 2.11.2
* Compatible with Valheim version 0.214.300

## Version 2.11.1
* Fixed KeyHints not being correctly destroyed when using inventories with containers
* Fixed ItemManager.RemoveItem was not removing the item from the active ObjectDB if it already existed
* Fixed an issue that caused client config values to not reset correctly when a mod used shared ConfigurationManagerAttributes/ConfigDescription with server enforced values

## Version 2.11.0
* Compatible with Valheim version 0.214.2
* Added Norse fonts and additional colors to the GUIManger (thx SpikeHimself)
* Added interface to remove a custom item conversion
* Added reference to the BepInExPack 5.4.2100
* Added reference to Unity.TextMeshPro in the NuGet package
* Updated ConfigurationManagerAttributes to match BepInEx.ConfigurationManager v17.0
* Updated the localization injection to be easier and removed the need to load vanilla translations a second time. Because the implementation concept changed, debug logs will no longer be printed
* Fixed dropdown created with GUIManager.CreateDropDown was not in the correct UI layer, causing it to be wrongly rendered in VR (thx SpikeHimself)
* Fixed calling LocalizationManager/CustomLocalization.TryTranslate() could cause the game localisation to be initialized too early
* Fixed an error inside the ModQuery if a null prefab is inside ZNetScene or ObjectDB
* Fixed an error caused by releasing a render texture too early
* Fixed KeyHints for keyboard, Gamepad hints are still not working a 100%
* Removed changing the hammer category tab width and removed re-parenting of the tab border for better compatibility

## Version 2.10.4
* Fixed that same item conversions could not be added to different stations
* Fixed custom skills names were not being added to autocomplete in cheat commands
* Fixed wrong console outputs when using custom skills in cheat commands

## Version 2.10.3
* Fixed ServerCharacters from showing up in the compatibility window as it does it's own version check on the modified version string
* Added explanation for further steps to the compatibility window

## Version 2.10.2
* Changed PieceConfig to not override piece requirements when no custom requirements were provided
* Changed PieceConfig, ItemConfig and RecipeConfig to remove non valid requirements (no item name or amount set)
* Changed Mocks to not resolve fields and properties marked as `NonSerialized`
* Improved warning messages with custom localization
* Fixed warning messages with a source mod where always attributed to Jotunn.Logger instead of the correct calling type

## Version 2.10.1
* Added [Server Troubleshooting](https://github.com/Valheim-Modding/Wiki/wiki/Server-Troubleshooting) link as a button to the compatibility window
* Changed "open log file" button to "open log folder" in the compatibility window
* Fixed an edge case where PrefabManager.Cache preferred the prefab for ObjectDB parent instead of the scene object
* Fixed an error when an independent mod adds a prefab twice to ObjectDB. A warning will be logged with the prefab name and hash to help debugging, but vanilla or other mods may still have issues with the double prefab
* Fixed an error when available pieces in the build hammer where not initialised properly
* Fixed an error that could occur when a build tab is localised
* Fixed dynamic build tab width was not working for localised tabs

## Version 2.10.0
* Deprecated "ChanceToSpawn" in LocationConfig and made it compatible with version 0.212.6
* Added Remove and Destroy methods to the ZoneManager
* Added method to inject a ZoneLocation at runtime to the ZoneManager
* Added missing GetClutter without mod guid to ModQuery and cleanup code
* Added catching of patch errors with the ModQuery to log a clean error on the console and avoid unrelated errors
* Added AddInitialSynchronization to SynchronizationManager, this allows sending arbitrary data to the connecting client and making sure it will be received before the client's connection is fully established
* Added registration of custom ConfigFiles to SynchronizationManager
* Fixed RenderManager inconsistency between a headless server and normal client. Instead of always returning null, an empty sprite is returned on headless when it should have been rendered
* Fixed trying to insert Jötunn's localisation multiple times

## Version 2.9.0
* Big compatibility window improvements: internal cleanup, better use of available space, button to open the log file, improved language and added german localization
* Changed that mocked shaders will not be resolved on a headless server because they are not available. This stops unnecessary warnings from being printed
* Fixed crossplay connection issues properly and removed the hotfix from Jotunn 2.8.1
* Fixed admin check was false instead of true in some special cases

## Version 2.8.1
* Implemented hotfix for PlayFab connection issues (disables vanilla compression again, but works for now at least)

## Version 2.8.0
* Compatible with Valheim version 0.211.11
* Fixed connection issues in Steam multiplayer with the latest Valheim patch. Crossplay (XBox multiplayer) is not working yet due to bigger changes, we are working on solving this issue
* Marked PatchInit attribute as obsolete

## Version 2.7.9
* Added ZoneManager.OnVanillaClutterAvailable event
* Added CustomClutter to ModRegistry
* Fixed mocked material textures/shaders where not directly fixed after they were injected. Properties that are not available at this time are still delayed
* Fixed connection issues with the upcoming cross play update
* Fixed admin checks for the upcoming cross play update

## Version 2.7.8
* Added custom clutter, check out the tutorial at https://valheim-modding.github.io/Jotunn/tutorials/zones.html#adding-clutter
* Improved mod compatibility window, the disconnect reason is shown inside the Jotunn window
* Fixed mocking of some textures loaded by the game after vanilla prefabs are available

## Version 2.7.7
* Fixed GUIManager.BlockInput also blocking Escape from opening the menu in-game
* Fixed Shader mocking and enabled it again. Shaders are a bit special and only those that are correctly marked as mocks are resolved, please see https://valheim-modding.github.io/Jotunn/tutorials/asset-mocking.html#shader-mocking
* Fixed disabling a piece inside a PieceConfig was not working
* Made UndoCreate and UndoRemove more extensible

## Version 2.7.6
* Added fixed Hint to ButtonConfig, overrides translated HintToken
* Fixed source mod of new prefabs in custom locations is now set to the corresponding mod instead of Jotunn

## Version 2.7.5
* Disabled mock shader resolve, seems to be causing problems and needs to be investigated more

## Version 2.7.4
* Changed that non-resolvable mock textures on a material only print a warning instead of skipping the whole prefab
* Fixed mock system did not resolve some arrays correctly
* Improved warning messages to include the source mod where available

## Version 2.7.3
* Added UndoManager to handle undo and redo actions and queue management, check out the tutorial at https://valheim-modding.github.io/Jotunn/tutorials/undoqueue.html (big thx to Jere)
* Refactored and improved the ModQuery utility
* Reworked mock system to improve world loading time, up to 5x faster than the old system
* Added support for shaders and materials to the mock system

## Version 2.7.2
* Fixed possible NRE in SyncManager
* Opened up some previously internal interfaces for mods to use (e.g. BepInExUtils)

## Version 2.7.1
* Fixed Bep ConfigManager hooking for config sync not working sometimes
* Slight improvements of startup time

## Version 2.7.0
* Removed mod settings as they have caused problems and are a redundant feature. The BepInEx ConfigurationManager can be used instead

## Version 2.6.12
* Fixed JotunnBuildTask. This has no effect on the actual game but fixes the NuGet package upload, meaning the mod version and NuGet version match again

## Version 2.6.11
* Compatible with Valheim version 0.209.8
* Fixed ModQuery has not cleared old prefabs, resulting in null instances

## Version 2.6.10
* Fixed mod settings could bypass ServerSync settings
* Fixed mod settings slider could bypass readonly settings

## Version 2.6.9
* Fixed error of ModQuery if no ObjectDB/ZNetScene is available.

## Version 2.6.8
* Changed mod settings to display all mods, not just Jötunn ones
* Added config option to disable the mod settings completely
* Added helper methods for CustomConfigs
* Fixed the PrefabManager Cache chose a child GameObject in rare cases, even if a better prefab with the same name existed
* Fixed cache path of rendered icons could contain illegal characters
* Fixed NRE of icon rendering if the prefab has null bones
* Fixed language loading if an empty language was saved
* Fixed tabs were rebuilt every time in RemovePieceCategory, even if categories have not changed
* Fixed NRE of ModQuery and slightly improved performance
* Fixed vanilla items could be detected as modded ones in ModQuery if the mod calls UpdateItemHashes in a prefix before vanilla
* Improved performance of adding and retrieving custom pieces

## Version 2.6.7
* Added display of the Valheim version string to the compatibility window. If a mismatch is produced by a mod, it will be displayed accordingly
* Added the ModQuery class which allows to get metadata about content of loaded mods, including non-Jötunn ones. It is disabled by default to not create unnecessary loading time when not used
* Added hammer tab UI settings to the public API

## Version 2.6.6
* Added automatic refresh of vanilla locations after OnVanillaLocationsAvailable to prevent ZNetView problems
* Added GetPieceCategory to the PieceManager for runtime translation of custom piece table categories to their int values
* Fixed NRE error message when client has disconnected before initial data sending

## Version 2.6.5
* Added RemovePieceCategory to the PieceManager to remove a category from a piece table again (works at runtime)
* Added GetPieceTables to the PieceManager to get a list of all piece tables in the game
* Added dynamic tab width calculation for custom piece categories
* Fixed pieces with PieceCategory.All were not displayed in custom tabs

## Version 2.6.4
* Fixed a CustomLocation was not prepared correctly if a deactivated prefab was passed, causing it to spawn inside itself when proximity loaded again
* Fixed vanilla piece categories could be hidden when a mod used the long piece table name

## Version 2.6.3
* Fixed connection error on first connection attempt with QuickConnect
* Fixed the compatibility window was not showing up, if the server has no password
* Fixed a client could sometimes connect to a server, even if mods are incompatible

## Version 2.6.2
* Fixed custom category display using Auga (and probably other UI mods)
* Fixed Mod Settings FPS drop

## Version 2.6.1
* Removed the MMHOOK dependency from Jötunn. Mods using MMHOOK themself should list the HookGenPatcher as a dependency directly.
* Jötunn's PrebuildTask does not generate MMHOOK dlls any more, publicized dlls can still be generated automatically.

## Version 2.6.0
* Compatible with Valheim version 0.207.20
* KeyHint performance improvements
* Fixed custom RPCs not routing to "self"
* Fixed empty KeyboardShortcut saving (thx Heinermann)
* Fixed Settings closing on Escape in KeyBind

## Version 2.5.1
* Added consumable items to CreatureConfig
* Added faction/group to CreatureConfig
* Added cumulative level effects for custom creatures (thx A Sharp Pen)
* Compiled against BepInEx 5.4.1900

## Version 2.5.0
* Added utility methods for texture rescaling to Utils.ShaderHelper
* Added CustomLocation.IsCustomLocation to check if a prefab is a custom location added by Jötunn
* Added CustomVegetation.IsCustomVegetation to check if a prefab is a custom vegetation added by Jötunn
* Added ZoneManager.GetCustomLocation
* Fixed setting MapOverlay.Enabled in code to also change the GUI toggle
* Fixed ZPackage corruption in certain custom RPC scenarios

## Version 2.4.10
* Added CreatureManager to inject custom creatures into the game using basic drop and spawn configs (see https://valheim-modding.github.io/Jotunn/tutorials/creatures.html for a tutorial)
* Added optional icon cache to the RenderManager (thx MSchmoecker)

## Version 2.4.9
* LocationContainer always create instances, add your custom locations directly to the manager if you don't want to alter it further (see https://valheim-modding.github.io/Jotunn/tutorials/zones.html#creating-locations-from-assetbundles for more information)

## Version 2.4.8
* Added mock support for custom vegetation
* Added mock support for DropTable structs
* Removed direct GO mock replacement

## Version 2.4.7
* Fixed mock resolving of certain components (piece place effects for example)

## Version 2.4.6
* Custom skills add an additional localization token to the game using the format "$skill_\{hashcode\}"
* Fixed multiple issues with custom locations (mocking, ZNetView handling, RandomSpawns)
* Added FixReference property to CustomLocation, obsoleted the parameter on ZoneManager.AddLocation and the old CustomLocation constructors
* Added possibility to create CustomLocation instances as early as the mod's Awake()
* __Mod authors are encouraged to adapt their mods to the new FixReference property__

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
