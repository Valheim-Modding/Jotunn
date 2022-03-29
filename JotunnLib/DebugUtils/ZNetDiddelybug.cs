using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Jotunn.DebugUtils
{
    internal class ZNetDiddelybug : MonoBehaviour
    {
        private static ZNetDiddelybug instance;

        private ConfigEntry<bool> _isModEnabled;
        private ConfigEntry<bool> _isOutputEnabled;

        private void Awake()
        {
            instance = this;

            _isModEnabled = Main.Instance.Config.Bind<bool>(
                nameof(ZNetDiddelybug), "Enabled", false, 
                new ConfigDescription("Globally enable or disable the RPC debug flag in ZRpc. Needs a server restart.", null,
                    new ConfigurationManagerAttributes() { IsAdminOnly = true }));

            _isOutputEnabled = Main.Instance.Config.Bind<bool>(
                nameof(ZNetDiddelybug), "Show Output", false,
                new ConfigDescription("Enable or disable RPC debug logging. Needs the debug flag to be enabled.", null,
                    new ConfigurationManagerAttributes() { IsAdminOnly = true }));

           Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake)), HarmonyPrefix]
            private static void ZNet_Awake(ZNet __instance) => instance.ZNet_Awake(__instance);

            [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.Awake)), HarmonyPrefix]
            private static void ZNetView_Awake(ZNetView __instance) => instance.ZNetView_Awake(__instance);

            [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.Send)), HarmonyPrefix]
            private static void ZSteamSocket_Send(ZSteamSocket __instance, ZPackage pkg) => instance.ZSteamSocket_Send(__instance, pkg);

            [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.Recv)), HarmonyPostfix]
            private static void ZSteamSocket_Recv(ZSteamSocket __instance, ref ZPackage __result) => instance.ZSteamSocket_Recv(__instance, __result);
        }

        private void ZNetView_Awake(ZNetView self)
        {
            if (_isModEnabled.Value && (ZNetView.m_forceDisableInit || ZDOMan.instance == null))
            {
                Logger.LogWarning($"ZNetView of {self.name} will self-destruct");
            }
        }

        private void ZNet_Awake(ZNet self)
        {
            Logger.LogDebug($"ZNet awoken. IsServer: {self.IsServer()} IsDedicated: {self.IsDedicated()}");

            if (_isModEnabled.Value)
            {
                ZRpc.m_DEBUG = true;
            }
        }

        private void ZSteamSocket_Send(ZSteamSocket self, ZPackage pkg)
        {
            if (!(_isModEnabled.Value && _isOutputEnabled.Value))
            {
                return;
            }
            
            pkg.SetPos(0);
            int methodHash = pkg.ReadInt();
            if (methodHash == 0)
            {
                Logger.LogMessage($"Sending RPC Ping to {self.GetHostName()}");
            }
            else
            {
                try
                {
                    string method = pkg.ReadString();

                    if (method == "RoutedRPC")
                    {
                        ZPackage wrapped = pkg.ReadPackage();
                        _ = wrapped.ReadInt();
                        method = pkg.ReadString();
                    }

                    Logger.LogMessage($"Sending RPC {method} to {self.GetHostName()}");
                }
                catch (Exception) { }
            }
        }

        private void ZSteamSocket_Recv(ZSteamSocket self, ZPackage pkg)
        {
            if (!(_isModEnabled.Value && _isOutputEnabled.Value))
            {
                return;
            }

            if (pkg != null)
            {
                int methodHash = pkg.ReadInt();
                if (methodHash == 0)
                {
                    Logger.LogMessage($"Received RPC Ping");
                }
                else
                {
                    try
                    {
                        string method = pkg.ReadString();

                        if (method == "RoutedRPC")
                        {
                            ZPackage wrapped = pkg.ReadPackage();
                            _ = wrapped.ReadInt();
                            method = pkg.ReadString();
                        }

                        Logger.LogMessage($"Received RPC {method}");
                    }
                    catch (Exception) { }
                }
                pkg.SetPos(0);
            }
        }
    }
}
