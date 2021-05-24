# Persistent & Synced Configurations

Jötunn itself does not provide any implementations or abstractions for persisent configurations. We do however respect [BepInEx.ConfigEntry](https://bepinex.github.io/bepinex_docs/master/articles/dev_guide/plugin_tutorial/3_configuration.html)'s, their various properties, as well as their [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) properties. Furthermore we have implemented a method of enforcing server side sync on specific configs via the `ConfigurationManagerAttributes` `IsAdminOnly` flag.

**Hint:** `IsAdminOnly` is provided via JVL, not BepInEx.

![Config Manager U I](../images/utils/ConfigManagerUI.png)

### Synced Configurations
We can sync a client configuration with the server by:
- ensuring that the [BaseUnityPlugin](xref:BepInEx.BaseUnityPlugin) has a [NetworkCompatibilityAttribute](xref:Jotunn.Utils.NetworkCompatibilityAttribute) enabled

```cs
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class JotunnModExample : BaseUnityPlugin
```

- and then setting the `IsAdminOnly` flag on the configuration like so:

```cs
// Create some sample configuration values to check server sync
private void CreateConfigValues()
{
    Config.SaveOnConfigSet = true;

    // Add server config which gets pushed to all clients connecting and can only be edited by admins
    // In local/single player games the player is always considered the admin
    Config.Bind("JotunnLibTest", "StringValue1", "StringValue", new ConfigDescription("Server side string", null, new ConfigurationManagerAttributes {IsAdminOnly = true}));
    Config.Bind("JotunnLibTest", "FloatValue1", 750f, new ConfigDescription("Server side float", new AcceptableValueRange<float>(500, 1000), new ConfigurationManagerAttributes {IsAdminOnly = true}));
    Config.Bind("JotunnLibTest", "IntegerValue1", 200, new ConfigDescription("Server side integer", new AcceptableValueRange<int>(5, 25), new ConfigurationManagerAttributes {IsAdminOnly = true}));
    Config.Bind("JotunnLibTest", "BoolValue1", false, new ConfigDescription("Server side bool", null, new ConfigurationManagerAttributes {IsAdminOnly = true}));
    Config.Bind("JotunnLibTest", "KeycodeValue", KeyCode.F10, new ConfigDescription("Server side Keycode", null, new ConfigurationManagerAttributes {IsAdminOnly = true}));
            
    // Add a client side custom input key for the EvilSword
    Config.Bind("JotunnLibTest", "EvilSwordSpecialAttack", KeyCode.B, new ConfigDescription("Key to unleash evil with the Evil Sword"));
}
```

Here we have implemented some BepInEx configuration attributes to act as a showcase for what BepInEx has to offer, as well as our own implementation of synced attributes. This allows admins defined in the servers adminlist.txt to change the values on the fly, however clients without admin have no control over this configs.

To access the configuration entries either use properties or cast the boxed value to the value type:

```cs
private ConfigEntry<int> configurationEntry1;

public void Awake2()
{
    configurationEntry1 = Config.Bind<int>("YourSectionName", "EntryName", 200, new ConfigDescription("Configuration entry #1", new AcceptableValueRange<int>(50, 300)));

    // Reading configuration entry
    int readValue = configurationEntry1.Value;
    // or
    int readBoxedValue = (int)Config["YourSectionName", "EntryName"].BoxedValue;

    // Writing configuration entry
    configurationEntry1.Value = 150;
    // or
    Config["YourSectionName", "EntryName"].BoxedValue = 800;
}
```

If you set `Value` it behaves different to setting `BoxedValue`.

Setting `Value` will apply value ranges (defined in the `ConfigurationManagerAttributes` via `AcceptableValueRange` for example) while `BoxedValue` will have no checks.

### Config synced event

Jötunn provides an event in the SynchronizationManager you can subscribe to: [SynchronizationManager.OnConfigurationSynchronized](xref:Jotunn.Managers.SynchronizationManager.OnConfigurationSynchronized). It fires when configuration is synced from a server to the client. Upon connection there is always an initial sync event. If configuration is changed and distributed during a game session, the event is fired every time you receive or send configuration. This applies to server side configuration only (i.e. `AdminOnly = true`). To distinguish between the initial and recurring config sync use the [ConfigurationSynchronizationEventArgs](xref:Jotunn.Managers.ConfigurationSynchronizationEventArgs):

```cs
SynchronizationManager.OnConfigurationSynchronized += (obj, attr) =>
{
    if (attr.InitialSynchronization)
    {
        Jotunn.Logger.LogMessage("Initial Config sync event received");
    }
    else
    {
        Jotunn.Logger.LogMessage("Config sync event received");
    }
};
```