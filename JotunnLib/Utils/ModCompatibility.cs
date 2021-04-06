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
        private static string lastServerMessage = "";

        private static int getVersionTrigger = 0;

#if DEBUG
        internal static bool enableTestCase = false;
#endif

        [PatchInit(-1000)]
        public static void InitPatch()
        {
            On.Version.GetVersionString += Version_GetVersionString;
            On.ZNet.RPC_PeerInfo += ZNet_RPC_PeerInfo;
            On.ZNet.SendPeerInfo += ZNet_SendPeerInfo;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            // Show message box if there is a message to show
            if (!string.IsNullOrEmpty(lastServerMessage) && scene.name == "start")
            {
                var panel = GUIManager.Instance.CreateWoodpanel(GUIManager.PixelFix.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0f), 500, 500);
                panel.SetActive(true);

                var remote = new MessageData(lastServerMessage);
                var local = new MessageData(AddModuleVersions(Version.GetVersionString()));

                var text = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));

                var showText = "Remote version: " + Environment.NewLine + remote + Environment.NewLine + "Local version: " + Environment.NewLine + local;

                text.GetComponent<Text>().text = showText;
                text.GetComponent<Text>().color = new Color(1f, 0.631f, 0.235f, 1f);
                text.GetComponent<Text>().font = GUIManager.Instance.AveriaSerifBold;
                text.GetComponent<Text>().fontSize = 19;
                text.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 1);
                text.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                text.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                text.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 450);
                text.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 450);

                text.transform.SetParent(panel.transform, false);

                var button = GUIManager.Instance.CreateButton("OK", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -215f));
                button.SetActive(true);
                button.GetComponent<Button>().onClick.AddListener(() =>
                {
                    panel.SetActive(false);
                    Object.Destroy(panel);
                });

                lastServerMessage = "";
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

#if DEBUG
            if (enableTestCase)
            {
                valheimVersion += "!";
            }
#endif

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
                lastServerMessage = data;
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


#if DEBUG
        internal static void Config_SettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            if (e.ChangedSetting.Definition.Section == "ModCompatibilityTest" && e.ChangedSetting.Definition.Key == "Enable")
            {
                enableTestCase = (bool)e.ChangedSetting.BoxedValue;
            }
        }
#endif
    }
}