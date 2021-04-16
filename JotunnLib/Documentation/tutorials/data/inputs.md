# Input manager



With the input manager you can create and maintain your custom keybindings.

### Registering a button

```cs

	// Register button from config value
	// parameters:
	// ModGUID: your mod's guid
	// name: the name of your button
	// key: the key code
	// repeatDelay: repeating delay
	// repeatInterval: interval between repeats
	InputManager.Instance.AddButton(ModGUID, buttonName, (KeyCode) Config["JotunnLibTest", "KeycodeValue"].BoxedValue, 2.0f, 0.1f);

```

If you bind it to a configuration value like in this example, you can change the keybinding in the settings menu.


### Registering event

The input manager also provides an eventhandler to give you an entry point to register your buttons

```cs

	InputManager.Instance.InputRegister += RegisterButtons;

```


### Note until version 1.1:

Until version 1.1 there is a small inconvenience for mod's:

If multiple mods bind the same button, the button's are internally split into two. This is to prevent another mod overwriting a keybinding with its keycode.
The downside: For the moment, both mod's will trigger if one of the bound keys is pressed.
