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
    public class ModCompatibility
    {
        /// <summary>
        ///     Stores the last server message.
        /// </summary>
        private static ZPackage lastServerVersion;

        private static On.ZNet.orig_SendPeerInfo originalZNetSendPeerInfo;
        private static string lastPassword = "";

        [PatchInit(-1000)]
        public static void InitPatch()
        {
            On.ZNet.RPC_PeerInfo += ZNet_RPC_PeerInfo;
            On.ZNet.SendPeerInfo += ZNet_SendPeerInfo;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            // Show message box if there is a message to show
            if (lastServerVersion != null && scene.name == "start")
            {
                ShowModCompatibilityErrorMessage();
            }
        }

        // Hook SendPeerInfo on client to add our method to the rpc
        private static void ZNet_SendPeerInfo(On.ZNet.orig_SendPeerInfo orig, ZNet self, ZRpc rpc, string password)
        {
            // Only client needs to register this one
            rpc.Register(nameof(RPC_Jotunn_ReceiveServerVersionData), new Action<ZRpc, ZPackage>(RPC_Jotunn_ReceiveServerVersionData));

            On.ZRpc.Invoke += AppendPackage;
            orig(self, rpc, password);
            On.ZRpc.Invoke -= AppendPackage;
        }

        // Append our version data package to the existing zPackage
        private static void AppendPackage(On.ZRpc.orig_Invoke orig, ZRpc self, string method, object[] parameters)
        {
            var pkg = (ZPackage)parameters[0];
            pkg.Write(new ModuleVersionData(GetEnforcableMods().ToList()).ToZPackage());
            orig(self, method, parameters);
        }

        // Hook RPC_PeerInfo to check in front of the original method
        private static void ZNet_RPC_PeerInfo(On.ZNet.orig_RPC_PeerInfo orig, ZNet self, ZRpc rpc, ZPackage pkg)
        {
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                pkg.ReadLong();
                pkg.ReadString();
                pkg.ReadVector3();
                pkg.ReadString();
                pkg.ReadString();
                pkg.ReadByteArray();

                try
                {
                    var appended = new ZPackage(pkg.ReadByteArray());

                    var clientVersion = new ModuleVersionData(appended);
                    var serverVersion = new ModuleVersionData(GetEnforcableMods().ToList());

                    if (!clientVersion.Equals(serverVersion))
                    {
                        rpc.Invoke(nameof(RPC_Jotunn_ReceiveServerVersionData), serverVersion.ToZPackage());

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

                // Reset the stream
                pkg.m_reader.BaseStream.Position = 0;
            }

            // call original method
            orig(self, rpc, pkg);
        }

        /// <summary>
        ///     Create and show mod compatibility error message
        /// </summary>
        private static void ShowModCompatibilityErrorMessage()
        {
            var panel = GUIManager.Instance.CreateWoodpanel(GUIManager.PixelFix.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
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

            var button = GUIManager.Instance.CreateButton("OK", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -215f));
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
            foreach (var module in client.Modules)
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
                var nca = plugin.Value.GetType().GetCustomAttributes(typeof(NetworkCompatibiltyAttribute), true).Cast<NetworkCompatibiltyAttribute>()
                    .FirstOrDefault();
                if (nca != null)
                {
                    yield return new Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>(plugin.Value.Info.Metadata.Name,
                        plugin.Value.Info.Metadata.Version, nca.EnforceModOnClients, nca.EnforceSameVersion);
                }
            }
        }

        /// <summary>
        ///     Store server's message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private static void RPC_Jotunn_ReceiveServerVersionData(ZRpc sender, ZPackage data)
        {
            Logger.LogDebug("Received version data from server");
            if (ZNet.instance.IsClientInstance())
            {
                var clientVersion = new ModuleVersionData(GetEnforcableMods().ToList());
                var serverVersion = new ModuleVersionData(data);

                if (!clientVersion.Equals(serverVersion))
                {
                    // Prepare to show error message on screen after scene load
                    lastServerVersion = data;

                    // Reset it's stream position
                    data.m_reader.BaseStream.Position = 0;
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
                catch (Exception)
                {
                    Logger.LogError("Could not deserialize version message data from zPackage");
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
                    var otherModule = other.Modules.FirstOrDefault(x => x.Item1 == module.Item1);
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

                // Check the other way around too
                foreach (var module in other.Modules)
                {
                    var serverModule = Modules.FirstOrDefault(x => x.Item1 == module.Item1);
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
