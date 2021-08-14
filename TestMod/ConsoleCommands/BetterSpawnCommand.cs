using UnityEngine;
using Jotunn.Entities;
using Jotunn.Managers;

namespace TestMod.ConsoleCommands
{
    public class BetterSpawnCommand : ConsoleCommand
    {
        public override string Name => "better_spawn";

        public override string Help => "like spawn but BETTER";

        public override void Run(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            GameObject prefab = PrefabManager.Instance.GetPrefab(args[0]);
            if (!prefab)
            {
                Console.instance.Print("that doesn't exist: " + args[0]);
                return;
            }

            int cnt = args.Length < 2 ? 1 : int.Parse(args[1]);
            for (int i = 0; i < cnt; i++)
            {
                UnityEngine.Object.Instantiate<GameObject>(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity);
            }
        }
    }
}
