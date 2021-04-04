using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;

namespace JotunnLib.Managers
{
    public class SynchronizationManager : Manager
    {
        private List<Tuple<string, string, string, string>> cachedConfigValues = new List<Tuple<string, string, string, string>>();

        private BaseUnityPlugin configurationManager;
        internal bool configurationManagerWindowShown;
        public static SynchronizationManager Instance { get; private set; }

        public bool PlayerIsAdmin { get; private set; }

        public void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError("Error, two instances of singleton: " + GetType().Name);
                return;
            }

            Instance = this;
        }

        /// <summary>
        ///     Main Init
        /// </summary>
        internal override void Init()
        {
            // Register RPCs in Game.Start
            On.Game.Start += Game_Start;

            // Hook RPC_PeerInfo for initial retrieval of admin status and configuration
            On.ZNet.RPC_PeerInfo += ZNet_RPC_PeerInfo;

            On.Menu.IsVisible += Menu_IsVisible;
        }

        // Hook Menu.IsVisible to unlock cursor properly and disable camera rotation
        private bool Menu_IsVisible(On.Menu.orig_IsVisible orig)
        {
            return orig() | configurationManagerWindowShown;
        }

        public void Start()
        {
            // Find Configuration manager plugin and add to DisplayingWindowChanged event
            HookConfigurationManager();
        }

        /// <summary>
        ///     Hook ConfigurationManager's DisplayingWindowChanged to be able to react on window open/close
        /// </summary>
        private void HookConfigurationManager()
        {
            Logger.LogDebug("Trying to hook config manager");

            var result = new Dictionary<string, BaseUnityPlugin>();
            configurationManager = FindObjectsOfType(typeof(BaseUnityPlugin)).Cast<BaseUnityPlugin>().ToArray()
                .FirstOrDefault(x => x.Info.Metadata.GUID == "com.bepis.bepinex.configurationmanager");

            if (configurationManager)
            {
                Logger.LogDebug("Configuration manager found, trying to hook DisplayingWindowChanged");
                var eventinfo = configurationManager.GetType().GetEvent("DisplayingWindowChanged");
                if (eventinfo != null)
                {
                    Action<object, object> local = ConfigurationManager_DisplayingWindowChanged;
                    var converted = Delegate.CreateDelegate(eventinfo.EventHandlerType, local.Target, local.Method);

                    eventinfo.AddEventHandler(configurationManager, converted);
                }
            }
        }

        /// <summary>
        ///     Window display state changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void ConfigurationManager_DisplayingWindowChanged(object sender, object e)
        {
            // Read configuration manager's DisplayingWindow property
            var pi = configurationManager.GetType().GetProperty("DisplayingWindow");
            configurationManagerWindowShown = (bool) pi.GetValue(configurationManager, null);

            // Did window open or close?
            if (configurationManagerWindowShown)
            {
                // If window just opened, cache the config values for comparison later
                cachedConfigValues = GetSyncConfigValues();
            }
            else
            {
                // Window closed, lets compare and send to server, if applicable
                var valuesToSend = GetSyncConfigValues();

                // We need only changed values
                valuesToSend = valuesToSend.Where(x => !cachedConfigValues.Contains(x)).ToList();

                // Send to server
                if (valuesToSend.Count > 0)
                {
                    var zPackage = GenerateConfigZPackage(valuesToSend);
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZNet.instance.GetServerPeer().m_uid, nameof(RPC_JotunnLib_ApplyConfig), zPackage);
                }
            }
        }

        /// <summary>
        ///     On RPC_PeerInfo on client, also ask server for admin status
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="rpc"></param>
        /// <param name="pkg"></param>
        private void ZNet_RPC_PeerInfo(On.ZNet.orig_RPC_PeerInfo orig, ZNet self, ZRpc rpc, ZPackage pkg)
        {
            orig(self, rpc, pkg);

            if (ZNet.instance.IsClientInstance())
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(ZNet.instance.GetServerPeer().m_uid, nameof(RPC_JotunnLib_IsAdmin), false);
            }
        }

        // Register RPCs
        private static void Game_Start(On.Game.orig_Start orig, Game self)
        {
            orig(self);
            ZRoutedRpc.instance.Register(nameof(RPC_JotunnLib_IsAdmin), new Action<long, bool>(RPC_JotunnLib_IsAdmin));
            ZRoutedRpc.instance.Register(nameof(RPC_JotunnLib_ConfigSync), new Action<long, ZPackage>(RPC_JotunnLib_ConfigSync));
            ZRoutedRpc.instance.Register(nameof(RPC_JotunnLib_ApplyConfig), new Action<long, ZPackage>(RPC_JotunnLib_ApplyConfig));
        }


        /// <summary>
        ///     Apply a partial config to server and send to other clients
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="configPkg"></param>
        private static void RPC_JotunnLib_ApplyConfig(long sender, ZPackage configPkg)
        {
            if (ZNet.instance.IsClientInstance())
            {
                if (configPkg != null && configPkg.Size() > 0 && sender == ZNet.instance.GetServerPeer().m_uid)
                {
                    ApplyConfigZPackage(configPkg);
                }
            }

            if (ZNet.instance.IsServerInstance())
            {
                // Is package not empty and is sender admin?
                if (configPkg != null && configPkg.Size() > 0 && ZNet.instance.m_adminList.Contains(ZNet.instance.GetPeer(sender)?.m_socket?.GetHostName()))
                {
                    // Send to all other clients
                    foreach (var peer in ZNet.instance.m_peers.Where(x => x.m_uid != sender))
                    {
                        ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, nameof(RPC_JotunnLib_ApplyConfig), configPkg);
                    }

                    // Apply config locally
                    ApplyConfigZPackage(configPkg);
                }
            }
        }

        /// <summary>
        ///     Determine Player's admin status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="isAdmin"></param>
        private static void RPC_JotunnLib_IsAdmin(long sender, bool isAdmin)
        {
            // Client Receive
            if (ZNet.instance.IsClientInstance())
            {
                Instance.PlayerIsAdmin = isAdmin;

                // If player is admin, unlock the configuration values
                if (isAdmin)
                {
                    UnlockConfigurationEntries();
                }
            }

            // Server Receive
            if (ZNet.instance.IsServerInstance())
            {
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    var result = ZNet.instance.m_adminList.Contains(peer.m_socket.GetHostName());
                    Logger.LogInfo($"Sending admin status to peer #{sender}: {(result ? "Admin" : "no admin")}");
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_JotunnLib_IsAdmin), result);

                    // Also sending server-only configuration values to client
                    RPC_JotunnLib_ConfigSync(sender, null);
                }
            }
        }

        /// <summary>
        ///     Unlock configuration entries
        /// </summary>
        private static void UnlockConfigurationEntries()
        {
            var loadedPlugins = GetDependentPlugins();

            foreach (var plugin in loadedPlugins)
            {
                foreach (var configDefinition in plugin.Value.Config.Keys)
                {
                    var configEntry = plugin.Value.Config[configDefinition.Section, configDefinition.Key];
                    var configAttribute = (ConfigurationManagerAttributes) configEntry.Description.Tags.FirstOrDefault(x =>
                        x is ConfigurationManagerAttributes {IsAdminOnly: true});
                    if (configAttribute != null)
                    {
                        configAttribute.UnlockSetting = true;
                    }
                }
            }
        }

        /// <summary>
        ///     Send initial configuration data to client (full set)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="configPkg"></param>
        public static void RPC_JotunnLib_ConfigSync(long sender, ZPackage configPkg)
        {
            if (ZNet.instance.IsClientInstance())
            {
                // Validate the message is from the server and not another client.
                if (configPkg != null && configPkg.Size() > 0 && sender == ZNet.instance.GetServerPeer().m_uid)
                {
                    Logger.LogDebug("Received configuration from server");
                    ApplyConfigZPackage(configPkg);
                }
            }

            if (ZNet.instance.IsServerInstance())
            {
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    Logger.LogMessage($"Sending configuration data to peer #{sender}");

                    var values = GetSyncConfigValues();

                    var pkg = GenerateConfigZPackage(values);

                    Logger.LogDebug($"Sending {values.Count} configuration values to client {sender}");
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_JotunnLib_ConfigSync), pkg);
                }
            }
        }

        /// <summary>
        ///     Apply received configuration values locally
        /// </summary>
        /// <param name="configPkg"></param>
        private static void ApplyConfigZPackage(ZPackage configPkg)
        {
            Logger.LogMessage("Received configuration data from server");

            var loadedPlugins = GetDependentPlugins();

            var numberOfEntries = configPkg.ReadInt();
            while (numberOfEntries > 0)
            {
                var modguid = configPkg.ReadString();
                var section = configPkg.ReadString();
                var key = configPkg.ReadString();
                var serializedValue = configPkg.ReadString();

                Logger.LogDebug($"Received {modguid} {section} {key} {serializedValue}");

                if (loadedPlugins.ContainsKey(modguid))
                {
                    if (loadedPlugins[modguid].Config.Keys.Contains(new ConfigDefinition(section, key)))
                    {
                        Logger.LogDebug($"Setting config value {modguid}.{section}.{key} to {serializedValue}");
                        loadedPlugins[modguid].Config[section, key].SetSerializedValue(serializedValue);
                    }
                    else
                    {
                        Logger.LogError($"Did not find Value for GUID: {modguid}, Section {section}, Key {key}");
                    }
                }
                else
                {
                    Logger.LogError($"No plugin with GUID {modguid} is loaded");
                }

                numberOfEntries--;
            }
        }

        /// <summary>
        ///     Generate ZPackage from configuration tuples
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private static ZPackage GenerateConfigZPackage(List<Tuple<string, string, string, string>> values)
        {
            var pkg = new ZPackage();
            var num = values.Count;
            pkg.Write(num);
            foreach (var entry in values)
            {
                Logger.LogDebug($"{entry.Item1} {entry.Item2} {entry.Item3} {entry.Item4}");
                pkg.Write(entry.Item1);
                pkg.Write(entry.Item2);
                pkg.Write(entry.Item3);
                pkg.Write(entry.Item4);
            }

            return pkg;
        }

        /// <summary>
        ///     Get syncable configuration values as tuples
        /// </summary>
        /// <returns></returns>
        internal static List<Tuple<string, string, string, string>> GetSyncConfigValues()
        {
            Logger.LogDebug("Gathering config values");
            var loadedPlugins = GetDependentPlugins();

            var values = new List<Tuple<string, string, string, string>>();
            foreach (var plugin in loadedPlugins)
            {
                foreach (var cd in plugin.Value.Config.Keys)
                {
                    var cx = plugin.Value.Config[cd.Section, cd.Key];
                    if (cx.Description.Tags.Any(x => x is ConfigurationManagerAttributes && ((ConfigurationManagerAttributes) x).IsAdminOnly))
                    {
                        var value = new Tuple<string, string, string, string>(plugin.Value.Info.Metadata.GUID, cd.Section, cd.Key, cx.GetSerializedValue());
                        values.Add(value);
                    }
                }
            }

            return values;
        }

        /// <summary>
        ///     Get a dictionary of loaded plugins which depend on JotunnLib
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, BaseUnityPlugin> GetDependentPlugins()
        {
            var result = new Dictionary<string, BaseUnityPlugin>();

            var plugins = FindObjectsOfType(typeof(BaseUnityPlugin)).Cast<BaseUnityPlugin>().ToArray();

            foreach (var plugin in plugins)
            {
                foreach (var attrib in plugin.GetType().GetCustomAttributes(typeof(BepInDependency), false).Cast<BepInDependency>())
                {
                    if (attrib.DependencyGUID == Main.ModGuid)
                    {
                        result.Add(plugin.Info.Metadata.GUID, plugin);
                    }
                }
            }

            return result;
        }
    }
}