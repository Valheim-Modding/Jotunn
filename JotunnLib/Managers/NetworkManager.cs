using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
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
        private static NetworkManager _instance;
        /// <summary>
        ///     Singleton instance
        /// </summary>
        public static NetworkManager Instance => _instance ??= new NetworkManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private NetworkManager() {}

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
            Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(Game), nameof(Game.Start)), HarmonyPostfix]
            private static void Game_Start() => Instance.Game_Start();
        }

        /// <summary>
        ///     Get a <see cref="CustomRPC"/> for your mod
        /// </summary>
        /// <param name="name">Unique name for your RPC</param>
        /// <param name="serverReceive">Delegate which gets called on client instances when packages are received</param>
        /// <param name="clientReceive">Delegate which gets called on server instances when packages are received</param>
        /// <returns>Existing or newly created <see cref="CustomRPC"/></returns>
        public CustomRPC AddRPC(string name, CoroutineHandler serverReceive, CoroutineHandler clientReceive)
        {
            return AddRPC(BepInExUtils.GetSourceModMetadata(), name, serverReceive, clientReceive);
        }

        /// <summary>
        ///     Get the <see cref="CustomRPC"/> for a given mod.
        /// </summary>
        /// <param name="sourceMod">Reference to the <see cref="BepInPlugin"/> which added this entity</param>
        /// <param name="name">Unique name for your RPC</param>
        /// <param name="serverReceive">Delegate which gets called on client instances when packages are received</param>
        /// <param name="clientReceive">Delegate which gets called on server instances when packages are received</param>
        /// <returns>Existing or newly created <see cref="CustomRPC"/>.</returns>
        internal CustomRPC AddRPC(BepInPlugin sourceMod, string name, CoroutineHandler serverReceive, CoroutineHandler clientReceive)
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

        /// <summary>
        ///     Register all custom RPCs as <see cref="ZRoutedRpc">ZRoutedRPCs</see>
        /// </summary>
        private void Game_Start()
        {
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
