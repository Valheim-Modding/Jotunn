# Registering custom data
This section will show how to register custom data into the game, such as custom prefabs, items, recipes, vegetation, etc.

# Saving
All custom data that is registered (prefabs, items, recipes) will automatically be saved and loaded by the game on logout/reload, and will persist across game sessions as long as the mods are still installed.  

**WARNING:** If either modded characters or worlds are loaded _without_ the mods installed, you may lose your modded items on that character/world, or it may produce undefined behaviour. I wouldn't recommend trying this on a character/world you care deeply about.

# Examples
Generally, you do this by adding a handler for a register event, and registering your data in there. In general, the register event will pass a null `sender`, and empty `EventArgs` unless otherwise specified.

Each section will have examples showing how this is done. All of the examples shown are published in the [TestMod](https://github.com/jotunnlib/jotunnlib/tree/main/TestMod), availible in our [GitHub repo](https://github.com/jotunnlib/jotunnlib).