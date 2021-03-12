# Getting started

## Dependencies
- [BepInEx](https://github.com/BepInEx/BepInEx)
- [HarmonyX](https://github.com/BepInEx/HarmonyX)
- [DocFX](https://github.com/dotnet/docfx)

## Setting up development environment
Setting up development environment for compilation:

1. Download [BepInEx for Valheim](https://valheim.thunderstore.io/package/download/denikson/BepInExPack_Valheim/5.4.701/) and extract the zip file into your root Valheim directory.
2. Create an environment variable `VALHEIM_INSTALL` with path to root Valheim directory.

## Creating your mod
To use JotunnLib, you must add it as a BepInEx dependency.

```cs
namespace TestMod
{
    [BepInPlugin("com.bepinex.plugins.testmod", "JotunnLib Test Mod", "0.0.1")]
    [BepInDependency("com.bepinex.plugins.jotunnlib")]
    public class TestMod : BaseUnityPlugin
    {
        // ...
    }
}
```