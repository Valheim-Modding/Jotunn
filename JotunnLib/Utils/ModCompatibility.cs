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
            // Register our RPC very early
            peer.m_rpc.Register<ZPackage>(nameof(RPC_Jotunn_ReceiveVersionData), RPC_Jotunn_ReceiveVersionData);
        }

        // Send client module list to server
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_ClientHandshake)), HarmonyPrefix, HarmonyPriority(Priority.Last)]
        private static void ZNet_RPC_ClientHandshake(ZNet __instance, ZRpc rpc)
        {
            rpc.Invoke(nameof(RPC_Jotunn_ReceiveVersionData), new ModuleVersionData(GetEnforcableMods().ToList()).ToZPackage());
        }

        // Send server module list to client
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_ServerHandshake)), HarmonyPrefix, HarmonyPriority(Priority.Last)]
        private static void ZNet_RPC_ServerHandshake(ZNet __instance, ZRpc rpc)
        {
            rpc.Invoke(nameof(RPC_Jotunn_ReceiveVersionData), new ModuleVersionData(GetEnforcableMods().ToList()).ToZPackage());
        }

        // Show mod compatibility error message when needed
        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.ShowConnectError)), HarmonyPostfix, HarmonyPriority(Priority.First)]
        private static void FejdStartup_ShowConnectError(FejdStartup __instance)
        {
            if (LastServerVersion != null && ZNet.m_connectionStatus == ZNet.ConnectionStatus.ErrorVersion)
            {
                ShowModCompatibilityErrorMessage();
                __instance.m_connectionFailedPanel.SetActive(false);
            }
        }

        // Hook client sending of PeerInfo
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.SendPeerInfo)), HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static void ZNet_SendPeerInfo(ZNet __instance, ZRpc rpc, string password)
        {
            if (ZNet.instance.IsClientInstance())
            {
                // If there was no server version response, Jötunn is not installed. Cancel if we have mandatory mods
                if (LastServerVersion == null &&
                    GetEnforcableMods().Any(x => x.Item3 == CompatibilityLevel.EveryoneMustHaveMod || x.Item3 == CompatibilityLevel.ServerMustHaveMod))
                {
                    rpc.Invoke("Disconnect");
                    LastServerVersion =
                        new ModuleVersionData(new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>()).ToZPackage();
                    ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorVersion;
                    return;
                }

                // If we got this far, clear lastServerVersion again
                LastServerVersion = null;
            }
        }

        // Hook RPC_PeerInfo to check in front of the original method
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo)), HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static void ZNet_RPC_PeerInfo(ZNet __instance, ZRpc rpc, ZPackage pkg)
        {
            if (!ZNet.instance.IsClientInstance())
            {
                // Vanilla client trying to connect?
                if (!ClientVersions.ContainsKey(rpc.GetSocket().GetEndPointString()))
                {
                    // Check mods, if there are some installed on the server which need also to be on the client
                    if (GetEnforcableMods().Any(x => x.Item3 == CompatibilityLevel.EveryoneMustHaveMod || x.Item3 == CompatibilityLevel.ClientMustHaveMod))
                    {
                        // There is a mod, which needs to be client side too
                        // Lets disconnect the vanilla client with Incompatible Version message

                        rpc.Invoke("Error", 3);
                        return;
                    }
                }
            }
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
                    sender.Invoke("Error", 3);
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
            foreach (var serverModule in serverData.Modules.Where(x => x.Item3 == CompatibilityLevel.EveryoneMustHaveMod || x.Item3 == CompatibilityLevel.ClientMustHaveMod))
            {
                if (!clientData.Modules.Any(x => x.Item1 == serverModule.Item1 && x.Item3 == serverModule.Item3))
                {
                    return false;
                }
            }

            // Check client enforced mods
            foreach (var clientModule in clientData.Modules.Where(x => x.Item3 == CompatibilityLevel.EveryoneMustHaveMod || x.Item3 == CompatibilityLevel.ServerMustHaveMod))
            {
                if (!serverData.Modules.Any(x => x.Item1 == clientModule.Item1 && x.Item3 == clientModule.Item3))
                {
                    return false;
                }
            }

            // Compare modules
            foreach (var serverModule in serverData.Modules)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (serverModule.Item3 == CompatibilityLevel.NoNeedForSync || serverModule.Item3 == CompatibilityLevel.NotEnforced)
                {
                    continue;
                }
#pragma warning restore CS0618 // Type or member is obsolete

                var clientModule = clientData.Modules.FirstOrDefault(x => x.Item1 == serverModule.Item1);

#pragma warning disable CS0618 // Type or member is obsolete
                if (clientModule == null &&
                    (serverModule.Item3 == CompatibilityLevel.OnlySyncWhenInstalled ||
                     serverModule.Item3 == CompatibilityLevel.VersionCheckOnly ||
                     serverModule.Item3 == CompatibilityLevel.ServerMustHaveMod))
                {
                    continue;
                }
#pragma warning restore CS0618 // Type or member is obsolete

                if (clientModule == null)
                {
                    return false;
                }

                if (serverModule.Item2.Major != clientModule.Item2.Major &&
                    (serverModule.Item4 >= VersionStrictness.Major || clientModule.Item4 >= VersionStrictness.Major))
                {
                    return false;
                }

                if (serverModule.Item2.Minor != clientModule.Item2.Minor &&
                    (serverModule.Item4 >= VersionStrictness.Minor || clientModule.Item4 >= VersionStrictness.Minor))
                {
                    return false;
                }

                if (serverModule.Item2.Build != clientModule.Item2.Build &&
                    (serverModule.Item4 >= VersionStrictness.Patch || clientModule.Item4 >= VersionStrictness.Patch))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Create and show mod compatibility error message
        /// </summary>
        private static void ShowModCompatibilityErrorMessage()
        {
            var panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), 700, 500);
            panel.SetActive(true);
            var remote = new ModuleVersionData(LastServerVersion);
            var local = new ModuleVersionData(GetEnforcableMods().ToList());


            var scroll = GUIManager.Instance.CreateScrollView(
                panel.transform, false, true, 8f, 10f, GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                new Color(0.1568628f, 0.1019608f, 0.0627451f, 1f), 650f, 400f);
            var scrolltf = scroll.GetComponent<RectTransform>();
            scrolltf.anchoredPosition = new Vector2(scrolltf.anchoredPosition.x, scrolltf.anchoredPosition.y + 15f);

            var tf = scrolltf.Find("Scroll View/Viewport/Content") as RectTransform;

            GUIManager.Instance.CreateText(
                "Remote version:", tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 19, GUIManager.Instance.ValheimOrange, true,
                new Color(0, 0, 0, 1), 600f, 40f, false);
            GUIManager.Instance.CreateText(
                remote.ToString(false), tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 19, Color.white, true,
                new Color(0, 0, 0, 1), 600f, 40f, false);
            GUIManager.Instance.CreateText(
                "Local version:", tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 19, GUIManager.Instance.ValheimOrange, true,
                new Color(0, 0, 0, 1), 600f, 40f, false);
            GUIManager.Instance.CreateText(
                local.ToString(false), tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 19, Color.white, true,
                new Color(0, 0, 0, 1), 600f, 40f, false);

            foreach (var part in CreateErrorMessage(remote, local))
            {
                GUIManager.Instance.CreateText(
                    part.Item2, tf, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                    GUIManager.Instance.AveriaSerifBold, 19, part.Item1, true,
                    new Color(0, 0, 0, 1), 600f, 40f, false);
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

            // And then each module
            foreach (var serverModule in serverData.Modules)
            {
                // Check first for missing modules on the client side
                if (serverModule.Item3 == CompatibilityLevel.EveryoneMustHaveMod || serverModule.Item3 == CompatibilityLevel.ClientMustHaveMod)
                {
                    if (clientData.Modules.All(x => x.Item1 != serverModule.Item1))
                    {
                        // client is missing needed module
                        yield return new Tuple<Color, string>(Color.red, "Missing mod:");
                        yield return new Tuple<Color, string>(Color.white, $"Please install mod {serverModule.Item1} v{serverModule.Item2}" + Environment.NewLine);
                        continue;
                    }

                    if (!clientData.Modules.Any(x => x.Item1 == serverModule.Item1 && x.Item3 == serverModule.Item3))
                    {
                        // module is there but mod compat level is lower
                        yield return new Tuple<Color, string>(Color.red, "Compatibility level mismatch:");
                        yield return new Tuple<Color, string>(Color.white, $"Please update mod {serverModule.Item1} version v{serverModule.Item2}." + Environment.NewLine);
                        continue;
                    }
                }

                // Then all version checks
                var clientModule = clientData.Modules.FirstOrDefault(x => x.Item1 == serverModule.Item1);

#pragma warning disable CS0618 // Type or member is obsolete
                if (clientModule == null && (serverModule.Item3 == CompatibilityLevel.NotEnforced || serverModule.Item3 == CompatibilityLevel.NoNeedForSync))
                {
                    continue;
                }
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable CS0618 // Type or member is obsolete
                if (clientModule == null &&
                    (serverModule.Item3 == CompatibilityLevel.OnlySyncWhenInstalled ||
                     serverModule.Item3 == CompatibilityLevel.VersionCheckOnly ||
                     serverModule.Item3 == CompatibilityLevel.ServerMustHaveMod))
                {
                    continue;
                }
#pragma warning restore CS0618 // Type or member is obsolete

                // Major
                if (serverModule.Item4 >= VersionStrictness.Major || clientModule.Item4 >= VersionStrictness.Major)
                {
                    if (serverModule.Item2.Major > clientModule.Item2.Major)
                    {
                        foreach (var messageLine in ClientVersionLowerMessage(serverModule))
                        {
                            yield return messageLine;
                        }

                        continue;
                    }

                    if (serverModule.Item2.Major < clientModule.Item2.Major)
                    {
                        foreach (var messageLine in ServerVersionLowerMessage(serverModule, clientModule))
                        {
                            yield return messageLine;
                        }

                        continue;
                    }

                    // Minor
                    if (serverModule.Item4 >= VersionStrictness.Minor || clientModule.Item4 >= VersionStrictness.Minor)
                    {
                        if (serverModule.Item2.Minor > clientModule.Item2.Minor)
                        {
                            foreach (var messageLine in ClientVersionLowerMessage(serverModule))
                            {
                                yield return messageLine;
                            }

                            continue;
                        }

                        if (serverModule.Item2.Minor < clientModule.Item2.Minor)
                        {
                            foreach (var messageLine in ServerVersionLowerMessage(serverModule, clientModule))
                            {
                                yield return messageLine;
                            }

                            continue;
                        }
                    }

                    // Patch
                    if (serverModule.Item4 >= VersionStrictness.Patch || clientModule.Item4 >= VersionStrictness.Patch)
                    {
                        if (serverModule.Item2.Build > clientModule.Item2.Build)
                        {
                            foreach (var messageLine in ClientVersionLowerMessage(serverModule))
                            {
                                yield return messageLine;
                            }

                            continue;
                        }

                        if (serverModule.Item2.Build < clientModule.Item2.Build)
                        {
                            foreach (var messageLine in ServerVersionLowerMessage(serverModule, clientModule))
                            {
                                yield return messageLine;
                            }
                        }
                    }
                }
            }

            // Now lets find additional modules with NetworkCompatibility attribute in the client's list
            foreach (var clientModule in clientData.Modules.Where(x => x.Item3 == CompatibilityLevel.EveryoneMustHaveMod || x.Item3 == CompatibilityLevel.ServerMustHaveMod))
            {
                if (serverData.Modules.All(x => x.Item1 != clientModule.Item1))
                {
                    yield return new Tuple<Color, string>(Color.red, "Additional mod detected:");
                    yield return new Tuple<Color, string>(GUIManager.Instance.ValheimOrange,
                        $"Mod {clientModule.Item1} v{clientModule.Item2} is not installed on the server.");
                    yield return new Tuple<Color, string>(Color.white, "Please consider uninstalling this mod." + Environment.NewLine);
                    continue;
                }

                if (!serverData.Modules.Any(x => x.Item1 == clientModule.Item1 && x.Item3 == clientModule.Item3))
                {
                    yield return new Tuple<Color, string>(Color.red, "Compatibility level mismatch:");
                    yield return new Tuple<Color, string>(Color.white, $"Please update mod {clientModule.Item1} version v{clientModule.Item2} on the server." + Environment.NewLine);
                    continue;
                }
            }
        }

        /// <summary>
        ///     Generate message for client's mod version lower than server's version
        /// </summary>
        /// <param name="module">Module version data</param>
        /// <returns></returns>
        private static IEnumerable<Tuple<Color, string>> ClientVersionLowerMessage(Tuple<string, System.Version, CompatibilityLevel, VersionStrictness> module)
        {
            yield return new Tuple<Color, string>(Color.red, "Mod update needed:");
            yield return new Tuple<Color, string>(Color.white, $"Please update mod {module.Item1} to version v{module.Item2}." + Environment.NewLine);
        }

        /// <summary>
        ///     Generate message for server's mod version lower than client's version
        /// </summary>
        /// <param name="module">server module data</param>
        /// <param name="clientModule">client module data</param>
        /// <returns></returns>
        private static IEnumerable<Tuple<Color, string>> ServerVersionLowerMessage(Tuple<string, System.Version, CompatibilityLevel, VersionStrictness> module,
            Tuple<string, System.Version, CompatibilityLevel, VersionStrictness> clientModule)
        {
            yield return new Tuple<Color, string>(Color.red, "Module version mismatch:");
            yield return new Tuple<Color, string>(GUIManager.Instance.ValheimOrange, $"Server has mod {module.Item1} v{module.Item2} installed.");
            yield return new Tuple<Color, string>(GUIManager.Instance.ValheimOrange,
                $"You have a higher version (v{clientModule.Item2}) of this mod installed.");
            yield return new Tuple<Color, string>(Color.white,
                "Please contact the server admin to update or downgrade the mod on your client." + Environment.NewLine);
        }

        /// <summary>
        ///     Get module.
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>> GetEnforcableMods()
        {
            foreach (var plugin in BepInExUtils.GetDependentPlugins(true).OrderBy(x => x.Key))
            {
                var networkCompatibilityAttribute = plugin.Value.GetType()
                    .GetCustomAttributes(typeof(NetworkCompatibilityAttribute), true)
                    .Cast<NetworkCompatibilityAttribute>()
                    .FirstOrDefault();
                if (networkCompatibilityAttribute != null)
                {
                    yield return new Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>(
                        plugin.Value.Info.Metadata.Name,
                        plugin.Value.Info.Metadata.Version,
                        networkCompatibilityAttribute.EnforceModOnClients,
                        networkCompatibilityAttribute.EnforceSameVersion);
                }
            }
        }

        /// <summary>
        ///     Deserialize version string into a usable format.
        /// </summary>
        internal class ModuleVersionData
        {
            /// <summary>
            ///     Create from module data
            /// </summary>
            /// <param name="versionData"></param>
            internal ModuleVersionData(List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>> versionData)
            {
                ValheimVersion = new System.Version(Version.m_major, Version.m_minor, Version.m_patch);
                Modules = new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>();
                Modules.AddRange(versionData);
            }

            internal ModuleVersionData(System.Version valheimVersion, List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>> versionData)
            {
                ValheimVersion = valheimVersion;
                Modules = new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>();
                Modules.AddRange(versionData);
            }

            /// <summary>
            ///     Create from ZPackage
            /// </summary>
            /// <param name="pkg"></param>
            internal ModuleVersionData(ZPackage pkg)
            {
                try
                {
                    // Needed !!
                    pkg.SetPos(0);
                    ValheimVersion = new System.Version(pkg.ReadInt(), pkg.ReadInt(), pkg.ReadInt());

                    var numberOfModules = pkg.ReadInt();

                    while (numberOfModules > 0)
                    {
                        Modules.Add(new Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>(pkg.ReadString(),
                            new System.Version(pkg.ReadInt(), pkg.ReadInt(), pkg.ReadInt()), (CompatibilityLevel)pkg.ReadInt(),
                            (VersionStrictness)pkg.ReadInt()));
                        numberOfModules--;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Could not deserialize version message data from zPackage");
                    Logger.LogError(ex.Message);
                }
            }

            /// <summary>
            ///     Valheim version
            /// </summary>
            public System.Version ValheimVersion { get; }

            /// <summary>
            ///     Module data
            /// </summary>
            public List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>> Modules { get; } =
                new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>();


            /// <summary>
            ///     Create ZPackage
            /// </summary>
            /// <returns>ZPackage</returns>
            public ZPackage ToZPackage()
            {
                var pkg = new ZPackage();
                pkg.Write(ValheimVersion.Major);
                pkg.Write(ValheimVersion.Minor);
                pkg.Write(ValheimVersion.Build);

                pkg.Write(Modules.Count);

                foreach (var module in Modules)
                {
                    pkg.Write(module.Item1);
                    pkg.Write(module.Item2.Major);
                    pkg.Write(module.Item2.Minor);
                    pkg.Write(module.Item2.Build);
                    pkg.Write((int)module.Item3);
                    pkg.Write((int)module.Item4);
                }

                return pkg;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    return ((ValheimVersion != null ? ValheimVersion.GetHashCode() : 0) * 397) ^ (Modules != null ? Modules.GetHashCode() : 0);
                }
            }

            // Default ToString override
            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Valheim {ValheimVersion.Major}.{ValheimVersion.Minor}.{ValheimVersion.Build}");

                foreach (var mod in Modules)
                {
                    sb.AppendLine($"{mod.Item1} {mod.Item2.Major}.{mod.Item2.Minor}.{mod.Item2.Build} {mod.Item3} {mod.Item4}");
                }

                return sb.ToString();
            }

            // Additional ToString method to show data without NetworkCompatibility attribute
            public string ToString(bool showEnforce)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Valheim {ValheimVersion.Major}.{ValheimVersion.Minor}.{ValheimVersion.Build}");

                foreach (var mod in Modules)
                {
                    sb.AppendLine($"{mod.Item1} {mod.Item2.Major}.{mod.Item2.Minor}.{mod.Item2.Build}" + (showEnforce ? " {mod.Item3} {mod.Item4}" : ""));
                }

                return sb.ToString();
            }
        }
    }
}
