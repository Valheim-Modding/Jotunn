using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;

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

        public static CustomRPC TestRPC;

        private void Awake()
        {
            TestRPC = NetworkManager.Instance.GetRPC("rpctest");
            TestRPC.OnServerReceive += TestRPC_OnServerReceive;
            TestRPC.OnClientReceive += TestRPC_OnClientReceive;

            CommandManager.Instance.AddConsoleCommand(new SendSomethingBigCommand());
        }

        private void TestRPC_OnServerReceive(long sender, ZPackage package)
        {
            Jotunn.Logger.LogMessage($"Received blob"); 

            if (TestRPC.IsSending)
            {
                Jotunn.Logger.LogMessage($"RPC is currently broadcasting packages, discarding");
                return;
            }

            Jotunn.Logger.LogMessage($"Broadcasting to all clients");
            TestRPC.SendPackage(ZNet.instance.m_peers, new ZPackage(package.GetArray()));
        }
        
        private void TestRPC_OnClientReceive(long sender, ZPackage package)
        {
            Jotunn.Logger.LogMessage($"Received blob");
        }

        public class SendSomethingBigCommand : ConsoleCommand
        {
            public override string Name => "rpc.blob";

            public override string Help => "Send some big ass data chunks";

            private int[] Sizes = { 1, 2, 4 };

            public override void Run(string[] args)
            {
                if (args.Length != 1 || !Sizes.Any(x => x.Equals(int.Parse(args[0]))))
                {
                    Console.instance.Print($"Usage: rpc.send [{string.Join("|", Sizes)}]");
                    return;
                }

                if (TestRPC.IsSending || TestRPC.IsReceiving)
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
                TestRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
            }

            public override List<string> CommandOptionList()
            {
                return Sizes.Select(x => x.ToString()).ToList();
            }
        }
    }
}
