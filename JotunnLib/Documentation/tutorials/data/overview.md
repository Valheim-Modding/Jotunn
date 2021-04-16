# Managers

This section covers the main interfaces we provide with the library, enabling developers to easily interact with items, recipes, skills, UI, entities, and much more.

# TODO

### Saving
All custom data that is registered (prefabs, items, recipes) will automatically be saved and loaded by the game on logout/reload, and will persist across game sessions as long as the mods are still installed.  

**WARNING:** If either modded characters or worlds are loaded _without_ the mods installed, you may lose your modded items on that character/world, or it may produce undefined behaviour. I wouldn't recommend trying this on a character/world you care deeply about.

Each section will have examples showing how this is done. All of the examples shown are published in the [Example Mod](https://github.com/Valheim-Modding/JotunnModExample), availible in our [GitHub repo](https://github.com/Valheim-Modding).