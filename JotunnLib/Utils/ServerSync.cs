using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Jotunn.Utils
{
    public class ServerSync
    {
        public static bool ProcessingServerUpdate = false;

        public readonly string Name;

        private static bool isServer;

        public ServerSync(string name)
        {
            Name = name;
            On.ZNet.Awake += ZNet_Awake;
            On.ZNet.OnNewConnection += ZNet_OnNewConnection;
        }

        private void ZNet_Awake(On.ZNet.orig_Awake orig, ZNet self)
        {
            orig(self);
            ZRoutedRpc.instance.Register<ZPackage>(Name + " ConfigSync", RPC_ConfigSync);
        }

        private void ZNet_OnNewConnection(On.ZNet.orig_OnNewConnection orig, ZNet self, ZNetPeer peer)
        {
            orig(self, peer);
            if (!self.IsServer())
            {
                peer.m_rpc.Register<ZPackage>(Name + " ConfigSync", RPC_InitialConfigSync);
            }
        }

        private const byte PARTIAL_CONFIGS = 1;
        private const byte FRAGMENTED_CONFIG = 2;
        private const byte COMPRESSED_CONFIG = 4;

        private readonly Dictionary<string, SortedDictionary<int, byte[]>> configValueCache = new();
        private readonly List<KeyValuePair<long, string>> cacheExpirations = new(); // avoid leaking memory

        private void RPC_InitialConfigSync(ZRpc rpc, ZPackage package) => RPC_ConfigSync(0, package);

        private void RPC_ConfigSync(long sender, ZPackage package)
        {
            try
            {
                cacheExpirations.RemoveAll(kv =>
                {
                    if (kv.Key < DateTimeOffset.Now.Ticks)
                    {
                        configValueCache.Remove(kv.Value);
                        return true;
                    }

                    return false;
                });

                byte packageFlags = package.ReadByte();

                if ((packageFlags & FRAGMENTED_CONFIG) != 0)
                {
                    long uniqueIdentifier = package.ReadLong();
                    string cacheKey = sender.ToString() + uniqueIdentifier;
                    if (!configValueCache.TryGetValue(cacheKey, out SortedDictionary<int, byte[]> dataFragments))
                    {
                        dataFragments = new SortedDictionary<int, byte[]>();
                        configValueCache[cacheKey] = dataFragments;
                        cacheExpirations.Add(new KeyValuePair<long, string>(DateTimeOffset.Now.AddSeconds(60).Ticks, cacheKey));
                    }

                    int fragment = package.ReadInt();
                    int fragments = package.ReadInt();

                    dataFragments.Add(fragment, package.ReadByteArray());

                    if (dataFragments.Count < fragments)
                    {
                        return;
                    }

                    configValueCache.Remove(cacheKey);

                    package = new ZPackage(dataFragments.Values.SelectMany(a => a).ToArray());
                    packageFlags = package.ReadByte();
                }

                ProcessingServerUpdate = true;

                if ((packageFlags & COMPRESSED_CONFIG) != 0)
                {
                    byte[] data = package.ReadByteArray();

                    MemoryStream input = new(data);
                    MemoryStream output = new();
                    using (DeflateStream deflateStream = new(input, CompressionMode.Decompress))
                    {
                        deflateStream.CopyTo(output);
                    }

                    package = new ZPackage(output.ToArray());
                    packageFlags = package.ReadByte();
                }
            }
            finally
            {
                ProcessingServerUpdate = false;
            }
        }

        private static long packageCounter = 0;

        private IEnumerator<bool> distributeConfigToPeers(ZNetPeer peer, ZPackage package)
        {
            if (ZRoutedRpc.instance is not ZRoutedRpc rpc)
            {
                yield break;
            }

            const int packageSliceSize = 250000;
            const int maximumSendQueueSize = 20000;

            IEnumerable<bool> waitForQueue()
            {
                float timeout = Time.time + 30;
                while (peer.m_socket.GetSendQueueSize() > maximumSendQueueSize)
                {
                    if (Time.time > timeout)
                    {
                        Debug.Log($"Disconnecting {peer.m_uid} after 30 seconds config sending timeout");
                        peer.m_rpc.Invoke("Error", ZNet.ConnectionStatus.ErrorConnectFailed);
                        ZNet.instance.Disconnect(peer);
                        yield break;
                    }

                    yield return false;
                }
            }

            void SendPackage(ZPackage pkg)
            {
                string method = Name + " ConfigSync";
                if (isServer)
                {
                    peer.m_rpc.Invoke(method, pkg);
                }
                else
                {
                    rpc.InvokeRoutedRPC(peer.m_server ? 0 : peer.m_uid, method, pkg);
                }
            }

            if (package.GetArray() is byte[] { LongLength: > packageSliceSize } data)
            {
                int fragments = (int)(1 + (data.LongLength - 1) / packageSliceSize);
                long packageIdentifier = ++packageCounter;
                for (int fragment = 0; fragment < fragments; fragment++)
                {
                    foreach (bool wait in waitForQueue())
                    {
                        yield return wait;
                    }

                    if (!peer.m_socket.IsConnected())
                    {
                        yield break;
                    }

                    ZPackage fragmentedPackage = new();
                    fragmentedPackage.Write(FRAGMENTED_CONFIG);
                    fragmentedPackage.Write(packageIdentifier);
                    fragmentedPackage.Write(fragment);
                    fragmentedPackage.Write(fragments);
                    fragmentedPackage.Write(data.Skip(packageSliceSize * fragment).Take(packageSliceSize).ToArray());
                    SendPackage(fragmentedPackage);

                    if (fragment != fragments - 1)
                    {
                        yield return true;
                    }
                }
            }
            else
            {
                foreach (bool wait in waitForQueue())
                {
                    yield return wait;
                }

                SendPackage(package);
            }
        }

        private IEnumerator sendZPackage(long target, ZPackage package)
        {
            if (!ZNet.instance)
            {
                return Enumerable.Empty<object>().GetEnumerator();
            }

            List<ZNetPeer> peers = (List<ZNetPeer>)AccessTools.DeclaredField(typeof(ZRoutedRpc), "m_peers").GetValue(ZRoutedRpc.instance);
            if (target != ZRoutedRpc.Everybody)
            {
                peers = peers.Where(p => p.m_uid == target).ToList();
            }

            return sendZPackage(peers, package);
        }

        private IEnumerator sendZPackage(List<ZNetPeer> peers, ZPackage package)
        {
            if (!ZNet.instance)
            {
                yield break;
            }

            const int compressMinSize = 10000;

            if (package.GetArray() is byte[] { LongLength: > compressMinSize } rawData)
            {
                ZPackage compressedPackage = new();
                compressedPackage.Write(COMPRESSED_CONFIG);
                MemoryStream output = new();
                using (DeflateStream deflateStream = new(output, System.IO.Compression.CompressionLevel.Optimal))
                {
                    deflateStream.Write(rawData, 0, rawData.Length);
                }
                compressedPackage.Write(output.ToArray());
                package = compressedPackage;
            }

            List<IEnumerator<bool>> writers = peers.Where(peer => peer.IsReady()).Select(p => distributeConfigToPeers(p, package)).ToList();
            writers.RemoveAll(writer => !writer.MoveNext());
            while (writers.Count > 0)
            {
                yield return null;
                writers.RemoveAll(writer => !writer.MoveNext());
            }
        }
/*
        [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
        private class SendConfigsAfterLogin
        {
            private class BufferingSocket : ISocket
            {
                public volatile bool finished = false;
                public readonly List<ZPackage> Package = new();
                public readonly ISocket Original;

                public BufferingSocket(ISocket original)
                {
                    Original = original;
                }

                public bool IsConnected() => Original.IsConnected();
                public ZPackage Recv() => Original.Recv();
                public int GetSendQueueSize() => Original.GetSendQueueSize();
                public int GetCurrentSendRate() => Original.GetCurrentSendRate();
                public bool IsHost() => Original.IsHost();
                public void Dispose() => Original.Dispose();
                public bool GotNewData() => Original.GotNewData();
                public void Close() => Original.Close();
                public string GetEndPointString() => Original.GetEndPointString();
                public void GetAndResetStats(out int totalSent, out int totalRecv) => Original.GetAndResetStats(out totalSent, out totalRecv);
                public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec) => Original.GetConnectionQuality(out localQuality, out remoteQuality, out ping, out outByteSec, out inByteSec);
                public ISocket Accept() => Original.Accept();
                public int GetHostPort() => Original.GetHostPort();
                public bool Flush() => Original.Flush();
                public string GetHostName() => Original.GetHostName();

                public void Send(ZPackage pkg)
                {
                    pkg.SetPos(0);
                    int methodHash = pkg.ReadInt();
                    if ((methodHash == "PeerInfo".GetStableHashCode() || methodHash == "RoutedRPC".GetStableHashCode()) && !finished)
                    {
                        Package.Add(new ZPackage(pkg.GetArray())); // the original ZPackage gets reused, create a new one
                    }
                    else
                    {
                        Original.Send(pkg);
                    }
                }
            }

            [HarmonyPriority(Priority.First)]
            private static void Prefix(ref Dictionary<Assembly, BufferingSocket>? __state, ZNet __instance, ZRpc rpc)
            {
                if (__instance.IsServer())
                {
                    BufferingSocket bufferingSocket = new(rpc.GetSocket());
                    AccessTools.DeclaredField(typeof(ZRpc), "m_socket").SetValue(rpc, bufferingSocket);

                    __state ??= new Dictionary<Assembly, BufferingSocket>();
                    __state[Assembly.GetExecutingAssembly()] = bufferingSocket;
                }
            }

            private static void Postfix(Dictionary<Assembly, BufferingSocket> __state, ZNet __instance, ZRpc rpc)
            {
                if (!__instance.IsServer())
                {
                    return;
                }

                ZNetPeer peer = (ZNetPeer)AccessTools.DeclaredMethod(typeof(ZNet), "GetPeer", new[] { typeof(ZRpc) }).Invoke(__instance, new object[] { rpc });

                IEnumerator sendAsync()
                {
                    foreach (ServerSync configSync in configSyncs)
                    {
                        List<PackageEntry> entries = new();
                        if (configSync.CurrentVersion != null)
                        {
                            entries.Add(new PackageEntry { section = "Internal", key = "serverversion", type = typeof(string), value = configSync.CurrentVersion });
                        }

                        if (configSync.MinimumRequiredVersion != null)
                        {
                            entries.Add(new PackageEntry { section = "Internal", key = "requiredversion", type = typeof(string), value = configSync.MinimumRequiredVersion });
                        }

                        entries.Add(new PackageEntry { section = "Internal", key = "lockexempt", type = typeof(bool), value = ((SyncedList)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance)).Contains(rpc.GetSocket().GetHostName()) });

                        ZPackage package = ConfigsToPackage(configSync.allConfigs.Select(c => c.BaseConfig), configSync.allCustomValues, entries, false);

                        yield return __instance.StartCoroutine(configSync.sendZPackage(new List<ZNetPeer> { peer }, package));
                    }

                    if (rpc.GetSocket() is BufferingSocket bufferingSocket)
                    {
                        AccessTools.DeclaredField(typeof(ZRpc), "m_socket").SetValue(rpc, bufferingSocket.Original);
                    }

                    bufferingSocket = __state[Assembly.GetExecutingAssembly()];
                    bufferingSocket.finished = true;

                    foreach (ZPackage package in bufferingSocket.Package)
                    {
                        bufferingSocket.Original.Send(package);
                    }
                }

                __instance.StartCoroutine(sendAsync());
            }
        }*/
    }
}
