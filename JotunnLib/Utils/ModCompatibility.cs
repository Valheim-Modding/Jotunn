using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Implementation of the mod compatibility features.
    /// </summary>
    public class ModCompatibility
    {
        /// <summary>
        ///     Stores the last server message.
        /// </summary>
        private static ZPackage lastServerVersion;

        private static Dictionary<string, ZPackage> clientVersions = new Dictionary<string, ZPackage>();

        /// <summary>
        ///     Initialize Patches
        /// </summary>
        [PatchInit(-1000)]
        public static void InitPatch()
        {
            On.ZNet.RPC_PeerInfo += ZNet_RPC_PeerInfo;
            On.ZNet.OnNewConnection += ZNet_OnNewConnection;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        /// <summary>
        ///     Initialize early running patches
        /// </summary>
        [PatchInit(int.MaxValue - 1000)]
        public static void InitPatchEarly()
        {
            On.ZNet.RPC_ClientHandshake += ZNet_RPC_ClientHandshake;
            On.ZNet.RPC_ServerHandshake += ZNet_RPC_ServerHandshake;
        }

        // Send client module list to server
        private static void ZNet_RPC_ClientHandshake(On.ZNet.orig_RPC_ClientHandshake orig, ZNet self, ZRpc rpc, bool needPassword)
        {
            rpc.Invoke(nameof(RPC_Jotunn_ReceiveVersionData), new ModuleVersionData(GetEnforcableMods().ToList()).ToZPackage());

            orig(self, rpc, needPassword);
        }

        // Send server module list to client
        private static void ZNet_RPC_ServerHandshake(On.ZNet.orig_RPC_ServerHandshake orig, ZNet self, ZRpc rpc)
        {
            rpc.Invoke(nameof(RPC_Jotunn_ReceiveVersionData), new ModuleVersionData(GetEnforcableMods().ToList()).ToZPackage());

            orig(self, rpc);
        }

        // Register our RPC
        private static void ZNet_OnNewConnection(On.ZNet.orig_OnNewConnection orig, ZNet self, ZNetPeer peer)
        {
            // Register our RPC very early
            peer.m_rpc.Register<ZPackage>(nameof(RPC_Jotunn_ReceiveVersionData), RPC_Jotunn_ReceiveVersionData);
            orig(self, peer);
        }

        // Show mod compatibility error message when needed
        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            // Show message box if there is a message to show
            if (lastServerVersion != null && scene.name == "start" && ZNet.m_connectionStatus == ZNet.ConnectionStatus.ErrorVersion)
            {
                ShowModCompatibilityErrorMessage();
            }
        }

        // Hook RPC_PeerInfo to check in front of the original method
        private static void ZNet_RPC_PeerInfo(On.ZNet.orig_RPC_PeerInfo orig, ZNet self, ZRpc rpc, ZPackage pkg)
        {
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                try
                {
                    var clientVersion = new ModuleVersionData(clientVersions[rpc.GetSocket().GetEndPointString()]);
                    var serverVersion = new ModuleVersionData(GetEnforcableMods().ToList());

                    // Remove from list
                    clientVersions.Remove(rpc.GetSocket().GetEndPointString());

                    // Compare and disconnect when not equal
                    if (!clientVersion.Equals(serverVersion))
                    {
                        rpc.Invoke("Error", 3);
                        return;
                    }
                }
                catch (EndOfStreamException)
                {
                    Logger.LogError("Reading beyond end of stream. Probably client without Jotunn tried to connect.");

                    // Client did not send appended package, just disconnect with the incompatible version error
                    rpc.Invoke("Error", 3);
                    return;
                }
                catch (KeyNotFoundException)
                {
                    // Vanilla client trying to connect?
                    // Check mods, if there are some installed on the server which need also to be on the client

                    if (GetEnforcableMods().Any(x => x.Item3 == CompatibilityLevel.EveryoneMustHaveMod))
                    {
                        // There is a mod, which needs to be client side too
                        // Lets disconnect the vanilla client with Incompatible Version message

                        rpc.Invoke("Error", 3);
                        return;
                    }
                }
            }
            else
            {
                // If we got this far on client side, clear lastServerVersion again
                lastServerVersion = null;
            }

            // call original method
            orig(self, rpc, pkg);
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
                clientVersions[sender.m_socket.GetEndPointString()] = data;
                var clientVersion = new ModuleVersionData(clientVersions[sender.GetSocket().GetEndPointString()]);
                var serverVersion = new ModuleVersionData(GetEnforcableMods().ToList());

                if (!clientVersion.Equals(serverVersion))
                {
                    // Disconnect if mods are not network compatible
                    sender.Invoke("Error", 3);
                }
            }
            else
            {
                lastServerVersion = data;
            }
        }

        /// <summary>
        ///     Create and show mod compatibility error message
        /// </summary>
        private static void ShowModCompatibilityErrorMessage()
        {
            var panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), 700, 500);
            panel.SetActive(true);
            var remote = new ModuleVersionData(lastServerVersion);
            var local = new ModuleVersionData(GetEnforcableMods().ToList());


            var scroll = GUIManager.Instance.CreateScrollView(panel.transform, false, true, 8f, 10f, GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                new Color(0.1568628f, 0.1019608f, 0.0627451f, 1f), 650f, 400f);

            scroll.SetActive(true);

            GUIManager.Instance.CreateText("Remote version:", scroll.transform.Find("Scroll View/Viewport/Content"), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 0), GUIManager.Instance.AveriaSerifBold, 19, GUIManager.Instance.ValheimOrange, true,
                new Color(0, 0, 0, 1), 600f, 40f, false);
            GUIManager.Instance.CreateText(remote.ToString(false), scroll.transform.Find("Scroll View/Viewport/Content"), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 0), GUIManager.Instance.AveriaSerifBold, 19, Color.white, true, new Color(0, 0, 0, 1), 600f, 40f,
                false);
            GUIManager.Instance.CreateText("Local version:", scroll.transform.Find("Scroll View/Viewport/Content"), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 0), GUIManager.Instance.AveriaSerifBold, 19, GUIManager.Instance.ValheimOrange, true,
                new Color(0, 0, 0, 1), 600f, 40f, false);
            GUIManager.Instance.CreateText(local.ToString(false), scroll.transform.Find("Scroll View/Viewport/Content"), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 0), GUIManager.Instance.AveriaSerifBold, 19, Color.white, true, new Color(0, 0, 0, 1), 600f, 40f,
                false);

            foreach (var part in CreateErrorMessage(remote, local))
            {
                GUIManager.Instance.CreateText(part.Item2, scroll.transform.Find("Scroll View/Viewport/Content"), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0, 0), GUIManager.Instance.AveriaSerifBold, 19, part.Item1, true, new Color(0, 0, 0, 1), 600f, 40f,
                    false);
            }

            scroll.transform.Find("Scroll View").GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;

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
            lastServerVersion = null;
        }

        /// <summary>
        ///     Create the error message(s) from the server and client message data
        /// </summary>
        /// <param name="server">server data</param>
        /// <param name="client">client data</param>
        /// <returns></returns>
        private static IEnumerable<Tuple<Color, string>> CreateErrorMessage(ModuleVersionData server, ModuleVersionData client)
        {
            // Check Valheim version first
            if (server.ValheimVersion != client.ValheimVersion)
            {
                yield return new Tuple<Color, string>(Color.red, "Valheim version error:");
                if (server.ValheimVersion > client.ValheimVersion)
                {
                    yield return new Tuple<Color, string>(Color.white, $"Please update your client to version {server.ValheimVersion}");
                }

                if (server.ValheimVersion < client.ValheimVersion)
                {
                    yield return new Tuple<Color, string>(Color.white,
                        $"The server you tried to connect runs {server.ValheimVersion}, which is lower than your version ({client.ValheimVersion})");
                    yield return new Tuple<Color, string>(Color.white, "Please contact the server admin for a server update." + Environment.NewLine);
                }
            }

            // And then each module
            foreach (var module in server.Modules)
            {
                // Check first for missing modules on the client side
                if (module.Item3 == CompatibilityLevel.EveryoneMustHaveMod)
                {
                    if (client.Modules.All(x => x.Item1 != module.Item1))
                    {
                        // client is missing needed module
                        yield return new Tuple<Color, string>(Color.red, "Missing mod:");
                        yield return new Tuple<Color, string>(Color.white, $"Please install mod {module.Item1} v{module.Item2}" + Environment.NewLine);
                        continue;
                    }
                }

                // Then all version checks
                var clientModule = client.Modules.First(x => x.Item1 == module.Item1);

                // Major
                if (module.Item4 >= VersionStrictness.Major || clientModule.Item4 >= VersionStrictness.Major)
                {
                    if (module.Item2.Major > clientModule.Item2.Major)
                    {
                        foreach (var messageLine in ClientVersionLowerMessage(module))
                        {
                            yield return messageLine;
                        }

                        continue;
                    }

                    if (module.Item2.Major < clientModule.Item2.Major)
                    {
                        foreach (var messageLine in ServerVersionLowerMessage(module, clientModule))
                        {
                            yield return messageLine;
                        }

                        continue;
                    }

                    // Minor
                    if (module.Item4 >= VersionStrictness.Minor || clientModule.Item4 >= VersionStrictness.Minor)
                    {
                        if (module.Item2.Minor > clientModule.Item2.Minor)
                        {
                            foreach (var messageLine in ClientVersionLowerMessage(module))
                            {
                                yield return messageLine;
                            }

                            continue;
                        }

                        if (module.Item2.Minor < clientModule.Item2.Minor)
                        {
                            foreach (var messageLine in ServerVersionLowerMessage(module, clientModule))
                            {
                                yield return messageLine;
                            }

                            continue;
                        }
                    }

                    // Patch
                    if (module.Item4 >= VersionStrictness.Patch || clientModule.Item4 >= VersionStrictness.Patch)
                    {
                        if (module.Item2.Build > clientModule.Item2.Build)
                        {
                            foreach (var messageLine in ClientVersionLowerMessage(module))
                            {
                                yield return messageLine;
                            }

                            continue;
                        }

                        if (module.Item2.Build < clientModule.Item2.Build)
                        {
                            foreach (var messageLine in ServerVersionLowerMessage(module, clientModule))
                            {
                                yield return messageLine;
                            }
                        }
                    }
                }
            }

            // Now lets find additional modules with NetworkCompatibility attribute in the client's list
            foreach (var module in client.Modules.Where(x => x.Item3 == CompatibilityLevel.EveryoneMustHaveMod))
            {
                if (server.Modules.All(x => x.Item1 != module.Item1))
                {
                    yield return new Tuple<Color, string>(Color.red, "Additional mod detected:");
                    yield return new Tuple<Color, string>(GUIManager.Instance.ValheimOrange,
                        $"Mod {module.Item1} v{module.Item2} is not installed on the server.");
                    yield return new Tuple<Color, string>(Color.white, "Please consider uninstalling this mod." + Environment.NewLine);
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
                var networkCompatibilityAttribute = plugin.Value.GetType().GetCustomAttributes(typeof(NetworkCompatibilityAttribute), true).Cast<NetworkCompatibilityAttribute>()
                    .FirstOrDefault();
                if (networkCompatibilityAttribute != null)
                {
                    yield return new Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>(plugin.Value.Info.Metadata.Name,
                        plugin.Value.Info.Metadata.Version, networkCompatibilityAttribute.EnforceModOnClients, networkCompatibilityAttribute.EnforceSameVersion);
                }
            }
        }

        /// <summary>
        ///     Deserialize version string into a usable format.
        /// </summary>
        private class ModuleVersionData : IEquatable<ModuleVersionData>
        {
            /// <summary>
            ///     Create from module data
            /// </summary>
            /// <param name="versionData"></param>
            public ModuleVersionData(List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>> versionData)
            {
                ValheimVersion = new System.Version(Version.m_major, Version.m_minor, Version.m_patch);
                Modules = new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>();
                Modules.AddRange(versionData);
            }

            /// <summary>
            ///     Create from ZPackage
            /// </summary>
            /// <param name="pkg"></param>
            public ModuleVersionData(ZPackage pkg)
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


            /// <inheritdoc />
            public bool Equals(ModuleVersionData other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (!Equals(ValheimVersion, other.ValheimVersion))
                {
                    return false;
                }

                // Check if server modules exist on client side 
                foreach (var module in Modules)
                {
                    if (module.Item3 == CompatibilityLevel.NoNeedForSync)
                    {
                        continue;
                    }

                    var otherModule = other.Modules.FirstOrDefault(x => x.Item1 == module.Item1);

                    if (otherModule == null && module.Item3 == CompatibilityLevel.OnlySyncWhenInstalled)
                    {
                        continue;
                    }

                    if (otherModule == null)
                    {
                        return false;
                    }

                    if (module.Item2.Major != otherModule.Item2.Major &&
                        (module.Item4 >= VersionStrictness.Major || otherModule.Item4 >= VersionStrictness.Major))
                    {
                        return false;
                    }

                    if (module.Item2.Minor != otherModule.Item2.Minor &&
                        (module.Item4 >= VersionStrictness.Minor || otherModule.Item4 >= VersionStrictness.Minor))
                    {
                        return false;
                    }

                    if (module.Item2.Build != otherModule.Item2.Build &&
                        (module.Item4 >= VersionStrictness.Patch || otherModule.Item4 >= VersionStrictness.Patch))
                    {
                        return false;
                    }
                }

                // Check if client modules exist on server side
                foreach (var module in other.Modules)
                {
                    if (module.Item3 == CompatibilityLevel.NoNeedForSync)
                    {
                        continue;
                    }

                    var serverModule = Modules.FirstOrDefault(x => x.Item1 == module.Item1);

                    if (serverModule == null && module.Item3 == CompatibilityLevel.OnlySyncWhenInstalled)
                    {
                        continue;
                    }

                    if (serverModule == null)
                    {
                        return false;
                    }

                    if (module.Item2.Major != serverModule.Item2.Major &&
                        (module.Item4 >= VersionStrictness.Major || serverModule.Item4 >= VersionStrictness.Major))
                    {
                        return false;
                    }

                    if (module.Item2.Minor != serverModule.Item2.Minor &&
                        (module.Item4 >= VersionStrictness.Minor || serverModule.Item4 >= VersionStrictness.Minor))
                    {
                        return false;
                    }

                    if (module.Item2.Build != serverModule.Item2.Build &&
                        (module.Item4 >= VersionStrictness.Patch || serverModule.Item4 >= VersionStrictness.Patch))
                    {
                        return false;
                    }
                }

                return true;
            }

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
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != GetType())
                {
                    return false;
                }

                return Equals((ModuleVersionData)obj);
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
