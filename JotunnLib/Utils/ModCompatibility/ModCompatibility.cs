using System;
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
                ShowModCompatibilityErrorMessage(failedConnectionText);
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
            if (serverData.Modules.Any(serverModule => serverModule.IsNeededOnClient() && !clientData.HasModule(serverModule.name))) {
                return false;
            }

            // Check client enforced mods
            if (clientData.Modules.Any(clientModule => clientModule.IsNeededOnServer() && !serverData.HasModule(clientModule.name))) {
                return false;
            }

            // Compare modules
            foreach (var serverModule in serverData.Modules)
            {
                if (!serverModule.IsEnforced())
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

        /// <summary>
        ///     Create and show mod compatibility error message
        /// </summary>
        private static void ShowModCompatibilityErrorMessage(string failedConnectionText)
        {
            const int panelWidth = 900;
            var panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), panelWidth, 500);
            panel.SetActive(true);
            var remote = new ModuleVersionData(LastServerVersion);
            var local = new ModuleVersionData(GetEnforcableMods().ToList());

            var scroll = GUIManager.Instance.CreateScrollView(
                panel.transform, false, true, 8f, 10f, GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                new Color(0.1568628f, 0.1019608f, 0.0627451f, 1f), panelWidth - 50f, 400f);
            var scrolltf = scroll.GetComponent<RectTransform>();
            scrolltf.anchoredPosition = new Vector2(scrolltf.anchoredPosition.x, scrolltf.anchoredPosition.y + 15f);

            var tf = scrolltf.Find("Scroll View/Viewport/Content") as RectTransform;

            // Show failed connection string
            GUIManager.Instance.CreateText(
                "Failed connection:", tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 19, GUIManager.Instance.ValheimOrange, true,
                new Color(0, 0, 0, 1), panelWidth - 100f, 40f, false);
            GUIManager.Instance.CreateText(
                failedConnectionText.Trim() + Environment.NewLine, tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 19, Color.white, true,
                new Color(0, 0, 0, 1), panelWidth - 100f, 40f, false);

            // list remote versions
            GUIManager.Instance.CreateText(
                "Remote version:", tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 19, GUIManager.Instance.ValheimOrange, true,
                new Color(0, 0, 0, 1), panelWidth - 100f, 40f, false);
            GUIManager.Instance.CreateText(
                remote.ToString(false), tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 19, Color.white, true,
                new Color(0, 0, 0, 1), panelWidth - 100f, 40f, false);

            // list local versions
            GUIManager.Instance.CreateText(
                "Local version:", tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 19, GUIManager.Instance.ValheimOrange, true,
                new Color(0, 0, 0, 1), panelWidth - 100f, 40f, false);
            GUIManager.Instance.CreateText(
                local.ToString(false), tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 19, Color.white, true,
                new Color(0, 0, 0, 1), panelWidth - 100f, 40f, false);

            foreach (var part in CreateErrorMessage(remote, local))
            {
                GUIManager.Instance.CreateText(
                    part.Item2, tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                    GUIManager.Instance.AveriaSerifBold, 19, part.Item1, true,
                    new Color(0, 0, 0, 1), panelWidth - 100f, 40f, false);
            }

            scroll.transform.Find("Scroll View").GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;

            scroll.SetActive(true);

            var button = GUIManager.Instance.CreateButton("OK", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -215f),
                100f, 40f);

            // Special condition, coming from ingame back into main scene
            button.GetComponent<Image>().pixelsPerUnitMultiplier = 2f;
            button.SetActive(true);

            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                panel.SetActive(false);
                Object.Destroy(panel);
            });

            // Reset the last server version
            LastServerVersion = null;
        }

        /// <summary>
        ///     Create the error message(s) from the server and client message data
        /// </summary>
        /// <param name="serverData">server data</param>
        /// <param name="clientData">client data</param>
        /// <returns></returns>
        private static IEnumerable<Tuple<Color, string>> CreateErrorMessage(ModuleVersionData serverData, ModuleVersionData clientData)
        {
            // Check Valheim version first
            if (serverData.ValheimVersion != clientData.ValheimVersion)
            {
                yield return new Tuple<Color, string>(Color.red, "Valheim version error:");
                if (serverData.ValheimVersion > clientData.ValheimVersion)
                {
                    yield return new Tuple<Color, string>(Color.white, $"Please update your client to version {serverData.ValheimVersion}");
                }

                if (serverData.ValheimVersion < clientData.ValheimVersion)
                {
                    yield return new Tuple<Color, string>(Color.white,
                        $"The server you tried to connect runs {serverData.ValheimVersion}, which is lower than your version ({clientData.ValheimVersion})");
                    yield return new Tuple<Color, string>(Color.white, "Please contact the server admin for a server update." + Environment.NewLine);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(serverData.VersionString) && serverData.VersionString != clientData.VersionString)
                {
                    yield return new Tuple<Color, string>(Color.red, "Valheim modded version string mismatch:");
                    yield return new Tuple<Color, string>(Color.white, $"Local: {clientData.VersionString}");
                    yield return new Tuple<Color, string>(Color.white, $"Remote: {serverData.VersionString}{Environment.NewLine}");
                }
            }

            // And then each module
            foreach (var serverModule in serverData.Modules)
            {
                if (!serverModule.IsEnforced())
                {
                    continue;
                }

                // Check first for missing modules on the client side
                if (serverModule.IsNeededOnClient() && !clientData.HasModule(serverModule.name))
                {
                    yield return new Tuple<Color, string>(Color.red, "Missing mod:");
                    yield return new Tuple<Color, string>(Color.white, $"Please install mod {serverModule.name} v{serverModule.version}" + Environment.NewLine);
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
                    foreach (var messageLine in ClientVersionLowerMessage(serverModule))
                    {
                        yield return messageLine;
                    }
                }

                if (ModModule.IsLowerVersion(clientModule, serverModule, serverModule.versionStrictness))
                {
                    foreach (var messageLine in ServerVersionLowerMessage(serverModule, clientModule))
                    {
                        yield return messageLine;
                    }
                }
            }

            // Now lets find additional modules with NetworkCompatibility attribute in the client's list
            foreach (var clientModule in clientData.Modules.Where(x => x.IsNeededOnServer()))
            {
                if (serverData.Modules.All(x => x.name != clientModule.name))
                {
                    yield return new Tuple<Color, string>(Color.red, "Additional mod loaded:");
                    yield return new Tuple<Color, string>(GUIManager.Instance.ValheimOrange, $"Mod {clientModule.name} v{clientModule.version} was not loaded or is not installed on the server.");
                    yield return new Tuple<Color, string>(Color.white, "Please contact your server admin or uninstall this mod." + Environment.NewLine);
                    continue;
                }
            }
        }

        /// <summary>
        ///     Generate message for client's mod version lower than server's version
        /// </summary>
        /// <param name="module">Module version data</param>
        /// <returns></returns>
        private static IEnumerable<Tuple<Color, string>> ClientVersionLowerMessage(ModModule module)
        {
            yield return new Tuple<Color, string>(Color.red, "Mod update needed:");
            yield return new Tuple<Color, string>(Color.white, $"Please update mod {module.name} to version v{module.GetVersionString()}." + Environment.NewLine);
        }

        /// <summary>
        ///     Generate message for server's mod version lower than client's version
        /// </summary>
        /// <param name="module">server module data</param>
        /// <param name="clientModule">client module data</param>
        /// <returns></returns>
        private static IEnumerable<Tuple<Color, string>> ServerVersionLowerMessage(ModModule module, ModModule clientModule)
        {
            yield return new Tuple<Color, string>(Color.red, "Module version mismatch:");
            yield return new Tuple<Color, string>(GUIManager.Instance.ValheimOrange, $"Server has mod {module.name} v{module.GetVersionString()} installed.");
            yield return new Tuple<Color, string>(GUIManager.Instance.ValheimOrange,
                $"You have a higher version (v{clientModule.GetVersionString()}) of this mod installed.");
            yield return new Tuple<Color, string>(Color.white,
                "Please contact the server admin to update or downgrade the mod on your client." + Environment.NewLine);
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
    }
}
