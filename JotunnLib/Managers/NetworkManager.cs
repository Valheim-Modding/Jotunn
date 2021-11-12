using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager handling all network related code
    /// </summary>
    public class NetworkManager : IManager
    {
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
        private static NetworkManager _instance;

        /// <summary>
        ///     Delegate for receiving <see cref="ZPackage">ZPackages</see>.
        ///     Gets called inside a <see cref="Coroutine"/>.
        /// </summary>
        /// <param name="sender">Sender ID of the package</param>
        /// <param name="package">Package sent</param>
        /// <returns></returns>
        public delegate IEnumerator CoroutineHandler(long sender, ZPackage package);

        /// <summary>
        ///     Internal list of registered RPCs
        /// </summary>
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
        public CustomRPC GetRPC(string name, CoroutineHandler serverReceive, CoroutineHandler clientReceive)
        {
            return GetRPC(BepInExUtils.GetSourceModMetadata(), name, serverReceive, clientReceive);
        }

        /// <summary>
        ///     Get the <see cref="CustomRPC"/> for a given mod.
        /// </summary>
        /// <returns>Existing or newly created <see cref="CustomRPC"/>.</returns>
        internal CustomRPC GetRPC(BepInPlugin sourceMod, string name, CoroutineHandler serverReceive, CoroutineHandler clientReceive)
        {
            var ret = RPCs.FirstOrDefault(x => x.SourceMod == sourceMod && x.Name == name);

            if (ret != null)
            {
                return ret;
            }

            ret = new CustomRPC(sourceMod, name, serverReceive, clientReceive);
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
