using System.Collections.Generic;
using Jotunn.Entities;

namespace TestMod.ConsoleCommands
{
    public class ResetCartographyCommand : ConsoleCommand
    {
        public override string Name => "resetcartography";
        public override string Help => "Reset cartography exploration status";
        public override void Run(string[] args)
        {
            if (!Minimap.instance)
            {
                return;
            }

            List<Minimap.PinData> playerpins = new List<Minimap.PinData>();
            foreach (var pin in Minimap.instance.m_pins)
            {
                if (pin.m_ownerID == Player.m_localPlayer.GetPlayerID())
                {
                    playerpins.Add(pin);
                }
            }
            Minimap.instance.m_pins = playerpins;

            Minimap.instance.m_exploredOthers = new bool[Minimap.instance.m_exploredOthers.Length];
            if (Minimap.instance.m_sharedMapHint)
            {
                UnityEngine.Object.Destroy(Minimap.instance.m_sharedMapHint);
            }
            Minimap.instance.m_showSharedMapData = false;
            Minimap.instance.SetMapData(Minimap.instance.GetMapData());
            Minimap.instance.SaveMapData();
            ZNet.instance?.SaveWorld(false);
        }
    }
}
