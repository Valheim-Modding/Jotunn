using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Entities;
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
        private CustomRPC ConfigRPC;
        private CustomRPC AdminRPC;
        private List<Tuple<CustomRPC, Func<ZNetPeer, ZPackage>>> InitialSync = new List<Tuple<CustomRPC, Func<ZNetPeer, ZPackage>>>();

        internal readonly Dictionary<ConfigEntryBase, object> localValues = new Dictionary<ConfigEntryBase, object>();

        private readonly Dictionary<string, bool> CachedAdminStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, ConfigFile> CustomConfigs = new Dictionary<string, ConfigFile>();
        private List<Tuple<string, string, string, string>> CachedConfigValues = new List<Tuple<string, string, string, string>>();
        private BaseUnityPlugin ConfigurationManager;
        private bool ConfigurationManagerWindowShown;

        /// <summary>
        ///     Event triggered after configuration has been synced on either the server or client
        /// </summary>
        public static event EventHandler<ConfigurationSynchronizationEventArgs> OnConfigurationSynchronized;

        /// <summary>
        ///     Event triggered before syncing configuration on either the server or client
        /// </summary>
        public static event EventHandler<ConfigurationSynchronizationEventArgs> OnApplyingConfiguration;

        /// <summary>
        ///     Event triggered after a clients admin status changed on the server
        /// </summary>
        public static event Action OnAdminStatusChanged;

        private static SynchronizationManager _instance;

        /// <summary>
        ///     Singleton instance
        /// </summary>
        public static SynchronizationManager Instance => _instance ??= new SynchronizationManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private SynchronizationManager()
        { }

        /// <summary>
        ///     Clientside indicator if the current player has admin status on
        ///     the current world, always true on local games
        /// </summary>
        public bool PlayerIsAdmin { get; private set; }

        /// <summary>
        ///     Manager's main init
        /// </summary>
        void IManager.Init()
        {
            Main.LogInit("SynchronizationManager");

            // Register RPCs and the admin watchdog
            ConfigRPC = NetworkManager.Instance.AddRPC(
                Main.Instance.Info.Metadata, "ConfigSync", ConfigRPC_OnServerReceive, ConfigRPC_OnClientReceive);

            AdminRPC = NetworkManager.Instance.AddRPC(
                Main.Instance.Info.Metadata, "AdminStatus", null, AdminRPC_OnClientReceive);

            Main.Harmony.PatchAll(typeof(Patches));

            // Hook start scene to reset config
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            // Find Configuration manager plugin and add to DisplayingWindowChanged event
            const string configManagerGuid = "com.bepis.bepinex.configurationmanager";
            if (Chainloader.PluginInfos.TryGetValue(configManagerGuid, out var configManagerInfo) && configManagerInfo.Instance)
            {
                ConfigurationManager = configManagerInfo.Instance;

                Logger.LogDebug("Configuration manager found, trying to hook DisplayingWindowChanged");
                var eventinfo = ConfigurationManager.GetType().GetEvent("DisplayingWindowChanged");
                if (eventinfo != null)
                {
                    Action<object, object> local = ConfigurationManager_DisplayingWindowChanged;
                    var converted = Delegate.CreateDelegate(eventinfo.EventHandlerType, local.Target, local.Method);

                    eventinfo.AddEventHandler(ConfigurationManager, converted);
                }
            }

            AddInitialSynchronization(AdminRPC, peer =>
            {
                var id = peer.m_socket.GetHostName();
                var isAdmin = !string.IsNullOrEmpty(id) && ZNet.instance.ListContainsId(ZNet.instance.m_adminList, id);
                Logger.LogDebug($"Admin status: {(isAdmin ? "Admin" : "No Admin")}");

                var adminPkg = new ZPackage();
                adminPkg.Write(isAdmin);
                return adminPkg;
            });

            AddInitialSynchronization(ConfigRPC, () => GenerateConfigZPackage(true, GetSyncConfigValues()));
        }

        /// <summary>
        ///     Registers a non default config file for possible synchronisation with all clients.
        ///     Entries still need the IsAdminOnly attribute in order to be synchronized.<br />
        ///     The file path must be saved under the executing BepInEx config folder, see <see cref="BepInEx.Paths.ConfigPath" />.
        ///     This guarantees the same relative path for all clients.
        /// </summary>
        /// <param name="customFile">the file to synchronize</param>
        /// <exception cref="T:System.ArgumentException">The config file is not saved under the BepInEx config folder</exception>
        /// <exception cref="T:System.ArgumentException">The config file is already registered</exception>
        /// <exception cref="T:System.ArgumentException">The config file is a default mod config and is already implicitly synchronized</exception>
        public void RegisterCustomConfig(ConfigFile customFile)
        {
            if (!customFile.ConfigFilePath.StartsWith(BepInEx.Paths.ConfigPath))
            {
                throw new ArgumentException($"Config file must be saved under the BepInEx config folder. {customFile.ConfigFilePath}");
            }

            string identifier = GetFileIdentifier(customFile);

            if (IsDefaultModConfig(identifier, out string modGUID))
            {
                throw new ArgumentException($"Config file must not be a default mod config: {modGUID}. It is already synchronized");
            }

            if (CustomConfigs.ContainsKey(identifier))
            {
                throw new ArgumentException($"Config file already registered. {customFile.ConfigFilePath}");
            }

            Logger.LogDebug($"Registering custom config file {identifier}");
            CustomConfigs.Add(identifier, customFile);
        }

        /// <summary>
        ///     Add a <see cref="CustomRPC"/> and a method for generating a <see cref="ZPackage"/> to the manager.<br />
        ///     The RPC will be initiated on the server side after login to sync arbitrary data to the connecting client.
        ///     The package is guaranteed to be received before the client's connection is fully established and the player loads into the world.
        /// </summary>
        /// <param name="rpc">RPC to be called</param>
        /// <param name="packageGenerator">Method generating the ZPackage payload, takes the client peer as its argument</param>
        public void AddInitialSynchronization(CustomRPC rpc, Func<ZNetPeer, ZPackage> packageGenerator)
        {
            InitialSync.Add(new Tuple<CustomRPC, Func<ZNetPeer, ZPackage>>(rpc, packageGenerator));
        }

        /// <summary>
        ///     Add a <see cref="CustomRPC"/> and a method for generating a <see cref="ZPackage"/> to the manager.<br />
        ///     The RPC will be initiated on the server side after login to sync arbitrary data to the connecting client.
        ///     The package is guaranteed to be received before the client's connection is fully established and the player loads into the world.
        /// </summary>
        /// <param name="rpc">RPC to be called</param>
        /// <param name="packageGenerator">Method generating the ZPackage payload</param>
        public void AddInitialSynchronization(CustomRPC rpc, Func<ZPackage> packageGenerator)
        {
            AddInitialSynchronization(rpc, peer => packageGenerator());
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake)), HarmonyPostfix]
            private static void ZNet_Awake(ZNet __instance) => Instance.ZNet_Awake(__instance);

            // Hook RPC_PeerInfo for initial retrieval of admin status and configuration
            [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo)), HarmonyPrefix]
            private static void ZNet_RPC_Pre_PeerInfo(ZNet __instance, ZRpc rpc, ref PeerInfoBlockingSocket __state) => Instance.ZNet_RPC_Pre_PeerInfo(__instance, rpc, ref __state);

            [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo)), HarmonyPostfix]
            private static void ZNet_RPC_Post_PeerInfo(ZNet __instance, ZRpc rpc, ref PeerInfoBlockingSocket __state) => Instance.ZNet_RPC_Post_PeerInfo(__instance, rpc, ref __state);

            // Hook SyncedList for admin list changes
            [HarmonyPatch(typeof(SyncedList), nameof(SyncedList.Load)), HarmonyPostfix]
            private static void SyncedList_Load(SyncedList __instance) => Instance.SyncedList_Load(__instance);

            [HarmonyPatch(typeof(SyncedList), nameof(SyncedList.Save)), HarmonyPostfix]
            private static void SyncedList_Save(SyncedList __instance) => Instance.SyncedList_Save(__instance);

            // Hook menu for ConfigManager integration
            [HarmonyPatch(typeof(Menu), nameof(Menu.IsVisible)), HarmonyPostfix]
            private static void Menu_IsVisible(ref bool __result) => Instance.Menu_IsVisible(ref __result);

            // Hook Fejd for ConfigReloaded event subscription
            [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake)), HarmonyPrefix]
            private static void FejdStartup_Awake() => Instance.FejdStartup_Awake();
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
        /// <param name="self"></param>
        private void ZNet_Awake(ZNet self)
        {
            if (self.IsServer())
            {
                IEnumerator watchdog()
                {
                    while (true)
                    {
                        yield return new WaitForSeconds(5);
                        self.m_adminList?.CheckLoad();
                    }
                }
                self.StartCoroutine(watchdog());
            }
        }

        /// <summary>
        ///     Hook ZNet.RPC_PeerInfo on the server to send initial data
        /// </summary>
        /// <param name="self"></param>
        /// <param name="rpc"></param>
        /// <param name="__state"></param>
        private void ZNet_RPC_Pre_PeerInfo(ZNet self, ZRpc rpc, ref PeerInfoBlockingSocket __state)
        {
            PeerInfoBlockingSocket bufferingSocket = null;

            // Create buffering socket
            if (self.IsServer())
            {
                bufferingSocket = new PeerInfoBlockingSocket(rpc.GetSocket());
                rpc.m_socket = bufferingSocket;

                ZNetPeer peer = self.GetPeer(rpc);
                if (ZNet.m_onlineBackend != OnlineBackendType.Steamworks && peer != null)
                {
                    peer.m_socket = bufferingSocket;
                }
            }

            __state = bufferingSocket;
        }

        private void ZNet_RPC_Post_PeerInfo(ZNet self, ZRpc rpc, ref PeerInfoBlockingSocket __state)
        {
            PeerInfoBlockingSocket bufferingSocket = __state;

            // Send initial data
            if (self.IsServer())
            {
                ZNetPeer peer = self.GetPeer(rpc);

                if (peer == null || !peer.IsReady())
                {
                    Logger.LogInfo($"Peer has disconnected. Skipping initial data send.");
                    return;
                }

                IEnumerator SynchronizeInitialData()
                {
                    Logger.LogInfo($"Sending initial data to peer #{peer.m_uid}");

                    // ReSharper disable once UseDeconstruction
                    foreach (var tuple in InitialSync)
                    {
                        var targetRPC = tuple.Item1;
                        var packageGenerator = tuple.Item2;
                        var package = packageGenerator(peer);
                        if (package != null && package.Size() > 0)
                        {
                            Logger.LogDebug($"Calling custom RPC {targetRPC}");
                            yield return ZNet.instance.StartCoroutine(targetRPC.SendPackageRoutine(peer.m_uid, package));
                        }
                    }

                    if (peer.m_rpc.GetSocket() is PeerInfoBlockingSocket currentSocket)
                    {
                        peer.m_rpc.m_socket = currentSocket.Original;
                        peer.m_socket = currentSocket.Original;
                    }

                    bufferingSocket.finished = true;

                    for (var i = 0; i < bufferingSocket.Package.Count; i++)
                    {
                        if (i == bufferingSocket.versionMatchPackageIndex)
                        {
                            bufferingSocket.Original.VersionMatch();
                        }
                        var package = bufferingSocket.Package[i];
                        bufferingSocket.Original.Send(package);
                    }

                    if (bufferingSocket.Package.Count == bufferingSocket.versionMatchPackageIndex)
                    {
                        bufferingSocket.Original.VersionMatch();
                    }
                }

                self.StartCoroutine(SynchronizeInitialData());
            }
        }

        /// <summary>
        ///     Hook <see cref="SyncedList.Save"/> to synchronize the admin status to the clients
        /// </summary>
        /// <param name="self"></param>
        private void SyncedList_Save(SyncedList self)
        {
            // Check if it really is the admin list
            if (ZNet.instance != null && self == ZNet.instance.m_adminList)
            {
                SynchronizeAdminStatus();
            }
        }

        /// <summary>
        ///     Hook <see cref="SyncedList.Load"/> to synchronize the admin status to the clients
        /// </summary>
        /// <param name="self"></param>
        private void SyncedList_Load(SyncedList self)
        {
            // Check if it really is the admin list
            if (ZNet.instance != null && self == ZNet.instance.m_adminList)
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
            var clientId = ZNet.instance.m_peers.FirstOrDefault(x => x.m_socket.GetHostName().EndsWith(entry))?.m_uid;
            if (clientId != null)
            {
                Logger.LogInfo($"Sending admin status to {entry}/{clientId} ({(admin ? "is admin" : "is no admin")})");
                var pkg = new ZPackage();
                pkg.Write(admin);
                AdminRPC.SendPackage(clientId.Value, pkg);
            }
        }

        private IEnumerator AdminRPC_OnClientReceive(long sender, ZPackage package)
        {
            bool isAdmin = package.ReadBool();

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
            yield break;
        }

        /// <summary>
        ///     Safely invoke the <see cref="OnAdminStatusChanged"/> event
        /// </summary>
        private void InvokeOnAdminStatusChanged()
        {
            OnAdminStatusChanged?.SafeInvoke();
        }

        private IEnumerable<ConfigFile> GetConfigFiles()
        {
            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);

            foreach (var plugin in loadedPlugins.Values)
            {
                yield return plugin.Config;
            }

            foreach (var customConfigFile in CustomConfigs.Values)
            {
                yield return customConfigFile;
            }
        }

        private static string GetFileIdentifier(ConfigFile config)
        {
            return config.ConfigFilePath.Replace(BepInEx.Paths.ConfigPath, "").Replace("\\", "/").Trim('/');
        }

        private static string GetPluginIdentifier(string configFileIdentifier)
        {
            if (configFileIdentifier.EndsWith(".cfg"))
            {
                configFileIdentifier = configFileIdentifier.Substring(0, configFileIdentifier.Length - 4);
            }
            return configFileIdentifier;
        }

        private ConfigFile GetConfigFile(string identifier)
        {
            if (CustomConfigs.TryGetValue(identifier, out var config))
            {
                return config;
            }

            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);

            if (IsDefaultModConfig(identifier, out string modGUID) && loadedPlugins.TryGetValue(modGUID, out var plugin))
            {
                return plugin.Config;
            }

            return null;
        }

        private static bool IsDefaultModConfig(string identifier, out string modGUID)
        {
            if (identifier.EndsWith(".cfg"))
            {
                modGUID = identifier.Substring(0, identifier.Length - 4);
                // must access Chainloader directly because the mod list may only be partially initialized
                return Chainloader.PluginInfos.ContainsKey(modGUID);
            }

            modGUID = string.Empty;
            return false;
        }

        /// <summary>
        ///     Unlock configuration entries.
        /// </summary>
        private void UnlockConfigurationEntries()
        {
            foreach (var config in GetConfigFiles())
            {
                foreach (var configDefinition in config.Keys)
                {
                    var configEntry = config[configDefinition.Section, configDefinition.Key];
                    var configAttribute = configEntry.GetConfigurationManagerAttributes();

                    if (configAttribute?.IsAdminOnly == true)
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
            foreach (var config in GetConfigFiles())
            {
                foreach (var configDefinition in config.Keys)
                {
                    var configEntry = config[configDefinition.Section, configDefinition.Key];
                    var configAttribute = configEntry.GetConfigurationManagerAttributes();

                    if (configAttribute?.IsAdminOnly == true)
                    {
                        configAttribute.IsUnlocked = false;
                    }
                }
            }
        }

        /// <summary>
        ///     Hook <see cref="Menu.IsVisible"/> to unlock cursor properly and disable camera rotation
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private void Menu_IsVisible(ref bool result)
        {
            result = result || ConfigurationManagerWindowShown;
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
        private void FejdStartup_Awake()
        {
            foreach (var config in GetConfigFiles())
            {
                config.ConfigReloaded += Config_ConfigReloaded;
            }

            // Harmony patch BepInEx to ensure locked values are not overwritten
            Main.Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(ConfigEntryBase), nameof(ConfigEntryBase.GetSerializedValue)),
                new HarmonyMethod(AccessTools.DeclaredMethod(typeof(SynchronizationManager),
                    nameof(ConfigEntryBase_GetSerializedValue))));
            Main.Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(ConfigEntryBase), nameof(ConfigEntryBase.SetSerializedValue)),
                new HarmonyMethod(AccessTools.DeclaredMethod(typeof(SynchronizationManager),
                    nameof(ConfigEntryBase_SetSerializedValue))));
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
        private static bool ConfigEntryBase_GetSerializedValue(ConfigEntryBase __instance, ref string __result)
        {
            if (!__instance.IsSyncable() || GUIManager.IsHeadless() || __instance.GetLocalValue() == null)
            {
                return true;
            }

            __result = TomlTypeConverter.ConvertToString(__instance.GetLocalValue(), __instance.SettingType);
            return false;
        }

        /// <summary>
        ///     Prevent overwriting bep config value when the setting is locked on config file reload
        /// </summary>
        private static bool ConfigEntryBase_SetSerializedValue(ConfigEntryBase __instance, string value)
        {
            if (GUIManager.IsHeadless() || __instance.GetLocalValue() == null)
            {
                return true;
            }
            return false;
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

            var entries = new List<Tuple<string, string, string, string>>();
            foreach (var config in GetConfigFiles())
            {
                string configIdentifier = GetFileIdentifier(config);

                foreach (var cd in config.Keys)
                {
                    ConfigEntryBase cx = config[cd.Section, cd.Key];
                    var configAttribute = cx.GetConfigurationManagerAttributes();

                    if (configAttribute?.IsAdminOnly == true)
                    {
                        var value = TomlTypeConverter.ConvertToString(cx.BoxedValue, cx.SettingType);
                        var entry = new Tuple<string, string, string, string>(configIdentifier, cd.Section, cd.Key, value);
                        entries.Add(entry);
                    }
                }
            }

            return entries;
        }

        /// <summary>
        ///     Syncs the changed configuration of a client to the server
        /// </summary>
        internal void SynchronizeChangedConfig()
        {
            // Lets compare and send to server, if applicable
            var valuesToSend = new List<Tuple<string, string, string, string>>();
            foreach (var config in GetConfigFiles())
            {
                string configIdentifier = GetFileIdentifier(config);

                foreach (var cd in config.Keys)
                {
                    var cx = config[cd.Section, cd.Key];
                    var configAttribute = cx.GetConfigurationManagerAttributes();

                    if (configAttribute?.IsAdminOnly == true)
                    {
                        var value = TomlTypeConverter.ConvertToString(cx.BoxedValue, cx.SettingType);
                        var entry = new Tuple<string, string, string, string>(configIdentifier, cd.Section, cd.Key, value);
                        valuesToSend.Add(entry);
                    }

                    // Set buttons if changed
                    SetInputButtons(cx);
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
                        // Fire event that admin config will be changed locally, since the RPC does not come back to the sender
                        InvokeOnApplyingConfiguration();
                        ConfigRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);

                        // Get IDs of plugins that received data
                        var pluginIDs = valuesToSend.Select(x => x.Item1).ToList();
                        pluginIDs.ForEach(x => GetPluginIdentifier(x));

                        // Also fire event that admin config was changed locally, since the RPC does not come back to the sender
                        InvokeOnConfigurationSynchronized(false, new HashSet<string>(pluginIDs));
                    }
                    // Send changed values to all connected clients
                    else
                    {
                        ConfigRPC.SendPackage(ZNet.instance.m_peers, package);
                    }
                }

                // Rebuild config cache
                CacheConfigurationValues();
            }
        }

        private void SetInputButtons(ConfigEntryBase entry)
        {
            if (ZInput.instance == null)
            {
                return;
            }

            string buttonName = entry.GetBoundButtonName();

            if (string.IsNullOrEmpty(buttonName))
            {
                return;
            }

            ZInput.ButtonDef def;

            if (entry.SettingType == typeof(KeyCode) &&
                ZInput.instance.m_buttons.TryGetValue(buttonName, out def))
            {
                def.m_key = (KeyCode)entry.BoxedValue;
            }

            if (entry.SettingType == typeof(KeyboardShortcut) &&
                ZInput.instance.m_buttons.TryGetValue(buttonName, out def))
            {
                def.m_key = ((KeyboardShortcut)entry.BoxedValue).MainKey;
            }

            if (entry.SettingType == typeof(InputManager.GamepadButton) &&
                ZInput.instance.m_buttons.TryGetValue($"Joy!{buttonName}", out def))
            {
                var keyCode = InputManager.GetGamepadKeyCode((InputManager.GamepadButton)entry.BoxedValue);
                var input = InputManager.GetGamepadInput((InputManager.GamepadButton)entry.BoxedValue);

                if (input != GamepadInput.None)
                {
                    def.m_gamepadInput = input;
                }
                else
                {
                    def.m_key = keyCode;
                }
            }
        }

        /// <summary>
        ///     Cache local config values for synced entries
        /// </summary>
        private void InitAdminConfigs()
        {
            foreach (var config in GetConfigFiles())
            {
                foreach (var configDefinition in config.Keys)
                {
                    var configEntry = config[configDefinition.Section, configDefinition.Key];
                    var configAttribute = configEntry.GetConfigurationManagerAttributes();

                    if (configAttribute?.IsAdminOnly == true && configEntry.BoxedValue != null)
                    {
                        localValues[configEntry] = configEntry.BoxedValue;
                    }
                }
            }
        }

        /// <summary>
        ///     Reset configs which may have been overwritten with server values to the local value
        /// </summary>
        private void ResetAdminConfigs()
        {
            foreach (var localValue in localValues)
            {
                localValue.Key.BoxedValue = localValue.Value;
            }

            localValues.Clear();
        }

        private const byte INITIAL_CONFIG = 64;

        private IEnumerator ConfigRPC_OnClientReceive(long sender, ZPackage package)
        {
            InvokeOnApplyingConfiguration();

            byte packageFlags = package.ReadByte();

            if ((packageFlags & INITIAL_CONFIG) != 0)
            {
                InitAdminConfigs();
            }

            package.SetPos(0);
            ApplyConfigZPackage(package, out bool initial, out HashSet<string> pluginIDs);
            InvokeOnConfigurationSynchronized(initial, pluginIDs);
            yield break;
        }

        private IEnumerator ConfigRPC_OnServerReceive(long sender, ZPackage package)
        {
            // Is sender admin?
            if (ZNet.instance.IsAdmin(sender))
            {
                Logger.LogInfo($"Received configuration data from client {sender}");
                InvokeOnApplyingConfiguration();

                // Apply config locally
                ApplyConfigZPackage(package, out bool initial, out HashSet<string> pluginIDs);
                InvokeOnConfigurationSynchronized(initial, pluginIDs);

                // Send to all other clients
                ConfigRPC.SendPackage(ZNet.instance.m_peers.Where(x => x.m_uid != sender).ToList(), package);
            }
            yield break;
        }

        /// <summary>
        ///     Safely invoke the <see cref="OnConfigurationSynchronized"/> event
        /// </summary>
        private void InvokeOnConfigurationSynchronized(bool initial, HashSet<string> pluginIDs)
        {
            OnConfigurationSynchronized?.SafeInvoke(
                this,
                new ConfigurationSynchronizationEventArgs()
                {
                    InitialSynchronization = initial,
                    UpdatedPluginIDs = pluginIDs
                }
            );
        }

        /// <summary>
        ///     Safely invoke the <see cref="OnConfigurationSynchronized"/> event
        /// </summary>
        private void InvokeOnApplyingConfiguration()
        {
            OnApplyingConfiguration?.SafeInvoke(this, new ConfigurationSynchronizationEventArgs());
        }

        /// <summary>
        ///     Apply received configuration values locally and regenerate the cache
        /// </summary>
        /// <param name="configPkg">Package of config tuples</param>
        /// <param name="initial">Indicator if this was an initial config package</param>
        /// <param name="pluginIDs">Indicator if this was an initial config package</param>
        private void ApplyConfigZPackage(ZPackage configPkg, out bool initial, out HashSet<string> pluginIDs)
        {
            initial = (configPkg.ReadByte() & INITIAL_CONFIG) != 0;
            pluginIDs = new HashSet<string>();

            Logger.LogDebug($"Applying{(initial ? " initial" : null)} configuration data package");

            var numberOfEntries = configPkg.ReadInt();
            if (numberOfEntries == 0)
            {
                return;
            }

            while (numberOfEntries > 0)
            {
                var configIdentifier = configPkg.ReadString();
                var section = configPkg.ReadString();
                var key = configPkg.ReadString();
                var serializedValue = configPkg.ReadString();
                pluginIDs.Add(GetPluginIdentifier(configIdentifier));

                Logger.LogDebug($"Received {configIdentifier} {section} {key} {serializedValue}");

                ConfigFile config = GetConfigFile(configIdentifier);

                if (config != null)
                {
                    pluginIDs.Add(configIdentifier);
                    if (config.Keys.Contains(new ConfigDefinition(section, key)))
                    {
                        var entry = config[section, key];
                        if (entry.IsSyncable())
                        {
                            Logger.LogDebug($"Setting config value {configIdentifier}.{section}.{key} to {serializedValue}");
                            entry.BoxedValue = TomlTypeConverter.ConvertToValue(serializedValue, entry.SettingType);

                            // Set buttons after receive
                            SetInputButtons(entry);
                        }
                        else
                        {
                            Logger.LogWarning($"Setting for Identifier: {configIdentifier}, Section {section}, Key {key} is not syncable");
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Did not find Value for Identifier: {configIdentifier}, Section {section}, Key {key}");
                    }
                }
                else
                {
                    Logger.LogWarning($"No config file with Identifier {configIdentifier} is loaded");
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
        ///     Wrapper Socket which holds up and preserves PeerInfo or RoutedRPC packages until
        ///     the finished member is set to true. All other packages get sent. This will
        ///     stop the client from completing the login handshake with the server until ready.
        /// </summary>
        internal class PeerInfoBlockingSocket : ISocket
        {
            public volatile bool finished = false;
            public volatile int versionMatchPackageIndex = -1;
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

            public void VersionMatch()
            {
                if (finished)
                {
                    Original.VersionMatch();
                }
                else
                {
                    versionMatchPackageIndex = Package.Count;
                }
            }

            public void Send(ZPackage pkg)
            {
                int methodHash = GetMethodHash(pkg);

                if (!finished && (methodHash == "PeerInfo".GetStableHashCode() ||
                                  methodHash == "RoutedRPC".GetStableHashCode() ||
                                  methodHash == "ZDOData".GetStableHashCode()))
                {
                    // the original ZPackage gets reused, create a new one
                    Package.Add(CopyZPackage(pkg));
                }
                else
                {
                    Original.Send(pkg);
                }
            }

            internal static int GetMethodHash(ZPackage pkg)
            {
                int originalPos = pkg.GetPos();
                pkg.SetPos(0);
                int methodHash = pkg.ReadInt();
                pkg.SetPos(originalPos);

                return methodHash;
            }

            internal static ZPackage CopyZPackage(ZPackage pkg)
            {
                ZPackage copy = new ZPackage(pkg.GetArray());
                copy.SetPos(pkg.GetPos());
                return copy;
            }
        }
    }
}
