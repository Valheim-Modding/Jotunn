# Custom inputs
Custom inputs can be registered through the [InputManager](xref:JotunnLib.Managers.InputManager) singleton. You must hook into the [InputRegister](xref:JotunnLib.Managers.InputManager.InputRegister) event, and create all your inputs within your event handler.

## Example
First, within `Awake` in your mod class, create register handler for the [InputRegister](xref:JotunnLib.Managers.InputManager.InputRegister)

```cs
private void Awake()
{
    InputManager.Instance.InputRegister += registerInputs;
}
```

Next, register your inputs within the handler using the `RegisterInput` command from [InputManager](xref:JotunnLib.Managers.InputManager).
This version of [RegisterInput](JotunnLib.Managers.InputManager.RegisterButton(System.String,UnityEngine.KeyCode,System.Single,System.Single)) takes a string argument for the key name (this **MUST** be unique), and a UnityEngine KeyCode as the default arguments

```cs
private void registerInputs(object sender, EventArgs e)
{
    // Init menu toggle key
    InputManager.Instance.RegisterButton("TestMod_Menu", KeyCode.Insert);
}
```

Now, to use our input, we can use the `ZInput` class provided by Valheim:

```cs
private void Update()
{
    // Since our Update function in our BepInEx mod class will load BEFORE Valheim loads,
    // we need to check that ZInput is ready to use first.
    if (ZInput.instance != null)
    {
        // Check if our button is pressed. This will only return true ONCE, right after our button is pressed.
        // If we hold the button down, it won't spam toggle our menu.
        if (ZInput.GetButtonDown("TestMod_Menu"))
        {
            showMenu = !showMenu;
        }
    }
}
```