# Jötunn, the Valheim Library

Jötunn (/ˈjɔːtʊn/, "giant"), the Valheim Library was created with the intent to facilitate developer creativity, unify the communities problem solving efforts, and enhance developer productivity by curating a library of common helper utilities, as well as interfaces and abstractions which aid with mod interoperability and networked gameplay consistency.

Some of our major features which we have worked hard on are [mock reference fixing](../data/mocks.md), [NetworkCompatibility / networked mod interop](../utils/networkcompatibility.md), [config synchronisation](../utils/config.md), our [mod stub](https://github.com/Valheim-Modding/JotunnModStub) which provides some basic automations, as well as our various `Manager` interfaces and `Entity` abstractions which allow developers to focus more on creating content, and less on implementation details.

We have lots of features planned for the future, and did hold back on what we wanted to release with so that we could focus on house keeping post merger, however I hope the features we have implemented thus far prove to be a useful base and provide an idea of the consistency we aim to deliver moving forwards.


# TODO

### Saving
All custom data that is registered (prefabs, items, recipes) will automatically be saved and loaded by the game on logout/reload, and will persist across game sessions as long as the mods are still installed.  

**WARNING:** If either modded characters or worlds are loaded _without_ the mods installed, you may lose your modded items on that character/world, or it may produce undefined behaviour. I wouldn't recommend trying this on a character/world you care deeply about.

Each section will have examples showing how this is done. All of the examples shown are published in the [Example Mod](https://github.com/Valheim-Modding/JotunnExampleMod), availible in our [GitHub repo](https://github.com/Valheim-Modding).