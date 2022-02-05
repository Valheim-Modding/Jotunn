# Step-by-Step Guide

# [1. Development Environment](#tab/tabid-1)
# Development Environment

* We begin with installing [Visual Studio 2019](https://visualstudio.microsoft.com/) as our IDE. Download the installer and run it on your Computer.

* Inside the visual studio installer, ensure that `.NET Desktop Development` and `.NET Core Cross-Platform Development` are installed, then click on the `Individual Components` tab and select `.NET Framework 4.6.2`:<br />
![Components](../images/getting-started/vs-InstallerComponents.png)

* Fork our [ModStub repository](https://github.com/Valheim-Modding/JotunnModStub) using github. You can also [use the template function of github](https://github.com/Valheim-Modding/JotunnModStub/generate) to create a new, clean repo under your account. You should have your own copy of the JotunnModStub now. To get your link to the repo click on `Code` on the front page of your repo and copy the link in it.<br />
![Forked github stub](../images/getting-started/gh-ForkedStub.png)

* Open Visual Studio and create a new project with `Clone a repository`. Paste the URL to your newly created repository on github and select a folder on your local hard drive for it.<br />
![VS Clone forked stub](../images/getting-started/vs-CloneForkedStub.png)

## BepInEx and unstripped Unity dlls

* For a mod to function at all you need BepInEx installed to Valheim and also the unstripped Unity libs as the devs of Valheim decided to strip their shipped versions down to just the methods they use. This is some sort of "protection" which totally does not hinder us at all ;)

* If you are using Vortex or any other mod manager, you most likely have the folders already in Valheim. If not download the [BepInExPack for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) and extract its contents anywhere except the Valheim installation folder. Inside the pack is another folder called `BepInExPack_Valheim`. Copy the contents of that folder into your own Valheim installation.


# [2. Customising the ModStub project](#tab/tabid-2)
# Customising the ModStub project

* Once you have your base project, select the solution in the solution explorer, hit F2 to rename the solution as required. Rename your plugin project, an all namespace references, then right click your project settings and ensure the assembly name has also been changed.

* Rename the `PluginGUID` `PluginName`, and `PluginVersion` to match your intended base release metadata. Your PluginGUID should contain your github username/organisation.

* Your project base is now ready for use! You can proceed to step 3 to activate the Jötunn PreBuild tasks which add references, publicized dlls and MMHook dlls to your project. Or select a specific [Tutorial](../tutorials/overview.md) to learn more about Jötunn and mod development for Valheim.

## Optional: Use or create a project template

* You can grab the [pre built VS Project Template](https://github.com/Valheim-Modding/JotunnModStub/raw/master/JotunnModStub.zip) which you can use to add new projects to your current solution, based on the mod stub boilerplate. Or create your own Template file using `Project` -> `Export Template`.

* Place the project template into your<br />
![VS Project Template Location](../images/getting-started/vs-ProjectTemplateLocationpng.png)

* Restart visual studio. You can now create a new project using the imported templated. Right click your solution, add, new project, then scroll to the bottom where you will find the template:<br />
![Create new project template](../images/getting-started/vs-CreateNewProjectTemplate.png)

# [3. PreBuild Automations](#tab/tabid-3)
# PreBuild Automations

## Add project references

Jötunn can automatically set references to all important libraries needed to mod the game on your project. To use this feature browse to your solution directory. Create a new file called `Environment.props` and place the following contents inside, modifying your `<VALHEIM_INSTALL>` to point to your game directory. This sets up references in your project to BepInEx, the publicised dlls, the unstripped corlibs from Unity and MMHook dlls.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Needs to be your path to the base Valheim folder -->
    <VALHEIM_INSTALL>X:\PathToYourSteamLibary\steamapps\common\Valheim</VALHEIM_INSTALL>
    <!-- This is the folder where your build gets copied to when using the post-build automations -->
    <MOD_DEPLOYPATH>$(VALHEIM_INSTALL)\BepInEx\plugins</MOD_DEPLOYPATH>
  </PropertyGroup>
</Project>
```

> [!WARNING]
> This prebuild task will add references to your current project. If you already have setup that references it most certainly will duplicate them.

## Publicize and MMHook Valheim assemblies

Jötunn can automatically create the publicised and MMHook assemblies for you. To use this feature you must have the `Environment.props` file from the last step. Additionally you need to create a new file called `DoPrebuild.props` in your solution directory. If you are using the ModStub the file will already be there so just change `ExecutePrebuild` to `true`. If you opt not to utilise this automation, you will need to generate your method detours and publicised assemblies, and add them to your projects references manually.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <ExecutePrebuild>true</ExecutePrebuild>
  </PropertyGroup>
</Project>
```

> [!WARNING]
> This prebuild task will automate the generation of MonoMod method detours and publicising of game assemblies. By enabling this, you understand that you will be generating new publicised assemblies and method detours upon PreBuild **IF** the binaries have been updated since the last time the PreBuild has run.

## Manual setup

If you decide to disable the prebuild, you will need to:
- acquire and install [HookGen](https://valheim.thunderstore.io/package/ValheimModding/HookGenPatcher/)
- launch the game to generate event wrappers
- add the `/BepInEx/plugins/MMHook/assembly_*` files to your project references. 
- grab the [Assembly Publicizer](https://github.com/CabbageCrow/AssemblyPublicizer) *(drag drop your `/BepInEx/plugins/MMHook/assembly_*` files on top of the publiciser)* and add the resulting assemblies to your stub project.
- Build the stub, make sure it compiles and automates the postbuild tasks, producing compiled binaries in your plugin directory `BepInEx/plugins/yourtestmod/` folder and also generated a `yourtestmod.dll.mdb` monodebug symbols file.
- You may now proceed to one of the [Tutorials](../tutorials/overview.md)

# [4. PostBuild Automations](#tab/tabid-4)
# PostBuild Automations

Included in the ModStub and ModExample repos is a PowerShell script `publish.ps1`. The script is referenced in the project file as a post build event. Depending on the chosen configuration in Visual Studio the script executes the following actions.

## Building Debug

* The compiled dll file for this project is copied to `<ValheimDir>\BepInEx\plugins`.
* A .mdb file is generated for the compiled project dll and copied to `<ValheimDir>\BepInEx\plugins`.
* `<ValheimModStub>\libraries\Debug\mono-2.0-bdwgc.dll` is copied to `<ValheimDir>\MonoBleedingEdge\EmbedRuntime` replacing the original file (a backup is created before).

## Building Release

* The README.md in `SolutionDir/ProjectDir/package/README.md` is copied to `SolutionDir/ProjectDir/README.md` so that it is present and readable in single-solution-multi-project githubs to give an overview of the project.
* The compiled binary is placed inside of `SolutionDir/ProjectDir/package/plugins`
* The contents of `SolutionDir/ProjectDir/package/*` is archived into a zip, ready for thunderstore upload.

## Debugging with Visual Studio

Please see: [this article](https://github.com/Valheim-Modding/Wiki/wiki/Debugging-Plugins-via-IDE)
