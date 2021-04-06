using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JotunnLib.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JotunnLib.Utils
{
    public class ModCompatibility
    {
        /// <summary>
        ///     Stores the last server message.
        /// </summary>
        private static string lastServerVersion = "";

        private static int getVersionTrigger = 0;

        [PatchInit(-1000)]
        public static void InitPatch()
        {
            On.Version.GetVersionString += Version_GetVersionString;
            On.ZNet.RPC_PeerInfo += ZNet_RPC_PeerInfo;
            On.ZNet.SendPeerInfo += ZNet_SendPeerInfo;
            On.ZNet.RPC_Error += ZNet_RPC_Error;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static void ZNet_RPC_Error(On.ZNet.orig_RPC_Error orig, ZNet self, ZRpc rpc, int error)
        {
            orig(self, rpc, error);
        }

        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            // Show message box if there is a message to show
            if (!string.IsNullOrEmpty(lastServerVersion) && scene.name == "start")
            {
                ShowModcompatibilityErrorMessage();
            }
        }

        private static void ShowModcompatibilityErrorMessage()
        {
            var panel = GUIManager.Instance.CreateWoodpanel(GUIManager.PixelFix.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0f), 700, 500);
            panel.SetActive(true);
            var remote = new MessageData(lastServerVersion);
            var local = new MessageData(AddModuleVersions(Version.GetVersionString()));

            var showText = "Remote version: " + Environment.NewLine + remote + Environment.NewLine + "Local version: " + Environment.NewLine + local;

            var scroll = GUIManager.Instance.CreateScrollView(panel.transform, false, true, 8f, 10f, GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                new Color(0.1568628f, 0.1019608f, 0.0627451f, 1f), 650f, 400f);

            scroll.SetActive(true);

            var text = GUIManager.Instance.CreateText(showText, scroll.transform.Find("Scroll View/Viewport/Content"), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0, 0), GUIManager.Instance.AveriaSerifBold, 19, GUIManager.Instance.ValheimOrange, true,
                new Color(0, 0, 0, 1), 600f, 40f, false);

            foreach (var part in CreateErrorMessage(remote, local))
            {
                GUIManager.Instance.CreateText(part.TrimStart('!'), scroll.transform.Find("Scroll View/Viewport/Content"), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0, 0), GUIManager.Instance.AveriaSerifBold, 19, part.StartsWith("!")?new Color(1,0,0,1):GUIManager.Instance.ValheimOrange, true,
                    new Color(0, 0, 0, 1), 600f, 40f, false);
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
            lastServerVersion = "";
        }

        /// <summary>
        /// Create the error message(s) from the server and client message data
        /// </summary>
        /// <param name="server">server data</param>
        /// <param name="client">client data</param>
        /// <returns></returns>
        private static IEnumerable<string> CreateErrorMessage(MessageData server, MessageData client)
        {
            // Check Valheim version first
            if (server.ValheimVersion != client.ValheimVersion)
            {
                yield return "!Valheim version error:";
                if (string.CompareOrdinal(server.ValheimVersion, client.ValheimVersion) > 0)
                {
                    yield return $"Please update your client to version {server.ValheimVersion}";
                }

                if (string.CompareOrdinal(server.ValheimVersion, client.ValheimVersion) < 0)
                {
                    yield return $"The server you tried to connect runs v{server.ValheimVersion}, which is lower than your version (v{client.ValheimVersion})";
                    yield return $"Please contact the server admin for a server update."+Environment.NewLine;
                }
            }

            // And then each module
            foreach (var module in server.Modules)
            {
                string mod = module.Contains(':') ? module.Split(':')[0] : module;
                string vers = module.Contains(':') ? module.Split(':')[1] : "";
                if (!client.Modules.Any(x => x.StartsWith(mod)))
                {
                    yield return $"!Missing mod:";
                    yield return $"Please install mod {mod} " + (string.IsNullOrEmpty(vers) ? "" : "v" + vers)+Environment.NewLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(vers))
                    {
                        string clientVersion = client.Modules.First(x => x.StartsWith(mod)).Split(':')[1];
                        int comparison = string.CompareOrdinal(vers, clientVersion);
                        if (comparison > 0)
                        {
                            yield return $"!Mod update needed:";
                            yield return $"Please update mod {mod} to version {vers}."+Environment.NewLine;
                        }

                        if (comparison < 0)
                        {
                            yield return $"Server has mod {mod} v{vers} installed.";
                            yield return $"You have a higher version ({clientVersion}) of this mod installed.";
                            yield return $"Please contact the server admin to update or downgrade the mod on your client."+Environment.NewLine;
                        }
                    }
                }
            }

            foreach (var module in client.Modules)
            {
                string mod = module.Contains(':') ? module.Split(':')[0] : module;
                string vers = module.Contains(':') ? module.Split(':')[1] : "";
                if (!server.Modules.Any(x => x.StartsWith(mod)))
                {
                    yield return $"!Additional mod detected:";
                    yield return $"Mod {mod}{(string.IsNullOrEmpty(vers) ? "" : " v" + vers)} is not installed on the server.";
                    yield return $"Please consider uninstalling this mod." + Environment.NewLine;
                }
            }
        }

        // Hook SendPeerInfo on client to add our method to the rpc
        private static void ZNet_SendPeerInfo(On.ZNet.orig_SendPeerInfo orig, ZNet self, ZRpc rpc, string password)
        {
            rpc.Register(nameof(RPC_JotunnLib_StoreServerMessage), new Action<ZRpc, string>(RPC_JotunnLib_StoreServerMessage));
            getVersionTrigger = 1;
            orig(self, rpc, password);
        }

        // Hook RPC_PeerInfo to check in front of the original method
        private static void ZNet_RPC_PeerInfo(On.ZNet.orig_RPC_PeerInfo orig, ZNet self, ZRpc rpc, ZPackage pkg)
        {
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                // Check if version is correct
                pkg.ReadLong();
                var vers = pkg.ReadString();

                // Reset package reader position
                pkg.m_reader.BaseStream.Position = 0;


                // Check version ourselves to be able to send back some data
                var serverVersion = Version.GetVersionString();
                serverVersion = AddModuleVersions(serverVersion);
                if (serverVersion != vers)
                {
                    rpc.Invoke(nameof(RPC_JotunnLib_StoreServerMessage), serverVersion);
                }
            }

            // Add module strings 2 times
            getVersionTrigger = 2;
            // call original method
            orig(self, rpc, pkg);
        }

        // Our own implementation of the GetVersionString
        private static string Version_GetVersionString(On.Version.orig_GetVersionString orig)
        {
            var valheimVersion = orig();

            if (getVersionTrigger > 0)
            {
                valheimVersion = AddModuleVersions(valheimVersion);
                getVersionTrigger--;
            }

            return valheimVersion;
        }


        /// <summary>
        ///     Add module versions to string.
        /// </summary>
        /// <param name="valheimVersion"></param>
        /// <returns></returns>
        private static string AddModuleVersions(string valheimVersion)
        {
            foreach (var mod in GetEnforcedMods())
            {
                if (mod.Item3 == CompatibilityLevel.EveryoneMustHaveMod)
                {
                    valheimVersion += $"@{mod.Item1}";
                    if (mod.Item4 == VersionStrictness.EveryoneNeedSameModVersion)
                    {
                        valheimVersion += $":{mod.Item2.Major}.{mod.Item2.Minor}.{mod.Item2.Build}";
                    }
                }
            }
            return valheimVersion;
        }

        /// <summary>
        ///     Get module.
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>> GetEnforcedMods()
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
        public static void RPC_JotunnLib_StoreServerMessage(ZRpc sender, string data)
        {
            if (ZNet.instance.IsClientInstance())
            {
                lastServerVersion = data;
            }
        }

        /// <summary>
        ///     Deserialize version string into a usable format.
        /// </summary>
        private class MessageData
        {
            public MessageData(string input)
            {
                try
                {
                    ValheimVersion = input.Split('@')[0];
                    var remaining = input.Substring(ValheimVersion.Length + 1);

                    var modules = remaining.Split('@');
                    foreach (var module in modules)
                    {
                        Modules.Add(module);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Could not deserialize version string '{input}'");
                }
            }

            public string ValheimVersion { get; }
            public List<string> Modules { get; } = new List<string>();

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine("Valheim: " + ValheimVersion);
                foreach (var module in Modules)
                {
                    var parts = module.Split(':');
                    sb.AppendLine(parts[0] + ": v" + parts[1]);
                }

                return sb.ToString();
            }
        }
    }
}