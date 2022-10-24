using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Implementation of the mod compatibility features.
    /// </summary>
    public static class ModCompatibility
    {
        /// <summary>
        ///     Stores the last server message.
        /// </summary>
        private static ZPackage LastServerVersion;

        private static readonly Dictionary<string, ZPackage> ClientVersions = new Dictionary<string, ZPackage>();

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection)), HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static void ZNet_OnNewConnection(ZNet __instance, ZNetPeer peer)
        {
            // clear the previous connection, if existing
            LastServerVersion = null;

            // Register our RPC very early
            peer.m_rpc.Register<ZPackage>(nameof(RPC_Jotunn_ReceiveVersionData), RPC_Jotunn_ReceiveVersionData);
        }

        // Send client module list to server
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_ClientHandshake)), HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static void ZNet_RPC_ClientHandshake(ZNet __instance, ZRpc rpc)
        {
            rpc.Invoke(nameof(RPC_Jotunn_ReceiveVersionData), new ModuleVersionData(GetEnforcableMods().ToList()).ToZPackage());
        }

        // Send server module list to client
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_ServerHandshake)), HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static void ZNet_RPC_ServerHandshake(ZNet __instance, ZRpc rpc)
        {
            rpc.Invoke(nameof(RPC_Jotunn_ReceiveVersionData), new ModuleVersionData(GetEnforcableMods().ToList()).ToZPackage());
        }

        // Show mod compatibility error message when needed
        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.ShowConnectError)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void FejdStartup_ShowConnectError(FejdStartup __instance)
        {
            if (LastServerVersion != null && ZNet.m_connectionStatus == ZNet.ConnectionStatus.ErrorVersion)
            {
                string failedConnectionText = __instance.m_connectionFailedError.text;
                __instance.StartCoroutine(ShowModCompatibilityErrorMessage(failedConnectionText));
                __instance.m_connectionFailedPanel.SetActive(false);
            }
        }

        // Hook client sending of PeerInfo
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.SendPeerInfo)), HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static bool ZNet_SendPeerInfo(ZNet __instance, ZRpc rpc, string password)
        {
            if (ZNet.instance.IsClientInstance())
            {
                // If there was no server version response, Jötunn is not installed. Cancel if we have mandatory mods
                if (LastServerVersion == null && GetEnforcableMods().Any(x => x.IsNeededOnServer()))
                {
                    Logger.LogWarning("Jötunn is not installed on the server. Client has mandatory mods. Cancelling connection");
                    rpc.Invoke("Disconnect");
                    LastServerVersion = new ModuleVersionData(new List<ModModule>()).ToZPackage();
                    ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorVersion;
                    return false;
                }
            }

            return true;
        }

        // Hook RPC_PeerInfo to check in front of the original method
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo)), HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static bool ZNet_RPC_PeerInfo(ZNet __instance, ZRpc rpc, ZPackage pkg)
        {
            if (!ZNet.instance.IsClientInstance())
            {
                // Vanilla client trying to connect?
                if (!ClientVersions.ContainsKey(rpc.GetSocket().GetEndPointString()))
                {
                    // Check mods, if there are some installed on the server which need also to be on the client
                    if (GetEnforcableMods().Any(x => x.IsNeededOnClient()))
                    {
                        // There is a mod, which needs to be client side too
                        // Lets disconnect the vanilla client with Incompatible Version message

                        Logger.LogWarning("Disconnecting vanilla client with incompatible version message. " +
                                          "There are mods that need to be installed on the client");
                        rpc.Invoke("Error", (int)ZNet.ConnectionStatus.ErrorVersion);
                        return false;
                    }
                }
                else
                {
                    var serverData = new ModuleVersionData(GetEnforcableMods().ToList());
                    var clientData = new ModuleVersionData(ClientVersions[rpc.m_socket.GetEndPointString()]);

                    if (!CompareVersionData(serverData, clientData))
                    {
                        // Disconnect if mods are not network compatible
                        Logger.LogWarning("RPC_PeerInfo: Disconnecting modded client with incompatible version message. " +
                                          "Mods are not compatible");
                        rpc.Invoke("Error", (int)ZNet.ConnectionStatus.ErrorVersion);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     Store server's message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private static void RPC_Jotunn_ReceiveVersionData(ZRpc sender, ZPackage data)
        {
            Logger.LogDebug($"Received Version package from {sender.m_socket.GetEndPointString()}");

            if (!ZNet.instance.IsClientInstance())
            {
                ClientVersions[sender.m_socket.GetEndPointString()] = data;
                var serverData = new ModuleVersionData(GetEnforcableMods().ToList());
                var clientData = new ModuleVersionData(data);

                if (!CompareVersionData(serverData, clientData))
                {
                    // Disconnect if mods are not network compatible
                    Logger.LogWarning("RPC_Jotunn_ReceiveVersionData: Disconnecting modded client with incompatible version message. " +
                                      "Mods are not compatible");
                    sender.Invoke("Error", (int)ZNet.ConnectionStatus.ErrorVersion);
                }
            }
            else
            {
                LastServerVersion = data;
            }
        }

        internal static bool CompareVersionData(ModuleVersionData serverData, ModuleVersionData clientData)
        {
            if (ReferenceEquals(serverData, clientData))
            {
                return true;
            }

            bool result = true;

            if (!Equals(serverData.ValheimVersion, clientData.ValheimVersion))
            {
                Logger.LogWarning($"Version incompatibility: Server {serverData.ValheimVersion}, Client {clientData.ValheimVersion}");
                result = false;
            }

            // Check server enforced mods
            foreach (var serverModule in serverData.Modules)
            {
                if (serverModule.IsNeededOnClient() && !clientData.HasModule(serverModule.name))
                {
                    Logger.LogWarning($"Missing mod on client: {serverModule.name}");
                    result = false;
                }
            }

            // Check client enforced mods
            foreach (var clientModule in clientData.Modules)
            {
                if (clientModule.IsNeededOnServer() && !serverData.HasModule(clientModule.name))
                {
                    Logger.LogWarning($"Client loaded additional mod: {clientModule.name}");
                    result = false;
                }
            }

            // Compare modules
            foreach (var serverModule in serverData.Modules)
            {
                if (serverModule.IsNotEnforced())
                {
                    continue;
                }

                var clientModule = clientData.FindModule(serverModule.name);

                if (clientModule == null)
                {
                    continue;
                }

                if (ModModule.IsLowerVersion(serverModule, clientModule, serverModule.versionStrictness))
                {
                    Logger.LogWarning($"Mod version mismatch {serverModule.name}: Server {serverModule.version}, Client {clientModule.version}");
                    result = false;
                }

                if (ModModule.IsLowerVersion(clientModule, serverModule, serverModule.versionStrictness))
                {
                    Logger.LogWarning($"Mod version mismatch {serverModule.name}: Server {serverModule.version}, Client {clientModule.version}");
                    result = false;
                }
            }

            return result;
        }

        private static CompatibilityWindow LoadCompatWindow()
        {
            AssetBundle jotunnBundle = AssetUtils.LoadAssetBundleFromResources("modcompat", typeof(Main).Assembly);
            var compatWindowGameObject = Object.Instantiate(jotunnBundle.LoadAsset<GameObject>("CompatibilityWindow"), GUIManager.CustomGUIFront.transform);
            jotunnBundle.Unload(false);

            var compatWindow = compatWindowGameObject.GetComponent<CompatibilityWindow>();
            var compatWindowRect = ((RectTransform)compatWindow.transform);

            foreach (var text in compatWindow.GetComponentsInChildren<Text>())
            {
                GUIManager.Instance.ApplyTextStyle(text, 18);
            }

            GUIManager.Instance.ApplyWoodpanelStyle(compatWindow.transform);
            GUIManager.Instance.ApplyScrollRectStyle(compatWindow.scrollRect);
            GUIManager.Instance.ApplyButtonStyle(compatWindow.continueButton);

            compatWindowRect.anchoredPosition = new Vector2(25, 0);
            compatWindow.gameObject.SetWidth(900);
            compatWindow.gameObject.SetHeight(500);

            return compatWindow;
        }

        /// <summary>
        ///     Create and show mod compatibility error message
        /// </summary>
        private static IEnumerator ShowModCompatibilityErrorMessage(string failedConnectionText)
        {
            var compatWindow = LoadCompatWindow();
            var remote = new ModuleVersionData(LastServerVersion);
            var local = new ModuleVersionData(GetEnforcableMods().ToList());

            compatWindow.failedConnection.text = ColoredText(GUIManager.Instance.ValheimOrange, "Failed connection:", true) +
                                                 failedConnectionText.Trim();
            compatWindow.localVersion.text = ColoredText(GUIManager.Instance.ValheimOrange, "Local Version (your game):", true) +
                                             local.ToString(false).Trim();
            compatWindow.remoteVersion.text = ColoredText(GUIManager.Instance.ValheimOrange, "Remote Version (the server):", true) +
                                              remote.ToString(false).Trim();
            compatWindow.errorMessages.text = CreateErrorMessage(remote, local).Trim();

            // Unity needs a frame to correctly calculate the preferred height. The components need to be active so we scale them down to 0
            // Their LayoutRebuilder.ForceRebuildLayoutImmediate does not take wrapped text into account
            compatWindow.transform.localScale = Vector3.zero;
            yield return null;
            compatWindow.transform.localScale = Vector3.one;

            compatWindow.UpdateTextPositions();
            compatWindow.continueButton.onClick.AddListener(() => Object.Destroy(compatWindow.gameObject));
            compatWindow.scrollRect.verticalNormalizedPosition = 1f;

            // Reset the last server version
            LastServerVersion = null;
        }

        /// <summary>
        ///     Create the error message(s) from the server and client message data
        /// </summary>
        /// <param name="serverData">server data</param>
        /// <param name="clientData">client data</param>
        /// <returns></returns>
        private static string CreateErrorMessage(ModuleVersionData serverData, ModuleVersionData clientData)
        {
            return CreateVanillaVersionErrorMessage(serverData, clientData) +
                   CreateVersionStringErrorMessage(serverData, clientData) +
                   CreateNotInstalledErrorMessage(serverData, clientData) +
                   CreateNotLowerVersionErrorMessage(serverData, clientData) +
                   CreateNotHigherVersionErrorMessage(serverData, clientData) +
                   CreateAdditionalModsErrorMessage(serverData, clientData);
        }

        private static string CreateVanillaVersionErrorMessage(ModuleVersionData serverData, ModuleVersionData clientData)
        {
            if (serverData.ValheimVersion > clientData.ValheimVersion)
            {
                return ColoredText(Color.red, "Valheim version error:", true) +
                       ColoredText(Color.white, $"The server runs Valheim {serverData.ValheimVersion}, while your client runs Valheim {clientData.ValheimVersion}", true) +
                       ColoredText(Color.white, $"Please update your game to version {serverData.ValheimVersion} or contact your server admin", true) +
                       Environment.NewLine;
            }

            if (serverData.ValheimVersion < clientData.ValheimVersion)
            {
                return ColoredText(Color.red, "Valheim version error:", true) +
                       ColoredText(Color.white, $"The server runs {serverData.ValheimVersion}, while your client runs {clientData.ValheimVersion}", true) +
                       ColoredText(Color.white, $"Please downgrade your game to version {serverData.ValheimVersion} or contact your server admin", true) +
                       Environment.NewLine;
            }

            return string.Empty;
        }

        private static string CreateVersionStringErrorMessage(ModuleVersionData serverData, ModuleVersionData clientData)
        {
            if (serverData.ValheimVersion == clientData.ValheimVersion && !string.IsNullOrEmpty(serverData.VersionString) && serverData.VersionString != clientData.VersionString)
            {
                return ColoredText(GUIManager.Instance.ValheimOrange, "Valheim modded version string mismatch:", true) +
                       ColoredText(Color.white, $"This may indicates that mods are missing. Not all mods change the version string equally or even require to be installed on both server and client. Please check the requirements of the listed mods", true) +
                       ColoredText(Color.white, $"Local (your game): {clientData.VersionString}", true) +
                       ColoredText(Color.white, $"Remote (the server): {serverData.VersionString}", true) +
                       Environment.NewLine;
            }

            return string.Empty;
        }

        private static string CreateNotInstalledErrorMessage(ModuleVersionData serverData, ModuleVersionData clientData)
        {
            List<ModModule> matchingServerMods = FindMods(serverData, clientData, (serverModule, clientModule) =>
            {
                return serverModule.IsNeededOnClient() && clientModule == null;
            }).ToList();

            if (matchingServerMods.Count == 0)
            {
                return string.Empty;
            }

            string result = ColoredText(Color.red, "Missing mods:", true);

            foreach (ModModule serverModule in matchingServerMods)
            {
                result += ColoredText(Color.white, $"Please install mod {serverModule.name} {serverModule.version}", true);
            }

            return result + Environment.NewLine;
        }

        private static string CreateNotLowerVersionErrorMessage(ModuleVersionData serverData, ModuleVersionData clientData)
        {
            List<ModModule> matchingServerMods = FindMods(serverData, clientData, (serverModule, clientModule) =>
            {
                return clientModule != null && ModModule.IsLowerVersion(serverModule, clientModule, serverModule.versionStrictness);
            }).ToList();

            if (matchingServerMods.Count == 0)
            {
                return string.Empty;
            }

            return ColoredText(Color.red, "Mod updates needed:", true) +
                   matchingServerMods.Aggregate("", (current, serverModule) => current + ColoredText(Color.white, $"Please update mod {serverModule.name} to version {serverModule.GetVersionString()}", true)) +
                   Environment.NewLine;
        }

        private static string CreateNotHigherVersionErrorMessage(ModuleVersionData serverData, ModuleVersionData clientData)
        {
            List<ModModule> matchingServerMods = FindMods(serverData, clientData, (serverModule, clientModule) =>
            {
                return clientModule != null && ModModule.IsLowerVersion(clientModule, serverModule, serverModule.versionStrictness);
            }).ToList();

            if (matchingServerMods.Count == 0)
            {
                return string.Empty;
            }

            return ColoredText(Color.red, "Mod downgrades needed:", true) +
                   matchingServerMods.Aggregate("", (current, serverModule) => current + ColoredText(Color.white, $"Please downgrade mod {serverModule.name} to version {serverModule.GetVersionString()}", true)) +
                   Environment.NewLine;
        }

        private static string CreateAdditionalModsErrorMessage(ModuleVersionData serverData, ModuleVersionData clientData)
        {
            List<ModModule> matchingClientMods = FindMods(clientData, serverData, (clientModule, serverModule) =>
            {
                return clientModule.IsNeededOnServer() && serverModule == null;
            }).ToList();

            if (matchingClientMods.Count == 0)
            {
                return string.Empty;
            }

            return ColoredText(Color.red, "Additional mods loaded:", true) +
                   ColoredText(Color.white, $"Please contact your server admin or uninstall those mods", true) +
                   matchingClientMods.Aggregate("", (current, clientModule) => current + ColoredText(Color.white, $"{clientModule.name} {clientModule.version} was not loaded on the server", true)) +
                   Environment.NewLine;
        }

        private static IEnumerable<ModModule> FindMods(ModuleVersionData baseModules, ModuleVersionData additionalModules, Func<ModModule, ModModule, bool> predicate)
        {
            foreach (ModModule baseModule in baseModules.Modules)
            {
                ModModule additionalModule = additionalModules.FindModule(baseModule.name);

                if (predicate(baseModule, additionalModule))
                {
                    yield return baseModule;
                }
            }
        }

        /// <summary>
        ///     Get module.
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<ModModule> GetEnforcableMods()
        {
            foreach (var plugin in BepInExUtils.GetDependentPlugins(true).OrderBy(x => x.Key))
            {
                var networkCompatibilityAttribute = plugin.Value.GetType()
                    .GetCustomAttributes(typeof(NetworkCompatibilityAttribute), true)
                    .Cast<NetworkCompatibilityAttribute>()
                    .FirstOrDefault();
                if (networkCompatibilityAttribute != null)
                {
                    yield return new ModModule(plugin.Value.Info.Metadata, networkCompatibilityAttribute);
                }
            }
        }

        private static string ColoredText(Color color, string inner, bool insertNewLine)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{inner}</color>" + (insertNewLine ? Environment.NewLine : string.Empty);
        }
    }
}
