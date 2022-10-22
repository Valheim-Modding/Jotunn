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

            if (!Equals(serverData.ValheimVersion, clientData.ValheimVersion))
            {
                return false;
            }

            // Check server enforced mods
            if (serverData.Modules.Any(serverModule => serverModule.IsNeededOnClient() && !clientData.HasModule(serverModule.name)))
            {
                return false;
            }

            // Check client enforced mods
            if (clientData.Modules.Any(clientModule => clientModule.IsNeededOnServer() && !serverData.HasModule(clientModule.name)))
            {
                return false;
            }

            // Compare modules
            foreach (var serverModule in serverData.Modules)
            {
                if (serverModule.IsNotEnforced())
                {
                    continue;
                }

                var clientModule = clientData.FindModule(serverModule.name);

                if (clientModule == null && serverModule.OnlyVersionCheck() || !serverModule.IsNeededOnClient())
                {
                    continue;
                }

                if (clientModule == null)
                {
                    return false;
                }

                if (ModModule.IsLowerVersion(serverModule, clientModule, serverModule.versionStrictness))
                {
                    return false;
                }

                if (ModModule.IsLowerVersion(clientModule, serverModule, serverModule.versionStrictness))
                {
                    return false;
                }
            }

            return true;
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

            StringBuilder sb = new StringBuilder();
            foreach (var part in CreateErrorMessage(remote, local))
            {
                sb.Append(part);
                sb.Append(Environment.NewLine);
            }

            compatWindow.failedConnection.text = ColoredText(GUIManager.Instance.ValheimOrange, "Failed connection:", true) +
                                                 failedConnectionText.Trim();
            compatWindow.localVersion.text = ColoredText(GUIManager.Instance.ValheimOrange, "Local Version:", true) +
                                             local.ToString(false).Trim();
            compatWindow.remoteVersion.text = ColoredText(GUIManager.Instance.ValheimOrange, "Remote Version:", true) +
                                              remote.ToString(false).Trim();
            compatWindow.errorMessages.text = sb.ToString().Trim();

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
        private static IEnumerable<string> CreateErrorMessage(ModuleVersionData serverData, ModuleVersionData clientData)
        {
            // Check Valheim version first
            if (serverData.ValheimVersion != clientData.ValheimVersion)
            {
                yield return ColoredText(Color.red, "Valheim version error:", false);
                if (serverData.ValheimVersion > clientData.ValheimVersion)
                {
                    yield return ColoredText(Color.white, $"Please update your client to version {serverData.ValheimVersion}", true);
                }

                if (serverData.ValheimVersion < clientData.ValheimVersion)
                {
                    yield return ColoredText(Color.white, $"The server you tried to connect runs {serverData.ValheimVersion}, which is lower than your version ({clientData.ValheimVersion})", false);
                    yield return ColoredText(Color.white, $"Please contact the server admin for a server update.", true);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(serverData.VersionString) && serverData.VersionString != clientData.VersionString)
                {
                    yield return ColoredText(Color.red, "Valheim modded version string mismatch:", false);
                    yield return ColoredText(Color.white, $"Local: {clientData.VersionString}", false);
                    yield return ColoredText(Color.white, $"Remote: {serverData.VersionString}", true);
                }
            }

            // And then each module
            foreach (var serverModule in serverData.Modules)
            {
                if (serverModule.IsNotEnforced())
                {
                    continue;
                }

                // Check first for missing modules on the client side
                if (serverModule.IsNeededOnClient() && !clientData.HasModule(serverModule.name))
                {
                    yield return ColoredText(Color.red, "Missing mod:", false);
                    yield return ColoredText(Color.white, $"Please install mod {serverModule.name} v{serverModule.version}", true);
                    continue;
                }

                // Then all version checks
                var clientModule = clientData.FindModule(serverModule.name);

                if (clientModule == null)
                {
                    continue;
                }

                if (ModModule.IsLowerVersion(serverModule, clientModule, serverModule.versionStrictness))
                {
                    yield return ClientVersionLowerMessage(serverModule);
                }

                if (ModModule.IsLowerVersion(clientModule, serverModule, serverModule.versionStrictness))
                {
                    yield return ServerVersionLowerMessage(serverModule, clientModule);
                }
            }

            // Now lets find additional modules with NetworkCompatibility attribute in the client's list
            foreach (var clientModule in clientData.Modules.Where(x => x.IsNeededOnServer()))
            {
                if (serverData.Modules.All(x => x.name != clientModule.name))
                {
                    yield return ColoredText(Color.red, "Additional mod loaded:", false);
                    yield return ColoredText(GUIManager.Instance.ValheimOrange, $"Mod {clientModule.name} v{clientModule.version} was not loaded or is not installed on the server.", false);
                    yield return ColoredText(Color.white, $"Please contact your server admin or uninstall this mod", true);
                    continue;
                }
            }
        }

        /// <summary>
        ///     Generate message for client's mod version lower than server's version
        /// </summary>
        /// <param name="module">Module version data</param>
        /// <returns></returns>
        private static string ClientVersionLowerMessage(ModModule module)
        {
            return ColoredText(Color.red, "Mod update needed:", true) +
                   ColoredText(Color.white, $"Please update mod {module.name} to version v{module.GetVersionString()}", true);
        }

        /// <summary>
        ///     Generate message for server's mod version lower than client's version
        /// </summary>
        /// <param name="module">server module data</param>
        /// <param name="clientModule">client module data</param>
        /// <returns></returns>
        private static string ServerVersionLowerMessage(ModModule module, ModModule clientModule)
        {
            return ColoredText(Color.red, "Module version mismatch:", true) +
                   ColoredText(GUIManager.Instance.ValheimOrange, $"Server has mod {module.name} v{module.GetVersionString()} installed.", true) +
                   ColoredText(GUIManager.Instance.ValheimOrange, $"You have a higher version (v{clientModule.GetVersionString()}) of this mod installed.", true) +
                   ColoredText(Color.white, $"Please contact the server admin to update or downgrade the mod on your client", true);
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
