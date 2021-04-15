# Developing Assets with Unity

New Assets can be created with Unity and imported into Valheim using Jötunn. A Unity project is included in [this repository](https://github.com/Valheim-Modding/JotunnModStub/tree/master/JotunnModUnity).

### Software Requirements

Creation Tools
* [Unity 2019.4.20](https://unity3d.com/unity/whats-new/2019.4.20) - Game engine that Valheim runs in
* Visual Studio (installable with Unity) - editor for our plugin code
* [uTinyRipper](https://sourceforge.net/projects/utinyripper/files/latest/download) - tool to rip assets and scripts from the release version of the game

Game Mods (install these into your game as our mod has dependencies on them)
* [Jötunn, the Valheim Library](https://github.com/Valheim-Modding/Jotunn) - Mod with convenience methods we will use
* [HookGenPatcher](https://valheim.thunderstore.io/package/ValheimModding/HookGenPatcher/) - Patcher that adds convenient hooks to execute our mod code at the right times, that ValheimLib relies on

## Summary of Steps

To add an item to the game, a mod maker will have to:
* Create an item asset in Unity
* Connect the required Valheim game scripts to the asset so that the game can interact with it
* Build the item into an Asset Bundle for importation into the Visual Studio project where they build the mod
* Retrieve the asset from the bundle in the mod code
* And add that item (and relevant recipes to make it) to the game's object database when it launches

This example is heavily based on [iDeathHD's Lead Mod](https://github.com/xiaoxiao921/Lead), and has been merged into the new JVL codebase. Please forgive any discrepancies, we are addressing them and converting them where possible.


# TODO
Throughout the code, we will reference code snippets from an example project. That project has a clear example of how to add a new, craftable item to the game with custom assets. You can get that project in its entirety [here](https://github.com/Valheim-Modding/JotunnExampleMod). You can download that repository and open the Lead.sln file in Visual Studio in order to see the way our mod will be structured.

## Useful Info About Valheim Items

To add the item into the game, we are going to be utilising various manager and utility methods provided by JVL.

[ItemManager.Instance.AddItem(CustomItem item)](xref:JotunnLib.ItemManager.AddItem) method.
But what is a `CustomItem` ?

### CustomItem
A [CustomItem](xref:JotunnLib.Entities.CustomItem) can be instantiated a different number of ways, to facilitate different workflows. In this example we will be providing:

* A `GameObject` that will hold a reference to the item prefab. This prefab contains an `ItemDrop` component.
* A `bool` called FixReferences
 
`ItemDrop` is a class from native valheim, which holds all the needed information for the game to define an item, its name, its functionalities, the text that will show up when hovering it, and so on.

## Unity Editor Setup
Valheim uses Unity Version **2019.4.20**

We will want to have two instances of the Unity Editor running, I know that it can be a bit annoying but it's better for how things will be setup, you'll see shortly.
- One unity project that will be made entirely by [uTinyRipper](https://sourceforge.net/projects/utinyripper/files/latest/download) (explained below in the `Making our Item Prefab` chapter)
- One [unity stub project](https://github.com/Valheim-Modding/JotunnModStub/tree/master/JotunnModUnity) that we will be at first entirely empty, that is made like so

### Unity Stub
1. [Download](https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe) UnityHub directly from Unity or install it with the Visual Studio Installer via `Individual Components` -> `Visual Studio Tools for Unity`.
2. You will need an Unity account to register your PC and get a free licence. Create the account, login with it in Unity Hub and get your licence via `Settings` -> `Licence Management`.
3. Install Unity Editor version 2019.4.20f. [download that and install it](https://unity3d.com/unity/whats-new/2019.4.20).
4. Open Unity and add the ValheimModUnity project.
5. Install the `AssetBundle Browser` package in the Unity Editor via `Window`-> `Package Manager`.
6. Copy all `assembly_*.dll` from `<ValheimDir>\valheim_Data\Managed` into `<ValheimModStub>\ValheimModUnity\Assets\Assemblies`.

 **Copy the assemblies to the new project directly via the filesystem - don't import the dlls via Unity**.

![Game Assemblies](https://i.imgur.com/yVOLfOa.png) ![Assemblies Folder](https://i.imgur.com/4Zun1yU.png)

7. Go back to the Unity Editor and press `Ctrl+R`. This should reload all files and add the Valheim assemblies under `Assets\Assemblies`
8. Click on `Window` -> `PackageManager` and search for `AssetBundleBrowser` and install it.

## Making our Item Prefab

Now we want to make a prefab that is setup similarly as the item prefabs that IronGate did, for that, what's best than looking directly at one of them.

If you haven't already used `uTinyRipper` on the game folder to get the prefabs showing up in the ripped game unity project, please do so.

A tutorial is available [here](https://github.com/Valheim-Modding/Wiki/wiki/Valheim-Unity-Project-Guide) (You only need to follow it up to the ILSpy part which is optional for what we do here)

Let's open the `SpearChitin` prefab as an example.
Should get something like this after opening it

![Assemblies Folder](https://i.imgur.com/taWzwlK.png)

As you can see, the root GameObject has multiple components.
- A rigidbody, for collision handling
- ZNetView for making the prefab network compatible
- ZSyncTransform, same as above
- ItemDrop

The last one holds the most interesting stuff, what's above `Shared` is non-important and is used for an actual item once its spawned, the meta-data we want to edit is stored under `SharedData`, so let's focus on that.

Since there is a stupid amount of fields, let's go over the one that I edited for my custom item [Lead](https://github.com/xiaoxiao921/Lead)

I chose this prefab as a base because almost everything is copied from it, except that I remove the model because its not a spear and remove a child on the projectile prefab that we'll see soon.

* The name: as you can see its a token that is prefixed with `$` . This dollar signifies this is a token due for replacement by localised content. In my case I changed the name to `$custom_item_lead`

* `Icons` : Its an array that is the same size as the number of variant you'll want for you item, if there is only 1 variant, have a single entry in there and drag and drop your 2D Sprite at the Element 0 field.

* Description Token: same thing for the name, mine is `$custom_item_lead_description`

* `Damages`: my item was dealing no damage and had no scaling per level so everything was set to 0.

* `Attack Status Effect`: Status Effect can be used for items, the chitin spear has the Harpooned Status Effect for, well, harpooning stuff.

## Porting the asset

Port the item into the new stubbed unity project, with ALL linked assets (mesh, texture, sound, material, etc). This will be the project from which we make the Asset Bundle to package with our plugin. When you import it, if your script references break, re-apply them and ensure your fields are populated with the desired data.

Before we actually create our bundle, though, you might be wondering - how am I gonna reference game assets in my Unity Project?
If I were to copy paste those, I'd need to fix shaders, and now I'd also have to be worried about copyright infringement, bad.

The solution is easy though, thanks to [JVL](https://github.com/Valheim-Modding/Jotunn), we introduce a [Mock object system](mocks.md). Click the link to learn more about resolving native asset references at runtime.


## AssetBundle

Now we want to make our AssetBundle so that we later inject it with our BepinEx plugin dll

Let's create an Asset Label for the AssetBundle that we'll call `item_lead`

![AssetBundle Label](https://i.imgur.com/RYZN76Q.png)

Now, let's use the `AssetBundle Browser` made by Unity to create our AssetBundle.

Window -> AssetBundle Browser -> Build Tab -> Build

![Click Build](https://i.imgur.com/cdkn6sl.png)

We now want to put our AssetBundle in the BepinEx plugin so that we can later inject it.

![Freshly made AB](https://i.imgur.com/495W7UL.png)


## Implementing your asset ingame using JVL

If you have not done so yet, please ensure you have completed the [relevant visual studio setup](../getting-started.md).
You can then follow the [Asset importing guide](../data/assets.md), [Item Creation](../data/items.md), and [Localization](../data/localization.md) tutorials.