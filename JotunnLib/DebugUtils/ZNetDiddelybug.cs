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
            On.ZSteamSocket.Recv += ZSteamSocket_Recv;
            On.ZNetView.Awake += ZNetView_Awake;
        }

        private void ZNetView_Awake(On.ZNetView.orig_Awake orig, ZNetView self)
        { 

            if (_isModEnabled.Value
                && (ZNetView.m_forceDisableInit || ZDOMan.instance == null))
            {
                Jotunn.Logger.LogWarning($"ZNetView of {self.name} will self-destruct");
            } 
            orig(self);
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

            orig(self, pkg);
        }
        private ZPackage ZSteamSocket_Recv(On.ZSteamSocket.orig_Recv orig, ZSteamSocket self)
        {
            if (!(_isModEnabled.Value && _isOutputEnabled.Value))
            {
                return orig(self);
            }

            var pkg = orig(self);

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

            return pkg;
        }
    }
}
