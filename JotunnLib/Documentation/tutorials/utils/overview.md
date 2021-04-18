# Utils
Jötunn provides some helper utilities to assist developers with common tasks and requirements.

 ### NetworkCompatibility
 The NetworkCompatibility module provides some basic assurances that clients and servers maintain plugin/version synchronisation where the developer desires. Allow for fine control over strictness (or lack thereof) but does not attempt to act as *"anti-cheat"*. We care about interoperability, nothing else.

 ### AssetUtils
 Asset utilities provide some basic asset loading methods to facilitate loading from file, asset bundles, as well as embedded resources.

 ### BoneReorder
 The BoneReorder provides a common method which developers may use to correct bone orderings on assets configured after ripping and importing into unity, which does not respect the BoneOrder provided by asset rippers, such as uTinyRipper.

 ### Configuration
 The configuration manager provides a UI for standard BepInEx `ConfigEntry`'s and respects JVL's server side synchronisation.

 ### SimpleJson
 JVL provides the MIT licensed SimpleJSON via the [SimpleJSON](xref:SimpleJson) namespace.