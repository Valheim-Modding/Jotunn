using Jotunn;
using Jotunn.Entities;

namespace TestMod.ConsoleCommands
{
    public class AdminCheckCommand : ConsoleCommand
    {
        public override string Name => "admin.checkadmin";

        public override string Help => "Checks the admin status of all connected peers";

        public override void Run(string[] args)
        {
            foreach (var peer in ZNet.instance.GetPeers())
            {
                Logger.LogInfo($"Is {peer.m_playerName}/{peer.m_uid} Admin: {ZNet.instance.IsAdmin(peer.m_uid)}");
            }
        }
    }
}
