using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace TestMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    internal class TestRPCs : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.testrpcs";
        private const string ModName = "Jotunn RPC Tests";
        private const string ModVersion = "0.1.0";

        public static CustomRPC NonblockingRPC;
        public static CustomRPC BlockingRPC;

        private void Awake()
        {
            CommandManager.Instance.AddConsoleCommand(new NonblockingRPCCommand());

            NonblockingRPC = NetworkManager.Instance.GetRPC("nonblocking");
            NonblockingRPC.OnServerReceive += NonblockingRPC_OnServerReceive;
            NonblockingRPC.OnClientReceive += NonblockingRPC_OnClientReceive;

            CommandManager.Instance.AddConsoleCommand(new BlockingRPCommand());   
            
            BlockingRPC = NetworkManager.Instance.GetRPC("blocking");
            BlockingRPC.Blocking = true;
            BlockingRPC.OnServerReceive += BlockingRPC_OnServerReceive;
            BlockingRPC.OnClientReceive += BlockingRPC_OnClientReceive;
        }
        
        public class NonblockingRPCCommand : ConsoleCommand
        {
            public override string Name => "rpc.nonblocking";

            public override string Help => "Send data chunks over a non-blocking RPC";

            private int[] Sizes = { 0, 1, 2, 4 };

            public override void Run(string[] args)
            {
                if (args.Length != 1 || !Sizes.Any(x => x.Equals(int.Parse(args[0]))))
                {
                    Console.instance.Print($"Usage: rpc.nonblocking [{string.Join("|", Sizes)}]");
                    return;
                }

                if (NonblockingRPC.IsSending || NonblockingRPC.IsReceiving)
                {
                    Console.instance.Print($"RPC is currently busy");
                    return;
                }

                ZPackage package = new ZPackage();
                System.Random random = new System.Random();
                byte[] array = new byte[int.Parse(args[0]) * 1024 * 1024];
                random.NextBytes(array);
                package.Write(array);

                Jotunn.Logger.LogMessage($"Sending {args[0]}MB blob to server.");
                NonblockingRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
            }

            public override List<string> CommandOptionList()
            {
                return Sizes.Select(x => x.ToString()).ToList();
            }
        }

        private IEnumerator NonblockingRPC_OnServerReceive(long sender, ZPackage package)
        {
            Jotunn.Logger.LogMessage($"Received blob"); 

            if (NonblockingRPC.IsSending)
            {
                Jotunn.Logger.LogMessage($"RPC is currently broadcasting packages, discarding");
                yield break;
            }

            Jotunn.Logger.LogMessage($"Broadcasting to all clients");
            NonblockingRPC.SendPackage(ZNet.instance.m_peers, new ZPackage(package.GetArray()));
        }
        
        private IEnumerator NonblockingRPC_OnClientReceive(long sender, ZPackage package)
        {
            Jotunn.Logger.LogMessage($"Received blob");
            yield break;
        }
        
        public class BlockingRPCommand : ConsoleCommand
        {
            public override string Name => "rpc.blocking";

            public override string Help => "Send data chunks over a blocking RPC";

            private int[] Sizes = { 0, 1, 2, 4 };

            public override void Run(string[] args)
            {
                if (args.Length != 1 || !Sizes.Any(x => x.Equals(int.Parse(args[0]))))
                {
                    Console.instance.Print($"Usage: rpc.blocking [{string.Join("|", Sizes)}]");
                    return;
                }
                
                ZPackage package = new ZPackage();
                System.Random random = new System.Random();
                byte[] array = new byte[int.Parse(args[0]) * 1024 * 1024];
                random.NextBytes(array);
                package.Write(array);

                Jotunn.Logger.LogMessage($"Sending {args[0]}MB blob to server.");
                BlockingRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
            }

            public override List<string> CommandOptionList()
            {
                return Sizes.Select(x => x.ToString()).ToList();
            }
        }
        private IEnumerator BlockingRPC_OnServerReceive(long sender, ZPackage package)
        {
            Jotunn.Logger.LogMessage($"Received blob, processing");

            yield return new WaitForSeconds(5f);

            Jotunn.Logger.LogMessage($"Broadcasting to all clients");
            BlockingRPC.SendPackage(ZNet.instance.m_peers, new ZPackage(package.GetArray()));
        }
        
        private IEnumerator BlockingRPC_OnClientReceive(long sender, ZPackage package)
        {
            Jotunn.Logger.LogMessage($"Received blob, processing");

            yield return new WaitForSeconds(5f);
        }
    }
}
