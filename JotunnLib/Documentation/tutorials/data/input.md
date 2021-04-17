# Custom inputs
Custom inputs can be registered through the [InputManager](xref:JotunnLib.Managers.InputManager) singleton.

## Example
First, within `Awake` in your mod class, call a method to create and add all your custom key bindings to the [InputManager](xref:JotunnLib.Managers.InputManager).

```cs
// Add custom key bindings
private void AddInputs()
{
    // Add key bindings on the fly
    InputManager.Instance.AddButton(PluginGUID, "JotunnModExample_Menu", KeyCode.Insert);

    // Add key bindings backed by a config value
    // Create a ButtonConfig to also add it as a custom key hint in AddClonedItems
    evilSwordSpecial = new ButtonConfig
    {
        Name = "EvilSwordSpecialAttack",
        Key = (KeyCode)Config["Client config", "EvilSwordSpecialAttack"].BoxedValue,
        HintToken = "$evilsword_beevil"
    };
    InputManager.Instance.AddButton(PluginGUID, evilSwordSpecial);
}
```

Now, to use our input, we can use the `ZInput` class provided by Valheim.

```cs
// Called every frame
private void Update()
{
    // Since our Update function in our BepInEx mod class will load BEFORE Valheim loads,
    // we need to check that ZInput is ready to use first.
    if (ZInput.instance != null)
    {
        // Check if our button is pressed. This will only return true ONCE, right after our button is pressed.
        // If we hold the button down, it won't spam toggle our menu.
        if (ZInput.GetButtonDown("JotunnModExample_Menu"))
        {
            showGUI = !showGUI;
        }
        
        // Use the name of the ButtonConfig to identify the button pressed
        if (ZInput.GetButtonDown(evilSwordSpecial.Name) && MessageHud.instance.m_msgQeue.Count == 0)
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$evilsword_beevilmessage");
        }
    }
}
```