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
        public static SynchronizationManager Instance => _instance ??= new SynchronizationManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private SynchronizationManager() { }

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

            // Hook PlayFab socket to disable compression as a hotfix for connection issues
            [HarmonyPatch(typeof(ZPlayFabSocket), nameof(ZPlayFabSocket.VersionMatch)), HarmonyPostfix]
            private static void ZPlayFabSocket_VersionMatch(ZPlayFabSocket __instance) => Instance.ZPlayFabSocket_VersionMatch(__instance);

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
            private static void FejdStartup_Awake(FejdStartup __instance) => Instance.FejdStartup_Awake(__instance);
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
                        yield return new WaitForSeconds(10);
                        self.m_adminList?.GetList();
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

                    var id = peer.m_socket.GetHostName();
                    var result =
                        !string.IsNullOrEmpty(ZNet.instance.m_adminList.m_list.FirstOrDefault(x => id.EndsWith(x)));
                    Logger.LogDebug($"Admin status: {(result ? "Admin" : "No Admin")}");
                    var adminPkg = new ZPackage();
                    adminPkg.Write(result);
                    yield return ZNet.instance.StartCoroutine(AdminRPC.SendPackageRoutine(peer.m_uid, adminPkg));

                    var pkg = GenerateConfigZPackage(true, GetSyncConfigValues());
                    yield return ZNet.instance.StartCoroutine(ConfigRPC.SendPackageRoutine(peer.m_uid, pkg));

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

        private void ZPlayFabSocket_VersionMatch(ZPlayFabSocket self)
        {
            self.m_useCompression = false;
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
        /// <param name="self"></param>
        private void FejdStartup_Awake(FejdStartup self)
        {
            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);
            foreach (var config in loadedPlugins.Where(x => x.Value.Config != null).Select(x => x.Value.Config))
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
            var loadedPlugins = BepInExUtils.GetDependentPlugins(true);

            var values = new List<Tuple<string, string, string, string>>();
            foreach (var plugin in loadedPlugins)
            {
                foreach (var cd in plugin.Value.Config.Keys)
                {
                    var cx = plugin.Value.Config[cd.Section, cd.Key];
                    if (cx.Description.Tags.Any(x => x is ConfigurationManagerAttributes { IsAdminOnly: true }))
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
                        x is ConfigurationManagerAttributes { IsAdminOnly: true, IsUnlocked: true }))
                    {
                        var value = new Tuple<string, string, string, string>(
                            plugin.Value.Info.Metadata.GUID, cd.Section, cd.Key, TomlTypeConverter.ConvertToString(cx.BoxedValue, cx.SettingType));
                        valuesToSend.Add(value);
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
                        ConfigRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);

                        // Also fire event that admin config was changed locally, since the RPC does not come back to the sender
                        InvokeOnConfigurationSynchronized(false);
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
                var axis = InputManager.GetGamepadAxis((InputManager.GamepadButton)entry.BoxedValue);

                if (!string.IsNullOrEmpty(axis))
                {
                    bool invert = axis.StartsWith("-");
                    def.m_axis = axis.TrimStart('-');
                    def.m_inverted = invert;
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

        private const byte INITIAL_CONFIG = 64;

        private IEnumerator ConfigRPC_OnClientReceive(long sender, ZPackage package)
        {
            byte packageFlags = package.ReadByte();

            if ((packageFlags & INITIAL_CONFIG) != 0)
            {
                InitAdminConfigs();
            }

            package.SetPos(0);
            ApplyConfigZPackage(package, out bool initial);
            InvokeOnConfigurationSynchronized(initial);
            yield break;
        }

        private IEnumerator ConfigRPC_OnServerReceive(long sender, ZPackage package)
        {
            // Is sender admin?
            var id = ZNet.instance.GetPeer(sender)?.m_socket?.GetHostName();
            if (!string.IsNullOrEmpty(id) &&
                !string.IsNullOrEmpty(ZNet.instance.m_adminList.m_list.FirstOrDefault(x => id.EndsWith(x))))
            {
                Logger.LogInfo($"Received configuration data from client {sender}");

                // Apply config locally
                ApplyConfigZPackage(package, out bool initial);

                // Send to all other clients
                ConfigRPC.SendPackage(ZNet.instance.m_peers.Where(x => x.m_uid != sender).ToList(), package);
            }
            yield break;
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
                            entry.BoxedValue = TomlTypeConverter.ConvertToValue(serializedValue, entry.SettingType);

                            // Set buttons after receive
                            SetInputButtons(entry);
                        }
                        else
                        {
                            Logger.LogWarning($"Setting for GUID: {modguid}, Section {section}, Key {key} is not syncable");
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Did not find Value for GUID: {modguid}, Section {section}, Key {key}");
                    }
                }
                else
                {
                    Logger.LogWarning($"No plugin with GUID {modguid} is loaded");
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
            public void VersionMatch() => Original.VersionMatch();

            public void Send(ZPackage pkg)
            {
                int methodHash = GetMethodHash(pkg);

                if ((methodHash == "PeerInfo".GetStableHashCode() || methodHash == "RoutedRPC".GetStableHashCode()) && !finished)
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
