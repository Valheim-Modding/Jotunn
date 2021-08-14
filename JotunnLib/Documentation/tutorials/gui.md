# GUI

To add custom GUI elements to the game it is necessary to add the prefabs or generate the GUI components in code respecting the [Unity UI guidelines](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/index.html).

## CustomGUI event and GameObjects

Valheim creates new clones of the whole menu and ingame GUI everytime the scene changes from start to main and vice versa. So if you dont want to create and draw on your own canvas, you have to add your custom stuff to the right path on every scene change, too. Additionally, Valheim implemented a scaling feature for high resolution display (4K output or high DPI screens) and a pixel correction component called the PixelFix. Theses concepts can be accessed easily via shortcuts through Jötunn. You can subscribe to the event [GUIManager.OnCustomGUIAvailable](xref:Jotunn.Managers.GUIManager.OnCustomGUIAvailable) which gets called everytime the scene changed and Jötunn has created and resolved the current GUI in the scene. In that event call you can load / create your custom GUI components and add them in the transform hierarchy under either the [GUIManager.CustomGUIBack](xref:Jotunn.Managers.GUIManager.CustomGUIBack) or the [GUIManager.CustomGUIFront](xref:Jotunn.Managers.GUIManager.CustomGUIFront) GameObject. The first resides before Valheim's own GUI elements in the transform hierarchy which means the GUI elements added to this GameObject will be drawn *before* the vanilla GUI and therefore appear *behind* of it. The latter is drawn *after* Valheim's GUI elements and therefore your custom GUI will be in *front* of Valheim's GUI.

## Block input for GUI

When drawing GUI elements Valheim does not stop interpreting the Mouse/Keyboard inputs in any way per default. So if you want to interact with your GUI, you would have to interrupt the receiving of input for the player, camera, etc. Jötunn provides a shortcut to enable or disable player and camera input as the convenient method [GUIManager.BlockInput(bool)](xref:Jotunn.Managers.GUIManager.BlockInput(System.Boolean)). When passing `true`, all input to the player is intercepted and the mouse is released from the camera so you can actually click on your custom GUI elements. You **must** call the method passing `false` again to release the input yourself upon closing your GUI components. Jötunn does not handle the release automatically.

## Determine a headless instance

A dedicated server running without GUI is commonly referred to as a headless server. Valheim provides methods in ZNet to determine if the current instance is a dedicated server, a "local" server (aka local game that others can connect to) or a client to another server. Jötunn also provides shortcuts to these via [ZNetExtension](xref:Jotunn.ZNetExtension). Problem is that both approaches require ZNet to be instantiated which is not the case on your mods Awake(). If you need that information early on, the GUIManager provides a method for that: [GUIManager.IsHeadless()](xref:Jotunn.Managers.GUIManager.IsHeadless) returns true if the current game instance is a dedicated/headless server without relying on ZNet. Jötunn also does not register GUI or Input hooks in that case to save on unnecessarily allocated resources.

## Valheim style GUI elements

The [GUIManager](xref:Jotunn.Managers.GUIManager) provides useful methods to create buttons, text element and more at runtime using the original Valheim assets to create a seamless look for your custom GUI components.

### ColorPicker and GradientPicker

![ColorPicker and GradientPicker](../images/data/colorgradientpicker.png)

Jötunn uses a custom made ColorPicker for its ModSettings dialogue which is also available for mods to use. Additionaly there is also a GradientPicker available.

ColorPickerExample:
```cs
GUIManager.Instance.CreateColorPicker(
    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
    r.sharedMaterial.color, // Initial selected color in the picker
    "Choose your poison",   // Caption of the picker window
    SetColor,               // Callback delegate when the color in the picker changes
    ColorChosen,            // Callback delegate when the window is closed
    true                    // Whether or not the alpha channel should be editable
);
```

GradientPicker example:
```cs
GUIManager.Instance.CreateGradientPicker(
    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
    new Gradient(),  // Initial gradient being used
    "Gradiwut?",     // Caption of the GradientPicker window
    SetGradient,     // Callback delegate when the gradient changes
    GradientFinished // Callback delegate when thw window is closed
);
```

A more explanatory example can be found in our [example mod](https://github.com/Valheim-Modding/JotunnModExample).

### Wood panels

![Woodpanel](../images/data/woodpanel.png)

Woodpanels, nicely usable as containers for other gui elements. Can automatically add the draggable Component to the panel object (default: true).

Example:
```cs
var panel = GUIManager.Instance.CreateWoodpanel(
    parent: GUIManager.PixelFix.transform, 
    anchorMin: new Vector2(0.5f, 0.5f), 
    anchorMax: new Vector2(0.5f, 0.5f), 
    position: new Vector2(0f, 0f), 
    width: 400f,
    height: 300f,
    draggable: true);
```

### Buttons

![GUI Button](../images/data/test-button.png)

To create buttons, provide text, the parent's transform, min and max anchors, the position and it's size (width and height).

Example:
```cs
var button = GUIManager.Instance.CreateButton(
    text: "A Test Button",
    parent: testPanel.transform,
    anchorMin: new Vector2(0.5f, 0.5f),
    anchorMax: new Vector2(0.5f, 0.5f),
    position: new Vector2(0f, 0f),
    width: 250f,
    height: 100f);
```

### Text elements

![Text Element](../images/data/text-element.png)

To create a text element, provide text, the parent's transform, min and max anchors, the position and it's size (width and height). You can also let Jötunn add a ContentSizeFitter Component so the text element will have its bounds automatically adjusted to its content.

Example:
```cs
var text = GUIManager.Instance.CreateText(
    text: "Jötunn, the Valheim Lib",
    parent: TestPanel.transform,
    anchorMin: new Vector2(0.5f, 1f),
    anchorMax: new Vector2(0.5f, 1f),
    position: new Vector2(0f, -100f),
    font: GUIManager.Instance.AveriaSerifBold,
    fontSize: 30,
    color: GUIManager.Instance.ValheimOrange,
    outline: true,
    outlineColor: Color.black,
    width: 350f,
    height: 40f,
    addContentSizeFitter: false);
```

### Checkboxes

![Checkbox](../images/data/checkbox.png)

Example:
```cs
var checkbox = GUIManager.Instance.CreateToggle(
    parent: GUIManager.PixelFix.transform,
    width: 40f,
    height: 40f);
```

### Getting sprites

Gets sprites from the textureatlas by name. You find a list of the sprite names [here](../data/gui/sprite-list.md).

```cs
var sprite = GUIManager.Instance.GetSprite("text_field");
```

### Instance properties

The [GUIManager](xref:Jotunn.Managers.GUIManager) also comes with some useful instance properties for your custom assets to resemble the vanilla Valheim style.

- Font AveriaSerif
- Font AveriaSerifBold (the default Valheim font)
- Color ValheimOrange

## Custom GUI Components

Jötunn provides helper Components for mods to use when handling with custom GUI.

### Draggable windows

[DragWindowCntrl](xref:Jotunn.GUI.DragWindowCntrl) is a simple Unity Component to make GUI elements draggable with the mouse. Does respect the window limits.

```cs
// Add the Jötunn draggable Component to the panel
DragWindowCntrl drag = TestPanel.AddComponent<DragWindowCntrl>();

// To actually be able to drag the panel, Unity events must be registered with the Component
EventTrigger trigger = TestPanel.AddComponent<EventTrigger>();
EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
beginDragEntry.eventID = EventTriggerType.BeginDrag;
beginDragEntry.callback.AddListener((data) => { drag.BeginDrag(); });
trigger.triggers.Add(beginDragEntry);
EventTrigger.Entry dragEntry = new EventTrigger.Entry();
dragEntry.eventID = EventTriggerType.Drag;
dragEntry.callback.AddListener((data) => { drag.Drag(); });
trigger.triggers.Add(dragEntry);
```

## Example

In our [example mod](https://github.com/Valheim-Modding/JotunnModExample) we use a [custom button](inputs.md) to toggle a simple, draggable panel with a text and a button on it. That button also gets a listener added to close the panel again. Also the input is blocked for the player and camera while the panel is active so we can actually use the mouse and cant control the player any more.

```cs
// Toggle our test panel with button
private void TogglePanel()
{
    // Create the panel if it does not exist
    if (TestPanel == null)
    {
        if (GUIManager.Instance == null)
        {
            Logger.LogError("GUIManager instance is null");
            return;
        }

        if (GUIManager.PixelFix == null)
        {
            Logger.LogError("GUIManager pixelfix is null");
            return;
        }

        // Create the panel object
        TestPanel = GUIManager.Instance.CreateWoodpanel(
            parent: GUIManager.PixelFix.transform,
            anchorMin: new Vector2(0.5f, 0.5f),
            anchorMax: new Vector2(0.5f, 0.5f),
            position: new Vector2(0, 0),
            width: 850,
            height: 600,
            draggable: false);
        TestPanel.SetActive(false);

        // Add the Jötunn draggable Component to the panel
        // Note: This is normally automatically added when using CreateWoodpanel()
        DragWindowCntrl drag = TestPanel.AddComponent<DragWindowCntrl>();

        // To actually be able to drag the panel, Unity events must be registered
        EventTrigger trigger = TestPanel.AddComponent<EventTrigger>();
        EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
        beginDragEntry.eventID = EventTriggerType.BeginDrag;
        beginDragEntry.callback.AddListener((data) => { drag.BeginDrag(); });
        trigger.triggers.Add(beginDragEntry);
        EventTrigger.Entry dragEntry = new EventTrigger.Entry();
        dragEntry.eventID = EventTriggerType.Drag;
        dragEntry.callback.AddListener((data) => { drag.Drag(); });
        trigger.triggers.Add(dragEntry);

        // Create the text object
        GameObject textObject = GUIManager.Instance.CreateText(
            text: "Jötunn, the Valheim Lib",
            parent: TestPanel.transform,
            anchorMin: new Vector2(0.5f, 1f),
            anchorMax: new Vector2(0.5f, 1f),
            position: new Vector2(0f, -100f),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 30,
            color: GUIManager.Instance.ValheimOrange,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        // Create the button object
        GameObject buttonObject = GUIManager.Instance.CreateButton(
            text: "A Test Button - long dong schlongsen text",
            parent: TestPanel.transform,
            anchorMin: new Vector2(0.5f, 0.5f),
            anchorMax: new Vector2(0.5f, 0.5f),
            position: new Vector2(0, 0),
            width: 250,
            height: 100);
        buttonObject.SetActive(true);

        // Add a listener to the button to close the panel again
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            TogglePanel();
        });
    }

    // Switch the current state
    bool state = !TestPanel.activeSelf;

    // Set the active state of the panel
    TestPanel.SetActive(state);

    // Toggle input for the player and camera while displaying the GUI
    GUIManager.BlockInput(state);
}
```