using System;
using BepInEx.Configuration;
using UnityEngine;

namespace Jotunn.DebugUtils
{
    internal class ZNetDiddelybug : MonoBehaviour
    {
        private ConfigEntry<bool> _isModEnabled;
        private ConfigEntry<bool> _isOutputEnabled;

        private void Awake()
        {
            _isModEnabled = Main.Instance.Config.Bind<bool>(
                nameof(ZNetDiddelybug), "Enabled", false, 
                new ConfigDescription("Globally enable or disable the RPC debug flag in ZRpc. Needs a server restart.", null,
                    new ConfigurationManagerAttributes() { IsAdminOnly = true }));

            _isOutputEnabled = Main.Instance.Config.Bind<bool>(
                nameof(ZNetDiddelybug), "Show Output", false,
                new ConfigDescription("Enable or disable RPC debug logging. Needs the debug flag to be enabled.", null,
                    new ConfigurationManagerAttributes() { IsAdminOnly = true }));

            On.ZNet.Awake += ZNet_Awake;
            On.ZSteamSocket.Send += ZSteamSocket_Send;
        }

        private void ZNet_Awake(On.ZNet.orig_Awake orig, ZNet self)
        {
            Logger.LogDebug($"ZNet awoken. IsServer: {self.IsServer()} IsDedicated: {self.IsDedicated()}");

            if (_isModEnabled.Value)
            {
                ZRpc.m_DEBUG = true;
            }

            orig(self);
        }

        private void ZSteamSocket_Send(On.ZSteamSocket.orig_Send orig, ZSteamSocket self, ZPackage pkg)
        {
            if (!(_isModEnabled.Value && _isOutputEnabled.Value))
            {
                orig(self, pkg);
                return;
            }
            
            pkg.SetPos(0);
            int methodHash = pkg.ReadInt();
            if (ZRpc.m_DEBUG)
            {
                if (methodHash == 0)
                {
                    Logger.LogMessage($"Sending RPC Ping to {self.GetHostName()}");
                }
                else
                {
                    try
                    {
                        string method = pkg.ReadString();

                        Logger.LogMessage($"Sending RPC {method} to {self.GetHostName()}");
                    }
                    catch (Exception) { }
                }
            }
            orig(self, pkg);
        }
    }
}
