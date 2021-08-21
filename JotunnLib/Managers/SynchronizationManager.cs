using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    /// <summary>
    ///    Manager for handling synchronisation between client and server instances.
    /// </summary>
    public class SynchronizationManager : IManager
    {
        private readonly Dictionary<string, bool> CachedAdminStates = new Dictionary<string, bool>();
        private List<Tuple<string, string, string, string>> CachedConfigValues = new List<Tuple<string, string, string, string>>();
        private BaseUnityPlugin ConfigurationManager;
        private bool ConfigurationManagerWindowShown;

        /// <summary>
        ///     Event triggered after server configuration is applied to client
        /// </summary>
        public static event EventHandler<ConfigurationSynchronizationEventArgs> OnConfigurationSynchronized;

        /// <summary>
        ///     Event triggered after a clients admin status changed on the server
        /// </summary>
        public static event Action OnAdminStatusChanged;

        private static SynchronizationManager _instance;
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
            // Register RPCs and the admin watchdog
            On.Game.Start += Game_Start;
            On.ZNet.Awake += ZNet_Awake;
            On.ZNet.OnNewConnection += ZNet_OnNewConnection;

            // Hook RPC_PeerInfo for initial retrieval of admin status and configuration
            On.ZNet.RPC_PeerInfo += ZNet_RPC_PeerInfo;
            
            // Hook SyncedList for admin list changes
            On.SyncedList.Load += SyncedList_Load;
            On.SyncedList.Save += SyncedList_Save;

            // Hook menu for ConfigManager integration
            On.Menu.IsVisible += Menu_IsVisible;

            // Hook Fejd for ConfigReloaded event subscription
            On.FejdStartup.Awake += FejdStartup_Awake;

            // Hook start scene to reset config
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            // Find Configuration manager plugin and add to DisplayingWindowChanged event
            if (!ConfigurationManager)
            {
                Logger.LogDebug("Trying to hook config manager");
                
                ConfigurationManager = Object.FindObjectsOfType(typeof(BaseUnityPlugin)).Cast<BaseUnityPlugin>().ToArray()
                    .FirstOrDefault(x => x.Info?.Metadata?.GUID == "com.bepis.bepinex.configurationmanager");

                if (ConfigurationManager)
                {
                    Logger.LogDebug("Configuration manager found, trying to hook DisplayingWindowChanged");
                    var eventinfo = ConfigurationManager.GetType().GetEvent("DisplayingWindowChanged");
                    if (eventinfo != null)
                    {
                        Action<object, object> local = ConfigurationManager_DisplayingWindowChanged;
                        var converted = Delegate.CreateDelegate(eventinfo.EventHandlerType, local.Target, local.Method);

                        eventinfo.AddEventHandler(ConfigurationManager, converted);
                    }
                }
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
            ZRoutedRpc.instance.Register(nameof(RPC_Jotunn_SyncConfig), new Action<long, ZPackage>(RPC_Jotunn_SyncConfig));
        }

        /// <summary>
        ///     Init or reset admin and configuration state
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="loadMode"></param>
        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name == "start")
            {
                PlayerIsAdmin = true;
                UnlockConfigurationEntries();
                ResetAdminConfigs();
                CacheConfigurationValues();
            }
        }

        /// <summary>
        ///     Hook <see cref="ZNet.Awake"/> to start a watchdog Coroutine which monitors the admin list.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void ZNet_Awake(On.ZNet.orig_Awake orig, ZNet self)
        {
            orig(self);

            if (self.IsServer())
            {
                IEnumerator watchdog()
                {
                    while (true)
                    {
                        yield return new WaitForSeconds(10);
                        self.m_adminList?.GetList();
                    }
                }
                self.StartCoroutine(watchdog());
            }
        }

        /// <summary>
        ///     Hook <see cref="ZNet.OnNewConnection"/> to register client side RPC calls for synchronization.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="peer"></param>
        private void ZNet_OnNewConnection(On.ZNet.orig_OnNewConnection orig, ZNet self, ZNetPeer peer)
        {
            orig(self, peer);
            if (!self.IsServer())
            {
                peer.m_rpc.Register<bool>(nameof(RPC_Jotunn_IsAdmin), RPC_Jotunn_InitialAdmin);
                peer.m_rpc.Register<ZPackage>(nameof(RPC_Jotunn_SyncConfig), RPC_Jotunn_SyncInitialConfig);
            }
        }

        /// <summary>
        ///     Hook ZNet.RPC_PeerInfo on the server to send initial data
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="rpc"></param>
        /// <param name="pkg"></param>
        private void ZNet_RPC_PeerInfo(On.ZNet.orig_RPC_PeerInfo orig, ZNet self, ZRpc rpc, ZPackage pkg)
        {
            PeerInfoBlockingSocket bufferingSocket = null;

            // Create buffering socket
            if (self.IsServer())
            {
                bufferingSocket = new PeerInfoBlockingSocket(rpc.GetSocket());
                rpc.m_socket = bufferingSocket;
            }

            orig(self, rpc, pkg);

            // Send initial data
            if (self.IsServer())
            {
                ZNetPeer peer = self.GetPeer(rpc);
                Logger.LogInfo($"Sending initial data to peer #{peer.m_uid}");

                IEnumerator SynchronizeInitialData()
                {
                    var result = ZNet.instance.m_adminList.Contains(peer.m_socket.GetHostName());
                    Logger.LogDebug($"Admin status: {(result ? "Admin" : "No Admin")}");
                    peer.m_rpc.Invoke(nameof(RPC_Jotunn_IsAdmin), result);
                    yield return null;

                    ZPackage pkg = GenerateConfigZPackage(true, GetSyncConfigValues());
                    yield return ZNet.instance.StartCoroutine(SendPackage(peer.m_uid, pkg));

                    if (peer.m_rpc.GetSocket() is PeerInfoBlockingSocket currentSocket)
                    {
                        peer.m_rpc.m_socket = currentSocket.Original;
                    }

                    bufferingSocket.finished = true;

                    foreach (ZPackage package in bufferingSocket.Package)
                    {
                        bufferingSocket.Original.Send(package);
                    }
                }
                self.StartCoroutine(SynchronizeInitialData());
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
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                List<string> adminListCopy = ZNet.instance.m_adminList.m_list.ToList();
                foreach (var entry in adminListCopy)
                {
                    // Admin state added, but not in cache list yet
                    if (!CachedAdminStates.ContainsKey(entry))
                    {
                        // Send RPC, new entry found
                        SendAdminStateToClient(entry, true);

                        CachedAdminStates.Add(entry, true);
                    }
                    // Admin state added and already in cache list
                    else
                    {
                        if (CachedAdminStates[entry] == false)
                        {
                            // Send RPC, new entry found
                            SendAdminStateToClient(entry, true);
                        }
                    }
                }

                foreach (var entry in CachedAdminStates.Keys.ToList())
                {
                    // Admin state removed
                    if (!adminListCopy.Contains(entry))
                    {
                        // If cached state is true
                        if (CachedAdminStates[entry])
                        {
                            // Send RPC, new entry found
                            SendAdminStateToClient(entry, false);
                        }

                        CachedAdminStates.Remove(entry);
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

        private void RPC_Jotunn_InitialAdmin(ZRpc rpc, bool isAdmin) => RPC_Jotunn_IsAdmin(0, isAdmin);

        /// <summary>
        ///     Determine Player's admin status.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="isAdmin"></param>
        private void RPC_Jotunn_IsAdmin(long sender, bool isAdmin)
        {
            // Client Receive
            if (ZNet.instance.IsClientInstance())
            {
                Logger.LogInfo($"Received admin status from server: {(isAdmin ? "Admin" : "No Admin")}");

                Instance.PlayerIsAdmin = isAdmin;
                InvokeOnAdminStatusChanged();

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
        }

        /// <summary>
        ///     Safely invoke the <see cref="OnAdminStatusChanged"/> event
        /// </summary>
        private void InvokeOnAdminStatusChanged()
        {
            OnAdminStatusChanged?.SafeInvoke();
        }

        /// <summary>
        ///     Unlock configuration entries.
        /// </summary>
        private void UnlockConfigurationEntries()
        {
            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);

            foreach (var plugin in loadedPlugins)
            {
                foreach (var configDefinition in plugin.Value.Config.Keys)
                {
                    var configEntry = plugin.Value.Config[configDefinition.Section, configDefinition.Key];
                    var configAttribute = (ConfigurationManagerAttributes)configEntry.Description.Tags
                        .FirstOrDefault(x => x is ConfigurationManagerAttributes { IsAdminOnly: true });
                    if (configAttribute != null)
                    {
                        configAttribute.IsUnlocked = true;
                    }
                }
            }
        }

        /// <summary>
        ///     Lock configuration entries.
        /// </summary>
        private void LockConfigurationEntries()
        {
            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);

            foreach (var plugin in loadedPlugins)
            {
                foreach (var configDefinition in plugin.Value.Config.Keys)
                {
                    var configEntry = plugin.Value.Config[configDefinition.Section, configDefinition.Key];
                    var configAttribute = (ConfigurationManagerAttributes)configEntry.Description.Tags
                        .FirstOrDefault(x => x is ConfigurationManagerAttributes { IsAdminOnly: true });
                    if (configAttribute != null)
                    {
                        configAttribute.IsUnlocked = false;
                    }
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
            return orig() | ConfigurationManagerWindowShown;
        }

        /// <summary>
        ///     Window display state changed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigurationManager_DisplayingWindowChanged(object sender, object e)
        {
            // Read configuration manager's DisplayingWindow property
            var pi = ConfigurationManager.GetType().GetProperty("DisplayingWindow");
            ConfigurationManagerWindowShown = (bool)pi.GetValue(ConfigurationManager, null);

            if (!ConfigurationManagerWindowShown)
            {
                // After closing the window check for changed configs
                SynchronizeChangedConfig();
            }
        }

        /// <summary>
        ///     Initial cache the config values of dependent plugins and register ourself to config change events
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void FejdStartup_Awake(On.FejdStartup.orig_Awake orig, FejdStartup self)
        {
            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);
            foreach (var config in loadedPlugins.Where(x => x.Value.Config != null).Select(x => x.Value.Config))
            {
                config.ConfigReloaded += Config_ConfigReloaded;
            }

            // Harmony patch BepInEx to ensure locked values are not overwritten
            Harmony.CreateAndPatchAll(typeof(ConfigEntryBase_SetSerializedValue));
            Harmony.CreateAndPatchAll(typeof(ConfigEntryBase_GetSerializedValue));

            orig(self);
        }

        /// <summary>
        ///     Sync the local bep config on reload
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Config_ConfigReloaded(object sender, EventArgs e)
        {
            SynchronizeChangedConfig();
        }

        /// <summary>
        ///     Return the cached local value of a bep config thats locked
        /// </summary>
        [HarmonyPatch(typeof(ConfigEntryBase), "GetSerializedValue")]
        private static class ConfigEntryBase_GetSerializedValue
        {
            private static bool Prefix(ConfigEntryBase __instance, ref string __result)
            {
                if (!__instance.IsSyncable() || GUIManager.IsHeadless() || __instance.GetLocalValue() == null)
                {
                    return true;
                }

                __result = TomlTypeConverter.ConvertToString(__instance.GetLocalValue(), __instance.SettingType);
                return false;
            }
        }

        /// <summary>
        ///     Prevent overwriting bep config value when the setting is locked on config file reload
        /// </summary>
        [HarmonyPatch(typeof(ConfigEntryBase), "SetSerializedValue")]
        private static class ConfigEntryBase_SetSerializedValue
        {
            private static bool Prefix(ConfigEntryBase __instance, string value)
            {
                if (GUIManager.IsHeadless() || __instance.GetLocalValue() == null)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        ///     Cache the synchronizable configuration values for comparison
        /// </summary>
        internal void CacheConfigurationValues()
        {
            CachedConfigValues = GetSyncConfigValues();
        }

        /// <summary>
        ///     Get syncable configuration values as tuples
        /// </summary>
        /// <returns></returns>
        private List<Tuple<string, string, string, string>> GetSyncConfigValues()
        {
            Logger.LogDebug("Gathering config values");
            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);

            var values = new List<Tuple<string, string, string, string>>();
            foreach (var plugin in loadedPlugins)
            {
                foreach (var cd in plugin.Value.Config.Keys)
                {
                    var cx = plugin.Value.Config[cd.Section, cd.Key];
                    if (cx.Description.Tags.Any(x => x is ConfigurationManagerAttributes && ((ConfigurationManagerAttributes)x).IsAdminOnly))
                    {
                        var value = new Tuple<string, string, string, string>(
                            plugin.Value.Info.Metadata.GUID, cd.Section, cd.Key, TomlTypeConverter.ConvertToString(cx.BoxedValue, cx.SettingType));
                        values.Add(value);
                    }
                }
            }

            return values;
        }

        /// <summary>
        ///     Syncs the changed configuration of a client to the server
        /// </summary>
        internal void SynchronizeChangedConfig()
        {
            // Lets compare and send to server, if applicable
            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);

            var valuesToSend = new List<Tuple<string, string, string, string>>();
            foreach (var plugin in loadedPlugins)
            {
                foreach (var cd in plugin.Value.Config.Keys)
                {
                    var cx = plugin.Value.Config[cd.Section, cd.Key];
                    if (cx.Description.Tags.Any(x =>
                        x is ConfigurationManagerAttributes && ((ConfigurationManagerAttributes)x).IsAdminOnly &&
                        ((ConfigurationManagerAttributes)x).IsUnlocked))
                    {
                        var value = new Tuple<string, string, string, string>(
                            plugin.Value.Info.Metadata.GUID, cd.Section, cd.Key, TomlTypeConverter.ConvertToString(cx.BoxedValue, cx.SettingType));
                        valuesToSend.Add(value);
                    }

                    string buttonName = cx.GetBoundButtonName();
                    if (cx.SettingType == typeof(KeyCode) && ZInput.instance.m_buttons.ContainsKey(buttonName))
                    {
                        ZInput.instance.Setbutton(buttonName, (KeyCode)cx.BoxedValue);
                    }
                }
            }

            // We need only changed values
            valuesToSend = valuesToSend.Where(x => !CachedConfigValues.Contains(x)).ToList();

            if (valuesToSend.Count > 0)
            {
                // Send if connected
                if (ZNet.instance != null)
                {
                    ZPackage package = GenerateConfigZPackage(false, valuesToSend);

                    // Send values to server if it is a client instance
                    if (ZNet.instance.IsClientInstance())
                    {
                        ZRoutedRpc.instance.InvokeRoutedRPC(nameof(RPC_Jotunn_SyncConfig), package);

                        // Also fire event that admin config was changed locally, since the RPC does not come back to the sender
                        InvokeOnConfigurationSynchronized(false);
                    }
                    // Send changed values to all connected clients
                    else
                    {
                        ZNet.instance.StartCoroutine(SendPackage(ZNet.instance.m_peers, package));
                    }
                }
                
                // Rebuild config cache
                CacheConfigurationValues();
            }
        }

        /// <summary>
        ///     Cache local config values for synced entries
        /// </summary>
        private void InitAdminConfigs()
        {
            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);

            foreach (var plugin in loadedPlugins)
            {
                foreach (var configDefinition in plugin.Value.Config.Keys)
                {
                    var configEntry = plugin.Value.Config[configDefinition.Section, configDefinition.Key];
                    var configAttribute = (ConfigurationManagerAttributes)configEntry.Description.Tags
                        .FirstOrDefault(x => x is ConfigurationManagerAttributes { IsAdminOnly: true });
                    if (configAttribute != null && configEntry.BoxedValue != null)
                    {
                        configAttribute.LocalValue = configEntry.BoxedValue;
                    }
                }
            }
        }

        /// <summary>
        ///     Reset configs which may have been overwritten with server values to the local value
        /// </summary>
        private void ResetAdminConfigs() 
        {
            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);

            foreach (var plugin in loadedPlugins)
            {
                foreach (var configDefinition in plugin.Value.Config.Keys)
                {
                    var configEntry = plugin.Value.Config[configDefinition.Section, configDefinition.Key];
                    var configAttribute = (ConfigurationManagerAttributes)configEntry.Description.Tags
                        .FirstOrDefault(x => x is ConfigurationManagerAttributes { IsAdminOnly: true });
                    if (configAttribute != null && configAttribute.LocalValue != null)
                    {
                        configEntry.BoxedValue = configAttribute.LocalValue;
                        configAttribute.LocalValue = null;
                    }
                }
            }
        }

        private const byte INITIAL_CONFIG = 1;
        private const byte FRAGMENTED_CONFIG = 2;
        private const byte COMPRESSED_CONFIG = 4;

        private readonly Dictionary<string, SortedDictionary<int, byte[]>> configValueCache = new Dictionary<string, SortedDictionary<int, byte[]>>();
        private readonly List<KeyValuePair<long, string>> cacheExpirations = new List<KeyValuePair<long, string>>(); // avoid leaking memory

        /// <summary>
        ///     Client side RPC to receive the initial config from the server
        /// </summary>
        /// <param name="rpc"></param>
        /// <param name="package"></param>
        private void RPC_Jotunn_SyncInitialConfig(ZRpc rpc, ZPackage package) => RPC_Jotunn_SyncConfig(0, package);

        /// <summary>
        ///     Receive and apply a partial config and send to other clients when the server receives changed config.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="package"></param>
        private void RPC_Jotunn_SyncConfig(long sender, ZPackage package)
        {
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                // Is package not empty and is sender admin?
                if (package != null && package.Size() > 0 && ZNet.instance.m_adminList.Contains(ZNet.instance.GetPeer(sender)?.m_socket?.GetHostName()))
                {
                    Logger.LogInfo($"Received configuration data from client {sender}");

                    // Apply config locally
                    ApplyConfigZPackage(package, out bool initial);

                    // Send to all other clients
                    ZNet.instance.StartCoroutine(SendPackage(ZNet.instance.m_peers.Where(x => x.m_uid != sender).ToList(), package));
                }
            }

            if (ZNet.instance.IsClientInstance())
            {
                if (package != null && package.Size() > 0)
                {
                    Logger.LogDebug("Received configuration data from server");
                    try
                    {
                        cacheExpirations.RemoveAll(kv =>
                        {
                            if (kv.Key < DateTimeOffset.Now.Ticks)
                            {
                                configValueCache.Remove(kv.Value);
                                return true;
                            }

                            return false;
                        });

                        byte packageFlags = package.ReadByte();

                        if ((packageFlags & FRAGMENTED_CONFIG) != 0)
                        {
                            long uniqueIdentifier = package.ReadLong();
                            string cacheKey = sender.ToString() + uniqueIdentifier;
                            if (!configValueCache.TryGetValue(cacheKey, out SortedDictionary<int, byte[]> dataFragments))
                            {
                                dataFragments = new SortedDictionary<int, byte[]>();
                                configValueCache[cacheKey] = dataFragments;
                                cacheExpirations.Add(new KeyValuePair<long, string>(DateTimeOffset.Now.AddSeconds(60).Ticks, cacheKey));
                            }

                            int fragment = package.ReadInt();
                            int fragments = package.ReadInt();

                            dataFragments.Add(fragment, package.ReadByteArray());

                            if (dataFragments.Count < fragments)
                            {
                                return;
                            }

                            configValueCache.Remove(cacheKey);

                            package = new ZPackage(dataFragments.Values.SelectMany(a => a).ToArray());
                            packageFlags = package.ReadByte();
                        }

                        //ProcessingServerUpdate = true;

                        if ((packageFlags & COMPRESSED_CONFIG) != 0)
                        {
                            byte[] data = package.ReadByteArray();

                            MemoryStream input = new MemoryStream(data);
                            MemoryStream output = new MemoryStream();
                            using (DeflateStream deflateStream = new DeflateStream(input, CompressionMode.Decompress))
                            {
                                deflateStream.CopyTo(output);
                            }

                            package = new ZPackage(output.ToArray());
                            packageFlags = package.ReadByte();
                        }

                        if ((packageFlags & INITIAL_CONFIG) != 0)
                        {
                            InitAdminConfigs();
                        }

                        package.SetPos(0);
                        ApplyConfigZPackage(package, out bool initial);
                        InvokeOnConfigurationSynchronized(initial);
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Error caught while applying config package: {e}");
                    }
                    finally
                    {
                        //ProcessingServerUpdate = false;
                    }
                }
            }
        }

        /// <summary>
        ///     Safely invoke the <see cref="OnConfigurationSynchronized"/> event
        /// </summary>
        private void InvokeOnConfigurationSynchronized(bool initial)
        {
            OnConfigurationSynchronized?.SafeInvoke(this, new ConfigurationSynchronizationEventArgs() { InitialSynchronization = initial });
        }

        /// <summary>
        ///     Apply received configuration values locally and regenerate the cache
        /// </summary>
        /// <param name="configPkg">Package of config tuples</param>
        /// <param name="initial">Indicator if this was an initial config package</param>
        private void ApplyConfigZPackage(ZPackage configPkg, out bool initial)
        {
            initial = (configPkg.ReadByte() & INITIAL_CONFIG) != 0;

            Logger.LogDebug($"Applying{(initial ? " initial" : null)} configuration data package");

            var numberOfEntries = configPkg.ReadInt();
            if (numberOfEntries == 0)
            {
                return;
            }

            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);
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
                        var entry = loadedPlugins[modguid].Config[section, key];
                        if (entry.IsSyncable())
                        {
                            Logger.LogDebug($"Setting config value {modguid}.{section}.{key} to {serializedValue}");
                            //loadedPlugins[modguid].Config[section, key].SetSerializedValue(serializedValue);
                            entry.BoxedValue = TomlTypeConverter.ConvertToValue(serializedValue, entry.SettingType);
                        }
                        else
                        {
                            Logger.LogError($"Setting for GUID: {modguid}, Section {section}, Key {key} is not syncable");
                        }
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

            // Recreate config cache
            CacheConfigurationValues();
        }

        /// <summary>
        ///     Generate ZPackage from configuration tuples
        /// </summary>
        /// <param name="initial">Indicator if this is the initial config package</param>
        /// <param name="values">List of config tuples to include in the package</param>
        /// <returns></returns>
        private ZPackage GenerateConfigZPackage(bool initial, List<Tuple<string, string, string, string>> values)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(initial ? INITIAL_CONFIG : (byte)0);
            int num = values.Count;
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
        ///     Coroutine to send a package async to a single target. Compresses the package if necessary.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        private IEnumerator SendPackage(long target, ZPackage package)
        {
            if (!ZNet.instance)
            {
                return Enumerable.Empty<object>().GetEnumerator();
            }

            List<ZNetPeer> peers = ZRoutedRpc.instance.m_peers;
            if (target != ZRoutedRpc.Everybody)
            {
                peers = peers.Where(p => p.m_uid == target).ToList();
            }

            return SendPackage(peers, package);
        }

        /// <summary>
        ///     Coroutine to send a package async to a list of Peers. Compresses the package if necessary.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        private IEnumerator SendPackage(List<ZNetPeer> peers, ZPackage package)
        {
            if (!ZNet.instance)
            {
                yield break;
            }

            const int compressMinSize = 10000;

            if (package.Size() > compressMinSize)
            {
                byte[] rawData = package.GetArray();
                Logger.LogDebug($"Compressing package with length {rawData.Length}");

                ZPackage compressedPackage = new ZPackage();
                compressedPackage.Write(COMPRESSED_CONFIG);
                MemoryStream output = new MemoryStream();
                using (DeflateStream deflateStream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
                {
                    deflateStream.Write(rawData, 0, rawData.Length);
                }
                compressedPackage.Write(output.ToArray());
                package = compressedPackage;
            }

            List<IEnumerator<bool>> writers = peers.Where(peer => peer.IsReady()).Select(p => SendToPeer(p, package)).ToList();
            writers.RemoveAll(writer => !writer.MoveNext());
            while (writers.Count > 0)
            {
                yield return null;
                writers.RemoveAll(writer => !writer.MoveNext());
            }
        }

        /// <summary>
        ///     Global package counter to identify fragmented packages
        /// </summary>
        private static long packageCounter = 0;

        /// <summary>
        ///     Coroutine to send a package async to an actual peer. Fragments the package if necessary.
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        private IEnumerator<bool> SendToPeer(ZNetPeer peer, ZPackage package)
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null)
            {
                yield break;
            }

            const int packageSliceSize = 250000;
            const int maximumSendQueueSize = 20000;

            IEnumerable<bool> waitForQueue()
            {
                float timeout = Time.time + 30;
                while (peer.m_socket.GetSendQueueSize() > maximumSendQueueSize)
                {
                    if (Time.time > timeout)
                    {
                        Logger.LogInfo($"Disconnecting {peer.m_uid} after 30 seconds config sending timeout");
                        peer.m_rpc.Invoke("Error", ZNet.ConnectionStatus.ErrorConnectFailed);
                        ZNet.instance.Disconnect(peer);
                        yield break;
                    }

                    yield return false;
                }
            }

            void SendPackage(ZPackage pkg)
            {
                string method = nameof(RPC_Jotunn_SyncConfig);
                if (ZNet.instance.IsServer())
                {
                    peer.m_rpc.Invoke(method, pkg);
                }
                else
                {
                    rpc.InvokeRoutedRPC(peer.m_server ? 0 : peer.m_uid, method, pkg);
                }
            }

            if (package.Size() > packageSliceSize)
            {
                byte[] data = package.GetArray();
                int fragments = (int)(1 + (data.LongLength - 1) / packageSliceSize);
                long packageIdentifier = ++packageCounter;
                for (int fragment = 0; fragment < fragments; fragment++)
                {
                    foreach (bool wait in waitForQueue())
                    {
                        yield return wait;
                    }

                    if (!peer.m_socket.IsConnected())
                    {
                        yield break;
                    }

                    ZPackage fragmentedPackage = new ZPackage();
                    fragmentedPackage.Write(FRAGMENTED_CONFIG);
                    fragmentedPackage.Write(packageIdentifier);
                    fragmentedPackage.Write(fragment);
                    fragmentedPackage.Write(fragments);
                    fragmentedPackage.Write(data.Skip(packageSliceSize * fragment).Take(packageSliceSize).ToArray());

                    Logger.LogDebug($"Sending fragmented package {packageIdentifier}:{fragment}");
                    SendPackage(fragmentedPackage);
                    
                    if (fragment != fragments - 1)
                    {
                        yield return true;
                    }
                }
            }
            else
            {
                foreach (bool wait in waitForQueue())
                {
                    yield return wait;
                }

                Logger.LogDebug("Sending package");
                SendPackage(package);
            }
        }

        /// <summary>
        ///     Wrapper Socket which holds up and preserves PeerInfo or RoutedRPC packages until
        ///     the finished member is set to true. All other packages get sent. This will
        ///     stop the client from completing the login handshake with the server until ready.
        /// </summary>
        private class PeerInfoBlockingSocket : ISocket
        {
            public volatile bool finished = false;
            public readonly List<ZPackage> Package = new List<ZPackage>();
            public readonly ISocket Original;

            public PeerInfoBlockingSocket(ISocket original)
            {
                Original = original;
            }

            public bool IsConnected() => Original.IsConnected();
            public ZPackage Recv() => Original.Recv();
            public int GetSendQueueSize() => Original.GetSendQueueSize();
            public int GetCurrentSendRate() => Original.GetCurrentSendRate();
            public bool IsHost() => Original.IsHost();
            public void Dispose() => Original.Dispose();
            public bool GotNewData() => Original.GotNewData();
            public void Close() => Original.Close();
            public string GetEndPointString() => Original.GetEndPointString();
            public void GetAndResetStats(out int totalSent, out int totalRecv) => Original.GetAndResetStats(out totalSent, out totalRecv);
            public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec) => Original.GetConnectionQuality(out localQuality, out remoteQuality, out ping, out outByteSec, out inByteSec);
            public ISocket Accept() => Original.Accept();
            public int GetHostPort() => Original.GetHostPort();
            public bool Flush() => Original.Flush();
            public string GetHostName() => Original.GetHostName();

            public void Send(ZPackage pkg)
            {
                pkg.SetPos(0);
                int methodHash = pkg.ReadInt();

                if ((methodHash == "PeerInfo".GetStableHashCode() || methodHash == "RoutedRPC".GetStableHashCode()) && !finished)
                {
                    Package.Add(new ZPackage(pkg.GetArray())); // the original ZPackage gets reused, create a new one
                }
                else
                {
                    Original.Send(pkg);
                }
            }
        }
    }
}
