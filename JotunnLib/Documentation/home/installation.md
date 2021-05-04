# Manual Installation Guide

This section will cover how to manually install Jötunn, without using a mod manager.

## 0. Installing BepInEx

Before we even start, make sure you have [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) downloaded and installed. If you have any other mods, odds are you'll have this installed.

If you're installing this manually as well, extract the ZIP archive and move everything from `BepInEx_Valheim` into your Valheim directory (typically something like `C:\<PathToYourSteamLibary>\steamapps\common\Valheim`).
It should look something like this:

![BepInEx Installed](../images/installation/bepinex.png)

## 1. Downloading Jötunn

First, download Jötunn from your prefered distribution source (either works, they're the same):
- [Nexus Mods](https://www.nexusmods.com/valheim/mods/1138)
- [Thunderstore](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)

Make sure you also download the MMHookGen dependency if you have not already. Again, you can use either distribution source, they're the same download:
- [Nexus Mods](https://www.nexusmods.com/valheim/mods/505)
- [Thunderstore](https://valheim.thunderstore.io/package/ValheimModding/HookGenPatcher/)

If you downloaded both, you should have two ZIP files like so (names may vary depending on download source and version, but contents will be the same):

![Downloaded Files](../images/installation/downloads.png)

## 2. Extracting

Now that you have everything downloaded, you'll need to extract them.  

First, navigate to your Valheim BepInEx directory (typically something like `C:\<PathToYourSteamLibary>\steamapps\common\Valheim\BepInEx`).
Now, we can extract them:  

**For MMHookGen**: extract the ZIP and put the `patchers` and `config` folders inside your BepInEx folder. These folders, assuming you have no other mods installed, should look like so:

![BepInEx Patchers Folder](../images/installation/patchers.png)

![BepInEx Config Folder](../images/installation/config.png)

**For Jötunn**: extract the ZIP, and put the `Jotunn.dll` file into your BepInEx `plugins` folder. Your plugins folder should look like so (assuming you have no other mods installed):

![BepInEx Plugins Folder](../images/installation/plugins.png)

## 3. Launch Valheim

That's it, you're done! Now you can launch Valheim and enjoy your mods.

> [!NOTE]
> Your first run of the game may take a few seconds longer than it would without any mods. **This is normal.** This is due to MMHookGen creating the MMHook DLL files that are needed for Jötunn and various mods to run. This will only take longer on first install, and after Valheim updates.
