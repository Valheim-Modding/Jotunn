using System;
using System.Linq;
using JotunnLib.Utils;

namespace JotunnLib.Patches
{
    public class ZNetPatches
    {
        [PatchInit(0)]
        public static void Init()
        {
            On.ZNet.RPC_ClientHandshake += ProvidePasswordPatch;
        }

        // to keep sane during testing period
        private static void ProvidePasswordPatch(On.ZNet.orig_RPC_ClientHandshake orig, ZNet self, ZRpc rpc, bool needPassword)
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
    }
}