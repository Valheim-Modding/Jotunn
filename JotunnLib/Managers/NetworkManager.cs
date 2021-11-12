using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Utils;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager handling all network related code
    /// </summary>
    public class NetworkManager : IManager
    {
        private static NetworkManager _instance;
        /// <summary>
        ///     Singleton instance
        /// </summary>
        public static NetworkManager Instance
        {
            get
            {
                return _instance ??= new NetworkManager();
            }
        }

        internal readonly List<CustomRPC> RPCs = new List<CustomRPC>();

        /// <summary>
        ///     Manager's main init
        /// </summary>
        public void Init()
        {
            On.Game.Start += Game_Start;
        }

        /// <summary>
        ///     Get a <see cref="CustomRPC"/> for your mod.
        /// </summary>
        /// <returns>Existing or newly created <see cref="CustomRPC"/>.</returns>
        public CustomRPC GetRPC(string name)
        {
            return GetRPC(BepInExUtils.GetSourceModMetadata(), name);
        }

        /// <summary>
        ///     Get the <see cref="CustomRPC"/> for a given mod.
        /// </summary>
        /// <returns>Existing or newly created <see cref="CustomRPC"/>.</returns>
        internal CustomRPC GetRPC(BepInPlugin sourceMod, string name)
        {
            var ret = RPCs.FirstOrDefault(x => x.SourceMod == sourceMod && x.Name == name);

            if (ret != null)
            {
                return ret;
            }

            ret = new CustomRPC(sourceMod, name);
            RPCs.Add(ret);
            return ret;
        }

        private void Game_Start(On.Game.orig_Start orig, Game self)
        {
            orig(self);

            if (!RPCs.Any())
            {
                return;
            }

            Logger.LogInfo($"Registering {RPCs.Count} custom RPCs");

            foreach (var rpc in RPCs)
            {
                ZRoutedRpc.instance.Register(rpc.ID, new Action<long, ZPackage>(rpc.ReceivePackage));
            }
        }
    }
}
