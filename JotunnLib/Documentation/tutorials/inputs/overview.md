# Custom inputs
Custom inputs can be registered through the [InputManager](xref:JotunnLib.Managers.InputManager) singleton. You must hook into the [InputRegister](xref:JotunnLib.Managers.InputManager.InputRegister) event, and create all your inputs within your event handler.

## Example
First, within `Awake` in your mod class, create register handler for the [InputRegister](xref:JotunnLib.Managers.InputManager.InputRegister)

```cs
private void Awake()
{
    InputManager.Instance.InputRegister += initInputs;
}
```

Next, register your inputs within the handler using the `RegisterInput` command from [InputManager](xref:JotunnLib.Managers.InputManager).
This version of [RegisterInput](JotunnLib.Managers.InputManager.RegisterButton(System.String,UnityEngine.KeyCode,System.Single,System.Single)) takes a string argument for the key name (this **MUST** be unique), and a UnityEngine KeyCode as the default arguments

```cs
private void initInputs(object sender, EventArgs e)
{
    InputManager.Instance.RegisterButton("Unmount", KeyCode.V);
}
```