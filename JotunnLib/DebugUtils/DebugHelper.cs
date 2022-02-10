using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jotunn.DebugUtils
{
    internal class DebugHelper : MonoBehaviour
    {
        private const string jtn = @"
 __/\\__  __/\\___  __/\\__  ___ /\\   _/\\___   _/\\___   
(_    _))(_     _))(__  __))/  //\ \\ (_      ))(_      )) 
  \  \\   /  _  \\   /  \\  \:.\\_\ \\ /  :   \\ /  :   \\ 
/\/ .:\\ /:.(_)) \\ /:.  \\  \  :.  ///:. |   ///:. |   // 
\__  _// \  _____// \__  // (_   ___))\___|  // \___|  //  
   \//    \//          \//    \//          \//       \//   
                                            DEBUG MÖDE
";

        private void Awake()
        {
            Main.RootObject.AddComponent<Eraser>();
            Main.RootObject.AddComponent<DebugInfo>();
            Main.RootObject.AddComponent<HoverInfo>();
            Main.RootObject.AddComponent<UEInputBlocker>();
            Main.RootObject.AddComponent<ZNetDiddelybug>();

            On.Terminal.ConsoleCommand.IsValid += (orig, self, context, check) => true;
            
            On.Player.OnSpawned += (orig, self) =>
            {
                self.m_firstSpawn = false;
                orig(self);

                Character.m_dpsDebugEnabled = true;
                Player.m_debugMode = true;
                Terminal.m_cheat = true;
                Console.instance.m_autoCompleteSecrets = true;
                Console.instance.updateCommandList();
                try
                {
                    Font fnt = Font.CreateDynamicFontFromOSFont("Consolas", 14);
                    Console.instance.gameObject.GetComponentInChildren<Text>(true).font = fnt;
                    Console.instance.Print(jtn);
                }
                catch (Exception) { }
            };
            On.ZNet.RPC_ClientHandshake += ProvidePasswordPatch;
            On.ZoneSystem.SpawnLocation += ZoneSystem_SpawnLocation;
            Main.Harmony.PatchAll(typeof(Debug_isDebugBuild));
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
        private void ProvidePasswordPatch(On.ZNet.orig_RPC_ClientHandshake orig, ZNet self, ZRpc rpc, bool needPassword)
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

            orig(self, rpc, needPassword);
        }

        /// <summary>
        ///     Output custom location spawns
        /// </summary>
        private GameObject ZoneSystem_SpawnLocation(On.ZoneSystem.orig_SpawnLocation orig, ZoneSystem self,
            ZoneSystem.ZoneLocation location, int seed, Vector3 pos, Quaternion rot, ZoneSystem.SpawnMode mode,
            List<GameObject> spawnedGhostObjects)
        {
            if (ZoneManager.Instance.Locations.ContainsKey(location.m_prefabName))
            {
                Logger.LogDebug($"spawned {location.m_prefabName}, mode: {mode}");
            }
            return orig(self, location, seed, pos, rot, mode, spawnedGhostObjects);
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
