# Map Overlays

You can create your own [MapOverlay](xref:Jotunn.Managers.MinimapManager.MapOverlay) classes using the [MinimapManager](xref:Jotunn.Managers.MinimapManager).


## Creating Overlays

The simplest way to create an overlay is to use the MapOverlay class.

This class provides a Texture2D which you can write to directly. This Texture2D is then rendered directly on top of the Vanilla Minimap.


### Example

In this example we will implement an overlay which shows the world's zones laid overtop of the map.

**Note**: The code snippets are taken from our [example mod](https://github.com/Valheim-Modding/JotunnModExample).


We start by creating a new overlay with a name `"zone overlay"`.
```
SimpleZoneOverlay = MinimapManager.Instance.AddMapOverlay("zone overlay");
```
Next we prepare to populate a `Color[]` array. The results of this array will be set to our overlay.
```
int mapSize = SimpleZoneOverlay.TextureSize * SimpleZoneOverlay.TextureSize;
int zoneSize = 64;
Color[] mainPixels = new Color[mapSize];
int index = 0;
```
We iterate over the dimensions of the overlay and set a pixel in our mainPixels array wherever a zone boundary is.
```
for (int x = 0; x < SimpleZoneOverlay.TextureSize; ++x)
{
    for (int y = 0; y < SimpleZoneOverlay.TextureSize; ++y, ++index)
    {
        if (x % zoneSize == 0 || y % zoneSize == 0)
        {
            mainPixels[index] = color;
        }
    }
}
```
We set the pixels of the overlay to the colours set in our array.
```
SimpleZoneOverlay.OverlayTex.SetPixels(mainPixels);
```

Apple the changes to the overlay. This also triggers the MinimapManager to display this overlay.
```
SimpleZoneOverlay.OverlayTex.Apply();
```


# Map Drawings

You can create your own [MapDrawing](xref:Jotunn.Managers.MinimapManager.MapDrawing) classes using the [MinimapManager](xref:Jotunn.Managers.MinimapManager).

## Map Layer Details

The MapDrawing object has a local Texture2D which corresponds to the following vanilla Minimap texture layers:

1. _MainTex:

    Texture displayed on normal terrain, eg, the colour of meadows.
2. _MaskTex
    
    Mask used to determine whether to display forests or not. 1 for forest, 0 for no forest.
3. _HeightTex
    
    Mask that determines height of different features. eg, Oceans, and mountains. Determines where MountainTex and WaterTex is displayed.
4. _FogTex

    Mask that determines where fog is displayed. Most values mean there is fog. 





## In-Depth Map Layer Details


In addition to the layers used by the MapDrawing object, there are more layers that interact with the Minimap which we do not use.
These are displayed here as a reference.


5. _BackgroundTex

    Colour of background texture. Displayed underneath map? Effects _MainTex. Gives an additional textured texture to the land.
6. _FogLayerTex

    Controls the colour of the Fog overlay. Also controls some sort of shading underneath water. Affected by world light. Lighter in center, darker in outside.
7. _MountainTex

    The texture displayed on mountains. Works with the _heightTex mask.
8. _ForestTex

    Controls the colour of forest areas. Is masked by _MaskTex.
9. _ForestColor

    Could not determine.
10. _WaterTex

    Controls shallow water and shorelines. Does not control deep ocean.
11. _SpaceTex

    Controls the spacey texture outside the large globe.
12. _CloudTex

    Controls the clouds that fly overtop everything else.

### Layer Draw Order

The Minimap is composed using multiple filters and in a specific order. The following are some details we have figured out:

Fog (6) on top, then clouds (12), then forest (8) (but is composited with _HeightTex (3) data).
Then _MainTex (1) is displayed alongside _WaterTex (10) and _MountainTex (7) and _FoglayerTex (6).
_BackgroundTex (5) also affects _MainTex somehow, it's best to just clear it.

_SpaceTex controls the space-themed texture background that's seen outside of the large minimap.

_MaskTex (2), _HeightTex (3), and _FogTex (4) are just used as filters/masks.


## Drawing to Layers

The following colours use full alpha values to correspond to activating a pixel in a layer. (An active pixel in the MapDraw texture is one that will overwrite the vanilla value).
Once a pixel is active it can then be used to enable or disable a filter, in the event of forests or fog, or be used to set height or mainTex to a specific value.

### Drawing to the Main Layer

Choose whichever RGB colours you want, and set the alpha value to maximum.


### Drawing to the Height Layer

Set the R value to correspond to the world height that you want.
Less than 0-30 means ocean height. 31 and up corresponds to variously shaded land heights until it reaches the mountain layer.


## MapDraw Example

In this example we will draw a square originating on each Map Pin. This involves setting the terrain height for the square to a flat value, removing forest, removing fog, and changing the main text colour.


We use this helper function to set pixels.
```
private static void DrawSquare(Texture2D tex, Vector2 start, Color col, int square_size)
{
    for (float i = start.x; i < start.x + square_size; i++)
    {
        for (float j = start.y; j < start.y + square_size; j++)
        {
            tex.SetPixel((int)i, (int)j, col);
        }
    }
}
```
We define our colours and use MeadowHeight that corresponds to an in-game height of 32.

```
private static Color MeadowHeight = new Color(32, 0, 0, 255);
private static Color FilterOn = new Color(1f, 0f, 0f, 255f);
private static Color FilterOff = new Color(0f, 0f, 0f, 255f);
```


We can then iterate over all map pins and call our helper function to draw our squares. Make sure to call Apply() to complete the updates.
```
private static void DrawSquaresOnMapPins(Color color, MinimapManager.MapDrawing ovl, bool extras = false)
{
    foreach (var p in Minimap.instance.m_pins)
    {
        DrawSquare(ovl.MainTex, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), color, 10);
        DrawSquare(ovl.ForestFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), FilterOff, 10);
        DrawSquare(ovl.FogFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), FilterOff, 10);
        DrawSquare(ovl.HeightFilter, MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, ovl.TextureSize), MeadowHeight, 10);
    }
    ovl.MainTex.Apply();
    ovl.FogFilter.Apply();
    ovl.ForestFilter.Apply();
    ovl.HeightFilter.Apply();
}
```









## Toggle Overlays via GUI

