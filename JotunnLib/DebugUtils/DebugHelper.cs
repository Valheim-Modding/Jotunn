using System;
using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jotunn.DebugUtils
{
    internal class DebugHelper : MonoBehaviour
    {
        private const string jtn = @"<mspace=10px>
 __/\\\__  __/\\\___  __/\\\__  ___ /\\\   _/\\\___   _/\\\___   
(_    _))(_     _))(__  __))/  //\ \\\ (_      ))(_      )) 
  \  \\\   /  _  \\\   /  \\\  \:.\\\_\ \\\ /  :   \\\ /  :   \\\ 
/\/ .:\\\ /:.(_)) \\\ /:.  \\\  \  :.  ///:. |   ///:. |   // 
\__  _// \  _____// \__  // (_   ___))\___|  // \___|  //  
   \//    \//          \//    \//          \//       \//   
                                            DEBUG MÖDE
</mspace>";

        private static DebugHelper instance;

        private void Awake()
        {
            instance = this;

            Main.RootObject.AddComponent<Eraser>();
            Main.RootObject.AddComponent<DebugInfo>();
            Main.RootObject.AddComponent<HoverInfo>();
            Main.RootObject.AddComponent<UEInputBlocker>();
            Main.RootObject.AddComponent<ZNetDiddelybug>();

            Main.Harmony.PatchAll(typeof(Patches));
            Main.Harmony.PatchAll(typeof(Debug_isDebugBuild));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(Terminal.ConsoleCommand), nameof(Terminal.ConsoleCommand.IsValid)), HarmonyPostfix]
            private static void Terminal_ConsoleCommand_IsValid(ref bool __result) => __result = true;

            [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_ClientHandshake)), HarmonyPrefix]
            private static void ProvidePasswordPatch(ZNet __instance, ZRpc rpc, bool needPassword) => instance.ProvidePasswordPatch(__instance, rpc, needPassword);

            [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SpawnLocation)), HarmonyPrefix]
            private static void ZoneSystem_SpawnLocation(ZoneSystem.ZoneLocation location, ZoneSystem.SpawnMode mode) => instance.ZoneSystem_SpawnLocation(location, mode);

            [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned)), HarmonyPrefix]
            private static void Player_OnSpawned_Prefix(Player __instance) => __instance.m_firstSpawn = false;

            [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned)), HarmonyPostfix]
            private static void Player_OnSpawned_Postfix() => instance.Player_OnSpawned_Postfix();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            { // Set a breakpoint here to break on F6 key press
            }
        }

        private void OnGUI()
        {
            // Display version in main menu
            if (SceneManager.GetActiveScene().name == "start")
            {
                UnityEngine.GUI.Label(new Rect(Screen.width - 100, 5, 100, 25), "Jötunn v" + Main.Version);
            }
        }

        private void Player_OnSpawned_Postfix()
        {
            Character.s_dpsDebugEnabled = true;
            Player.m_debugMode = true;
            Terminal.m_cheat = true;
            Console.instance.m_autoCompleteSecrets = true;
            Console.instance.updateCommandList();
            Console.instance.Print(jtn);
        }

        private void ProvidePasswordPatch(ZNet self, ZRpc rpc, bool needPassword)
        {
            if (Environment.GetCommandLineArgs().Any(x => x.ToLower() == "+password"))
            {
                var args = Environment.GetCommandLineArgs();

                // find password argument index
                var index = 0;
                while (index < args.Length && args[index].ToLower() != "+password")
                {
                    index++;
                }

                index++;

                // is there a password after +password?
                if (index < args.Length)
                {
                    // do normal handshake
                    self.m_connectingDialog.gameObject.SetActive(false);
                    self.SendPeerInfo(rpc, args[index]);
                    return;
                }
            }
        }

        /// <summary>
        ///     Output custom location spawns
        /// </summary>
        private void ZoneSystem_SpawnLocation(ZoneSystem.ZoneLocation location, ZoneSystem.SpawnMode mode)
        {
            if (ZoneManager.Instance.Locations.ContainsKey(location.m_prefabName))
            {
                Logger.LogDebug($"spawned {location.m_prefabName}, mode: {mode}");
            }
        }

        /// <summary>
        ///     Pretend to be a debugBuild :)
        /// </summary>
        [HarmonyPatch(typeof(Debug), "get_isDebugBuild")]
        private static class Debug_isDebugBuild
        {
            private static bool Prefix(Debug __instance, ref bool __result)
            {
                __result = true;
                return false;
            }
        }
    }
}
