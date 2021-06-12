# Kitbash
[Kitbashing](https://en.wikipedia.org/wiki/Kitbashing) refers to using (parts of) existing prefabs to creating a new prefab for use in your mod.

## Setup
You will need the ripped Unity project, follow [these instructions](https://github.com/Valheim-Modding/Wiki/wiki/Valheim-Unity-Project-Guide) (You only need to follow it up to the ILSpy part which is optional for what we do here) if you have not set this up yet.


Prefabs you want to import from an AssetBundle should not be created in the ripped project, but in your own Unity project, to avoid accidentally importing copyrighted assets!
 
## KitBashing the ripped Unity project
Create an empty GameObject to assemble your master copy.
This object will be used as reference for position, rotation and scale of the KitBashSourceConfigs.

Let's create a small decorative garden gnome as an example.