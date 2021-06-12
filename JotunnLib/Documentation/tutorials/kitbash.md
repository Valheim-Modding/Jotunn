# Kitbash
[Kitbashing](https://en.wikipedia.org/wiki/Kitbashing) refers to using (parts of) existing prefabs to creating a new prefab for use in your mod.

## Setup
You will need the ripped Unity project, follow [these instructions](https://github.com/Valheim-Modding/Wiki/wiki/Valheim-Unity-Project-Guide) (You only need to follow it up to the ILSpy part which is optional for what we do here) if you have not set this up yet.


Prefabs you want to import from an AssetBundle should not be created in the ripped project, but in your own Unity project, to avoid accidentally importing copyrighted assets!
 
## KitBashing the ripped Unity project
Create an empty GameObject to assemble your master copy.
This object will be used as reference for position, rotation and scale of the KitBashSourceConfigs.

### Simple kitbash piece
For the first example, we won't be using AssetBundles, only the ripped Unity project as reference.

We use the "empty" CustomPiece as a base, so let's recreate that in Unity.

Create a new folder in the ripped project, so we don't get confused about what's vanilla and what's not.

Create an empty GameObject in the ripped project by right-clicking on the hierarchy in any scene/prefab and select **Create empty**. Drag this new GameObject into the new folder for our Kitbashes. The GameObject in the hierarchy will turn blue, remove it from the hierarchy, it is no longer needed here.

Rename your new GameObject to `simple_kitbash` and add a Cube (**3D Object > Cube**).

You should now have something like this:

![Kitbash Simple Kitbash Base](../images/data/kitbashSimpleKitbashBase.png)

We can now start adding other pieces from vanilla assets! 

Open a prefab and look for the lowest (in the hierarchy) GameObject that has everything that you want.

For example, the Ruby:
![Kitbash Ruby Source](../images/data/kitbashRubySource.png)

We don't need every GameObject in this prefab, we only want the visual (MeshRenderer) and usually also any Colliders.
For the Ruby this means that we should copy the `model` gameObject, right click and select **Copy**.

Create a new `KitbashSourceConfig` for each time you copy a part, so you don't forget where it came from.
```cs
new KitbashSourceConfig
{
    name = "eye_1",
    sourcePrefab = "Ruby",
    sourcePath = "attach/model"
}
```

Go back to `simple_kitbash` and paste the copied GameObject.
Unity seems to paste 50 units above the source location, so either zoom out or edit that manually to see the pasted part.
Move & scale the GameObject however you want, you can edit all properties of the Transform

Once everything is in position, add the position, rotation and scale of the GameObject in the KitbashSourceConfig:
![Kitbash Ruby Transform](../images/data/kitbashRubyTransform.png)
```cs
new KitbashSourceConfig
{
    name = "eye_1",
    sourcePrefab = "Ruby",
    sourcePath = "attach/model",
    position = new Vector3(0.528f, 0.1613345f, -0.253f),
    rotation = Quaternion.Euler(0, 180, 0f),
    scale = new Vector3(0.02473f, 0.05063999f, 0.05064f)
}
```

You can also have entire trees of GameObjects as the source, for example the Draugr Bow:

![Kitbash Draugr Bow Source](../images/data/kitbashDraugrBowSource.png)

In this case the visuals are in 2 pieces, we need both `bow` and its child `Cube` (the drawstring)

To do this, we simply use the parent `bow` as the source, the child will be copied along, just like in the Unity editor.
```cs
new KitbashSourceConfig
{
    name = "mouth",
    sourcePrefab = "draugr_bow",
    sourcePath = "attach/bow",
}
```

Again, paste the `bow` into `simple_kitbash` and position it until we have created our masterpiece
![Kitbash Draugr Bow Transform](../images/data/kitbashDraugrBowTransform.png)
```cs
new KitbashSourceConfig
{
    name = "mouth",
    sourcePrefab = "draugr_bow",
    sourcePath = "attach/bow",
    position = new Vector3(0.53336f, -0.315f, -0.001953f),
    rotation = Quaternion.Euler(-0.06500001f, -2.213f, -272.086f),
    scale = new Vector3(0.41221f, 0.41221f, 0.41221f)
}
```

We now have all the KitbashSourceConfigs we need, so we can put it all together:
```cs
var simpleKitbashPiece = new CustomPiece("piece_simple_kitbash", "Hammer"); 
var piece = simpleKitbashPiece.Piece;
piece.m_icon = testSprite; 
simpleKitbashPiece.FixReference = true;
PieceManager.Instance.AddPiece(simpleKitbashPiece);
KitbashManager.Instance.Kitbash(simpleKitbashPiece.PiecePrefab, new KitbashConfig { 
    layer = "piece",
    KitbashSources = new List<KitbashSourceConfig>
    {
        new KitbashSourceConfig
        {
            name = "eye_1",
            sourcePrefab = "Ruby",
            sourcePath = "attach/model",
            position = new Vector3(0.528f, 0.1613345f, -0.253f),
            rotation = Quaternion.Euler(0, 180, 0f),
            scale = new Vector3(0.02473f, 0.05063999f, 0.05064f)
        },
        new KitbashSourceConfig
        {
            name = "eye_2",
            sourcePrefab = "Ruby",
            sourcePath = "attach/model",
            position = new Vector3(0.528f, 0.1613345f, 0.253f),
            rotation = Quaternion.Euler(0, 180, 0f),
            scale = new Vector3(0.02473f, 0.05063999f, 0.05064f)
        },
        new KitbashSourceConfig
        {
            name = "mouth",
            sourcePrefab = "draugr_bow",
            sourcePath = "attach/bow",
            position = new Vector3(0.53336f, -0.315f, -0.001953f),
            rotation = Quaternion.Euler(-0.06500001f, -2.213f, -272.086f),
            scale = new Vector3(0.41221f, 0.41221f, 0.41221f)
        }
    }
}); 
```

That's it! We can now place our kitbashed piece in game!
![Kitbash Simple In Game](../images/data/kitbashSimpleInGame.png)

## Advanced kitbash piece
### Skeleton prefab
We can add a lot more features to the Kitbashed object if we start with a skeleton created in Unity (a separate project from the ripped project!)
![Kitbash Odin Statue Skeleton](../images/data/kitbashOdinStatueSkeleton.png)

Here we have prepared quite a few things:
- Collider
  - Not all parts that we copy have attached colliders, sometimes we need to prepare additional ones
- ZNetView
- Piece
  - We don't need to add the required resources here, we can do this in `CustomPiece` later, or use [JVMmocks_](./asset-mocking.md)
- WearNTear
- Animator
  - The animator animates an empty GameObject called `pivot`, we can later add parts to this to make them also rotate along.

### Loading the skeleton
See [Asset creation - AssetBundle ](./asset-creation.md#assetbundle) on how to create the AssetBundle
```cs 
AssetBundle kitbashAssetBundle = AssetUtils.LoadAssetBundleFromResources("kitbash", typeof(TestMod).Assembly);
try
{ 
    KitbashObject kitbashObject = KitbashManager.Instance.Kitbash(kitbashAssetBundle.LoadAsset<GameObject>("piece_odin_statue"), new KitbashConfig
    {
        layer = "piece",
        KitbashSources = new List<KitbashSourceConfig>
        {
            ...
        }
    }); 
    //get the kitbashed prefab using kitbashObject.Prefab
} finally
{
    kitbashAssetBundle.Unload(false);
}
```

### Using exploded pieces
Many of the Pieces in Valheim have a special animation when destroying them, where they break up into many parts, for example, the Spinning Wheel:
![Kitbash Spinning Wheel New](../images/data/kitbashSpinningWheelNew.png)

Disable the `New` GameObject, and enable the `SpinningWheel_Destruction` GameObject instead

![Kitbash Spinning Wheel Exploded](../images/data/kitbashSpinningWheelExploded.png)

These parts usually have the Worn version of the material, which distorts the mesh a bit, update all materials to the new version to get a good look at all parts

![Kitbash Spinning Wheel Exploded New Mats](../images/data/kitbashSpinningWheelExplodedNewMats.png)

We can now use these smaller parts for our kitbash, along with the new material:

![Kitbash Spinning Wheel Wheel Part](../images/data/kitbashSpinningWheelWheelPart.png)

```cs
new KitbashSourceConfig
{
    sourcePrefab = "piece_spinning_wheel",
    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.002_SpinningWheel_Broken.018",
    materials = new string[]{
        "SpinningWheel_mat",
        "TearChanal_mat"
    }
},
```
### Materials
Any material can be used, it does not have to be associated with the original mesh (many materials will map terribly though :D)

Drag & drop Materials onto the mesh to "paint" your kitbashed GameObject
![Kitbash Change Material](../images/data/kitbashChangeMaterial.png)

Add the `materials` property to your KitbashSourceConfig:
```cs
new KitbashSourceConfig
{
    name = "eye_1",
    sourcePrefab = "Ruby",
    sourcePath = "attach/model",
    position = new Vector3(0.528f, 0.1613345f, -0.253f),
    rotation = Quaternion.Euler(0, 180, 0f),
    scale = new Vector3(0.02473f, 0.05063999f, 0.05064f),
    materials = new string[]{ "antifreezegland" }
},
```

### Target parent
If you're using a more complicated skeleton, make sure that the master copy GameObject (in the ripped Unity project) is set up exactly the same as your skeleton in the AssetBundle (notice the gameObjects `collider`, `new` and `pivot`):
![Kitbash Skeleton Master Copy](../images/data/kitbashSkeletonMasterCopy.png)

You can now paste the parts into for example `pivot` to add the parts to the rotating pivot.
The Transform values are relative to the parent GameObject, so make sure that all positions are **exactly** the same as your skeleton.

Use the `targetParentPath` field to set the target:
```cs
 new KitbashSourceConfig
{
    sourcePrefab = "guard_stone",
    sourcePath = "new/default",
    targetParentPath = "new/pivot",
    position = new Vector3(0, 0.0591f ,0),
    rotation = Quaternion.identity,
    scale = Vector3.one * 0.2f,
    materials = new string[]{
        "bronze",
        "obsidian_nosnow"
    }
},
```

### Collider
The gear, like all (?) exploded parts, has no Collider, so currently, our `piece_odin_statue` will clip into the table a bit, as only the Ward mesh is used to place it.
To fix this, we can use a custom Collider, and remove the mesh collider, to save on processing power.

In the master copy, update the values of the Collider so they match close enough (don't worry too much about pixel perfect accuracy here, try it in game to check how it feels to run into)
![Kitbash Collider](../images/data/kitbashCollider.png)

Once you are happy with the collider, copy the values from your master copy into your skeleton.
![Kitbash Collider Skeleton](../images/data/kitbashColliderSkeleton.png)

We still need to get rid of the MeshCollider that is attached to the `new/default` of the Ward:
```cs
kitbashObject.KitbashApplied += () =>
{
    //We've added a CapsuleCollider to the skeleton, this is no longer needed
    Object.Destroy(kitbashObject.Prefab.transform.Find("new/default").GetComponent<MeshCollider>());
};
```

