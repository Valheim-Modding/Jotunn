# Getting started

## Dependencies
- [BepInEx](https://github.com/BepInEx/BepInEx)
- [HarmonyX](https://github.com/BepInEx/HarmonyX)

## Installation

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