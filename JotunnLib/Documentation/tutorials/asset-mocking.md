# Mock References

What are mock references? What do they do? What do they solve?

Mock's are placeholder objects which you create inside of asset bundles, and prepend the specific `JVLmock_` prefix to the name of an already existing prefab. When we load our assets at runtime, any references to mock objects will be recursively resolved until a depth of 3. When resolving references, we iterate through scene objects attempting to match our objects name with an existing one in the scene, and then duplicate the asset such that we may use it for our own purposes, without needing to copy copyrighted content within our own custom assets! Neat right?! And better yet, we don't even need to code anything ourselves to do this, just make sure that you set the `fixReference` bool on any `Custom*` entities to true and the mock reference will automatically be resolved at runtime.

## Mocked Assets

For this example we took the Cheat Sword of Valheim and added it to the game again as a new item using only mocked assets. You can check out the result in our [example mod's](https://github.com/Valheim-Modding/JotunnModExample) Unity project. Note that the example mod uses its own Unity project for mod assets. Please read the [asset creation guide](asset-creation.md) on how to setup such a mod project yourself.

We'll start by creating a folder that will hold all our new asset, let's call it `CheatSword`. We now want to drag and drop the existing asset from the ripped game project to our custom item project. The original asset is called `SwordCheat`. Open the ripped project, search for that prefab and drop onto the newly created folder in your working project. Rename the prefab to something different than the vanilla prefab, we used `Cheaty` as the name.

Open the prefab by double-clicking it. If you followed our [asset creation guide](asset-creation.md), all references to the vanilla scripts should still be there. If this is not the case, your prefab now looks something like this:

![Missing script refs](../images/data/cheaty_missingrefs.png)

You will have to fix those script references first. Be sure to **always fix the script references for all items that you've brought into the new project** including ItemDrop, etc. Doing this manually unfortunately clears out all values previously set on those components. To avoid this, you can use the Unity package [NG Script recovery](https://assetstore.unity.com/packages/tools/utilities/ng-missing-script-recovery-102272). Install it and let NG fix the references. We won't go into details of that process, so please read up on NG usage on their website.

After fixing the script references for the cheat sword (if needed), you will still have stuff missing. This is where the mocks come into play. For Jötunn to resolve the vanilla assets for you at runtime, we need to create corresponding assets inside our project, reference them inside our components and tell Jötunn to fix those references for us. Let's look at the vanilla icon reference in the ItemDrop component for example:

![script refs fixed](../images/data/cheaty_refsfixed.png)

Identify all references on all GameObjects of your prefab gone "Missing" like this. In case of the Cheat Sword, those were `Icons`, `Materials`, a `Mesh` and some `fx Prefabs`. Create folders for all of those types and create empty objects using the same type as the previous referenced asset. It is important that you name those objects exactly like the vanilla objects you want to reference and prefix those with `JVLmock_`. This is how that looks for our sword:

![mocked icon](../images/data/cheaty_mockicon.png) ![mocked materials](../images/data/cheaty_mockmaterial.png)

![mocked mesh](../images/data/cheaty_mockmesh.png) ![mocked prefabs](../images/data/cheaty_mockprefab.png)

Now assign all mocked objects in the components of your custom prefab instead of the vanilla objects. Replace the icon reference with the newly created mock icon, the material reference with the newly created mock material and so on. This is how our missing icon reference should look like now for example:

![mocks assigned](../images/data/cheaty_mocksassigned.png)

That's it. When importing your prefabs into the game, Jötunn will automatically reference the vanilla objects for you at runtime. Please make sure you set `fixReference: true` when you create your custom entities:

```cs
private void AddMockedItems()
{
    // Load completely mocked "Shit Sword" (Cheat Sword copy)
    var cheatybundle = AssetUtils.LoadAssetBundleFromResources("cheatsword", typeof(JotunnModExample).Assembly);
    var cheaty = cheatybundle.LoadAsset<GameObject>("Cheaty");
    ItemManager.Instance.AddItem(new CustomItem(cheaty, fixReference: true));
    cheatybundle.Unload(false);
}
```

> [!NOTE]
> You don't need to copy vanilla prefabs in order to use mocked references. You can facilitate the system using you own prefabs, too. Just make sure to create a custom entity using that prefab (CustomPrefab, CustomItem, etc) and set the fixReference parameter to true.

If you have been following the Unity Asset Creation guide, you can return back to where you [left off](asset-creation.md#assetbundle).

## Mocking GameObjects from within a Prefab

As well as mocking assets to use for referencing, it is also possible to reference complete GameObjects from within an asset. The Recursive Reference Resolver will dig through children of an asset, querying for any prefabs with the `JVLmock_` prefix, it will strip the prefix and then try to resolve the name to an existing asset.

This is also by no means limited to vanilla assets. In our example project we create [custom locations and vegetation](zones.md). For this we mocked our own custom piece `piece_lul` as well as the vanilla prefab `crystal_wall_1x1` like this:

![mocking gameobjects](../images/data/mock_gameobjects.png)