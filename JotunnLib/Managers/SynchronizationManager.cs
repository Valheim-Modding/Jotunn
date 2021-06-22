using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jotunn.Managers
{
    /// <summary>
    ///    Manager for handling synchronisation between client and server instances.
    /// </summary>
    public class SynchronizationManager : IManager
    {
        private List<Tuple<string, string, string, string>> cachedConfigValues = new List<Tuple<string, string, string, string>>();

        private BaseUnityPlugin configurationManager;
        internal bool configurationManagerWindowShown;
        private static SynchronizationManager _instance;
        private static Dictionary<string, bool> lastAdminStates = new Dictionary<string, bool>();
        private double m_lastLoadCheckTime;

        /// <summary>
        ///     Event triggered after server configuration is applied to client
        /// </summary>
        public static event EventHandler<ConfigurationSynchronizationEventArgs> OnConfigurationSynchronized;

        /// <summary>
        ///     Singleton instance
        /// </summary>
        public static SynchronizationManager Instance
        {
            get
            {
                if (_instance == null) _instance = new SynchronizationManager();
                return _instance;
            }
        }

        /// <summary>
        ///     Clientside indicator if the current player has admin status on 
        ///     the current world, always true on local games
        /// </summary>
        public bool PlayerIsAdmin { get; private set; }


        /// <summary>
        ///     Manager's main init
        /// </summary>
        public void Init()
        {
            // Register RPCs in Game.Start
            On.Game.Start += Game_Start;

            // Hook RPC_PeerInfo for initial retrieval of admin status and configuration
            On.ZNet.RPC_PeerInfo += ZNet_RPC_PeerInfo;

            On.Menu.IsVisible += Menu_IsVisible;
            On.SyncedList.Load += SyncedList_Load;
            On.SyncedList.Save += SyncedList_Save;

            // Find Configuration manager plugin and add to DisplayingWindowChanged event
            if (!configurationManager)
            {
                HookConfigurationManager();
            }
        }

        /// <summary>
        ///     Timer method for refreshing the ZNet admin list, polls the list every 10 seconds
        /// </summary>
        internal void AdminListUpdate()
        {
            if (Time.realtimeSinceStartup - this.m_lastLoadCheckTime >= 10.0f)
            {
                ZNet.instance.m_adminList.GetList();
                m_lastLoadCheckTime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        ///     Hook <see cref="Game.Start"/> to register RPCs
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void Game_Start(On.Game.orig_Start orig, Game self)
        {
            orig(self);
            ZRoutedRpc.instance.Register(nameof(RPC_Jotunn_IsAdmin), new Action<long, bool>(RPC_Jotunn_IsAdmin));
            ZRoutedRpc.instance.Register(nameof(RPC_Jotunn_ConfigSync), new Action<long, ZPackage>(RPC_Jotunn_ConfigSync));
            ZRoutedRpc.instance.Register(nameof(RPC_Jotunn_ApplyConfig), new Action<long, ZPackage>(RPC_Jotunn_ApplyConfig));

            if (ZNet.instance != null && ZNet.instance.IsLocalInstance())
            {
                Logger.LogDebug("Player is in local instance, lets make him admin");
                Instance.PlayerIsAdmin = true;
                UnlockConfigurationEntries();
            }

            // Add event to be notified on logout
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        /// <summary>
        ///     Reset configuration unlock state
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="loadMode"></param>
        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name == "start")
            {
                PlayerIsAdmin = false;
                LockConfigurationEntries();
            }

            // Remove from handler
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
        }

        /// <summary>
        ///     Hook ZNet.RPC_PeerInfo on client to query the server for admin status
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
                ZRoutedRpc.instance.InvokeRoutedRPC(ZNet.instance.GetServerPeer().m_uid, nameof(RPC_Jotunn_IsAdmin), false);
            }
        }

        /// <summary>
        ///     Hook <see cref="SyncedList.Save"/> to synchronize the admin status to the clients
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void SyncedList_Save(On.SyncedList.orig_Save orig, SyncedList self)
        {
            orig(self);

            // Check if it really is the admin list
            if (self == ZNet.instance.m_adminList)
            {
                SynchronizeAdminStatus();
            }
        }

        /// <summary>
        ///     Hook <see cref="SyncedList.Load"/> to synchronize the admin status to the clients
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void SyncedList_Load(On.SyncedList.orig_Load orig, SyncedList self)
        {
            orig(self);

            // Check if it really is the admin list
            if (self == ZNet.instance.m_adminList)
            {
                SynchronizeAdminStatus();
            }
        }

        /// <summary>
        ///     Checks the ZNet.m_instance.m_adminList against the cached list and send any
        ///     changes to the corresponding clients.
        /// </summary>
        private void SynchronizeAdminStatus()
        {
            if (ZNet.instance == null)
            {
                return;
            }
            
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                List<string> adminListCopy = ZNet.instance.m_adminList.m_list.ToList();
                foreach (var entry in adminListCopy)
                {
                    // Admin state added, but not in cache list yet
                    if (!lastAdminStates.ContainsKey(entry))
                    {
                        // Send RPC, new entry found
                        SendAdminStateToClient(entry, true);

                        lastAdminStates.Add(entry, true);
                    }
                    // Admin state added and already in cache list
                    else
                    {
                        if (lastAdminStates[entry] == false)
                        {
                            // Send RPC, new entry found
                            SendAdminStateToClient(entry, true);
                        }
                    }
                }

                foreach (var entry in lastAdminStates.Keys.ToList())
                {
                    // Admin state removed
                    if (!adminListCopy.Contains(entry))
                    {
                        // If cached state is true
                        if (lastAdminStates[entry])
                        {
                            // Send RPC, new entry found
                            SendAdminStateToClient(entry, false);
                        }

                        lastAdminStates.Remove(entry);
                    }
                }
            }
        }

        /// <summary>
        ///     Sends the current admin state of a player on a server to the client
        /// </summary>
        /// <param name="entry">Socket host name of the peer</param>
        /// <param name="admin">Admin state to send to the client</param>
        private void SendAdminStateToClient(string entry, bool admin)
        {
            var clientId = ZNet.instance.m_peers.FirstOrDefault(x => x.m_socket.GetHostName() == entry)?.m_uid;
            if (clientId != null)
            {
                Logger.LogInfo($"Sending admin status to {entry}/{clientId} ({(admin ? "is admin" : "is no admin")})");
                ZRoutedRpc.instance.InvokeRoutedRPC((long)clientId, nameof(RPC_Jotunn_IsAdmin), admin);
            }
        }

        /// <summary>
        ///     Hook ConfigurationManager's DisplayingWindowChanged to be able to react on window open/close.
        /// </summary>
        private void HookConfigurationManager()
        {
            Logger.LogDebug("Trying to hook config manager");

            var result = new Dictionary<string, BaseUnityPlugin>();
            configurationManager = GameObject.FindObjectsOfType(typeof(BaseUnityPlugin)).Cast<BaseUnityPlugin>().ToArray()
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
        ///     Hook <see cref="Menu.IsVisible"/> to unlock cursor properly and disable camera rotation
        /// </summary>
        /// <param name="orig"></param>
        /// <returns></returns>
        private bool Menu_IsVisible(On.Menu.orig_IsVisible orig)
        {
            return orig() | configurationManagerWindowShown;
        }

        /// <summary>
        ///     Window display state changed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigurationManager_DisplayingWindowChanged(object sender, object e)
        {
            // Read configuration manager's DisplayingWindow property
            var pi = configurationManager.GetType().GetProperty("DisplayingWindow");
            configurationManagerWindowShown = (bool)pi.GetValue(configurationManager, null);

            // Did window open or close?
            if (configurationManagerWindowShown)
            {
                // If window just opened, cache the config values for comparison later
                CacheConfigurationValues();
            }
            else
            {
                SynchronizeChangedConfig();
            }
        }

        /// <summary>
        ///     Cache the synchronizable configuration values
        /// </summary>
        internal void CacheConfigurationValues()
        {
            cachedConfigValues = GetSyncConfigValues();
        }

        /// <summary>
        ///     Syncs the changed configuration of a client to the server
        /// </summary>
        internal void SynchronizeChangedConfig()
        {
            // Lets compare and send to server, if applicable
            var loadedPlugins = BepInExUtils.GetDependentPlugins();

            var valuesToSend = new List<Tuple<string, string, string, string>>();
            foreach (var plugin in loadedPlugins)
            {
                foreach (var cd in plugin.Value.Config.Keys)
                {
                    var cx = plugin.Value.Config[cd.Section, cd.Key];
                    string buttonName = cx.GetBoundButtonName();
                    if (cx.Description.Tags.Any(x =>
                        x is ConfigurationManagerAttributes && ((ConfigurationManagerAttributes)x).IsAdminOnly &&
                        ((ConfigurationManagerAttributes)x).UnlockSetting))
                    {
                        var value = new Tuple<string, string, string, string>(plugin.Value.Info.Metadata.GUID, cd.Section, cd.Key, cx.GetSerializedValue());
                        valuesToSend.Add(value);
                    }

                    if (cx.SettingType == typeof(KeyCode))
                    {
                        ZInput.instance.Setbutton($"{buttonName}!{plugin.Value.Info.Metadata.GUID}", (KeyCode)cx.BoxedValue);
                    }
                }
            }

            // We need only changed values
            valuesToSend = valuesToSend.Where(x => !cachedConfigValues.Contains(x)).ToList();

            // Send to server
            if (valuesToSend.Count > 0)
            {
                var zPackage = GenerateConfigZPackage(valuesToSend);

                // Send values to server if it is a client instance
                if (ZNet.instance.IsClientInstance())
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZNet.instance.GetServerPeer().m_uid, nameof(RPC_Jotunn_ApplyConfig), zPackage);

                    // Also fire event that admin config was changed locally, since the RPC does not come back to the sender
                    OnConfigurationSynchronized.SafeInvoke(this, new ConfigurationSynchronizationEventArgs() { InitialSynchronization = false });

                }
                // If it is a local instance, send it to all connected peers
                if (ZNet.instance.IsLocalInstance())
                {
                    foreach (var peer in ZNet.instance.m_peers)
                    {
                        ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, nameof(RPC_Jotunn_ApplyConfig), zPackage);
                    }
                }
            }
        }

        /// <summary>
        ///     Apply a partial config to server and send to other clients.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="configPkg"></param>
        internal void RPC_Jotunn_ApplyConfig(long sender, ZPackage configPkg)
        {
            if (ZNet.instance.IsClientInstance())
            {
                if (configPkg != null && configPkg.Size() > 0 && sender == ZNet.instance.GetServerPeer().m_uid)
                {
                    Logger.LogInfo("Received configuration data from server");
                    ApplyConfigZPackage(configPkg);

                    OnConfigurationSynchronized.SafeInvoke(this, new ConfigurationSynchronizationEventArgs() { InitialSynchronization = false });
                }
            }

            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                // Is package not empty and is sender admin?
                if (configPkg != null && configPkg.Size() > 0 && ZNet.instance.m_adminList.Contains(ZNet.instance.GetPeer(sender)?.m_socket?.GetHostName()))
                {
                    Logger.LogInfo($"Received configuration data from client {sender}");

                    // Send to all other clients
                    foreach (var peer in ZNet.instance.m_peers.Where(x => x.m_uid != sender))
                    {
                        ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, nameof(RPC_Jotunn_ApplyConfig), configPkg);
                    }

                    // Apply config locally
                    ApplyConfigZPackage(configPkg);
                }
            }
        }

        /// <summary>
        ///     Determine Player's admin status.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="isAdmin"></param>
        internal void RPC_Jotunn_IsAdmin(long sender, bool isAdmin)
        {
            // Client Receive
            if (ZNet.instance.IsClientInstance())
            {
                Logger.LogInfo($"Received admin status from server: {(isAdmin ? "Admin" : "no admin")}");

                Instance.PlayerIsAdmin = isAdmin;

                // If player is admin, unlock the configuration values
                if (isAdmin)
                {
                    UnlockConfigurationEntries();
                }
                else
                {
                    LockConfigurationEntries();
                }
            }

            // Server Receive
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    var result = ZNet.instance.m_adminList.Contains(peer.m_socket.GetHostName());
                    Logger.LogInfo($"Sending admin status to peer #{sender}: {(result ? "Admin" : "no admin")}");
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_Jotunn_IsAdmin), result);

                    // Also sending server-only configuration values to client
                    RPC_Jotunn_ConfigSync(sender, null);
                }
            }
        }

        /// <summary>
        ///     Unlock configuration entries.
        /// </summary>
        private void UnlockConfigurationEntries()
        {
            var loadedPlugins = BepInExUtils.GetDependentPlugins();

            foreach (var plugin in loadedPlugins)
            {
                foreach (var configDefinition in plugin.Value.Config.Keys)
                {
                    var configEntry = plugin.Value.Config[configDefinition.Section, configDefinition.Key];
                    var configAttribute = (ConfigurationManagerAttributes)configEntry.Description.Tags.FirstOrDefault(x =>
                       x is ConfigurationManagerAttributes { IsAdminOnly: true });
                    if (configAttribute != null)
                    {
                        configAttribute.UnlockSetting = true;
                    }
                }
            }
        }

        /// <summary>
        ///     Lock configuration entries (on logout).
        /// </summary>
        private void LockConfigurationEntries()
        {
            var loadedPlugins = BepInExUtils.GetDependentPlugins();

            foreach (var plugin in loadedPlugins)
            {
                foreach (var configDefinition in plugin.Value.Config.Keys)
                {
                    var configEntry = plugin.Value.Config[configDefinition.Section, configDefinition.Key];
                    var configAttribute = (ConfigurationManagerAttributes)configEntry.Description.Tags.FirstOrDefault(x =>
                       x is ConfigurationManagerAttributes { IsAdminOnly: true });
                    if (configAttribute != null)
                    {
                        configAttribute.IsAdminOnly = true;
                    }
                }
            }
        }

        /// <summary>
        ///     Send initial configuration data to client (full set).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="configPkg"></param>
        internal void RPC_Jotunn_ConfigSync(long sender, ZPackage configPkg)
        {
            if (ZNet.instance.IsClientInstance())
            {
                // Validate the message is from the server and not another client.
                if (configPkg != null && configPkg.Size() > 0 && sender == ZNet.instance.GetServerPeer().m_uid)
                {
                    Logger.LogInfo("Received configuration data from server");
                    ApplyConfigZPackage(configPkg);

                    OnConfigurationSynchronized.SafeInvoke(this, new ConfigurationSynchronizationEventArgs() { InitialSynchronization = true });
                }
            }

            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                var peer = ZNet.instance.m_peers.FirstOrDefault(x => x.m_uid == sender);
                if (peer != null)
                {
                    Logger.LogInfo($"Sending configuration data to peer #{sender}");

                    var values = GetSyncConfigValues();

                    var pkg = GenerateConfigZPackage(values);

                    Logger.LogDebug($"Sending {values.Count} configuration values to client {sender}");
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, nameof(RPC_Jotunn_ConfigSync), pkg);
                }
            }
        }

        /// <summary>
        ///     Apply received configuration values locally
        /// </summary>
        /// <param name="configPkg"></param>
        internal void ApplyConfigZPackage(ZPackage configPkg)
        {
            Logger.LogDebug("Applying configuration data package");

            var loadedPlugins = BepInExUtils.GetDependentPlugins();

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
        private ZPackage GenerateConfigZPackage(List<Tuple<string, string, string, string>> values)
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
        private List<Tuple<string, string, string, string>> GetSyncConfigValues()
        {
            Logger.LogDebug("Gathering config values");
            var loadedPlugins = BepInExUtils.GetDependentPlugins();

            var values = new List<Tuple<string, string, string, string>>();
            foreach (var plugin in loadedPlugins)
            {
                foreach (var cd in plugin.Value.Config.Keys)
                {
                    var cx = plugin.Value.Config[cd.Section, cd.Key];
                    if (cx.Description.Tags.Any(x => x is ConfigurationManagerAttributes && ((ConfigurationManagerAttributes)x).IsAdminOnly))
                    {
                        var value = new Tuple<string, string, string, string>(plugin.Value.Info.Metadata.GUID, cd.Section, cd.Key, cx.GetSerializedValue());
                        values.Add(value);
                    }
                }
            }

            return values;
        }
    }
}
