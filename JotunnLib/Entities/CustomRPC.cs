using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BepInEx;
using Jotunn.Managers;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Wrapper for Valheim's RPC calls implementing convenience delegate methods for client and server processing of packages.<br/>
    ///     Automatically compresses and slices big packages to fit into the Steam package limit.<br/>
    ///     All sending and processing of received packages is executed in Coroutines to ensure the game loop's execution.
    /// </summary>
    public class CustomRPC : CustomEntity
    {
        private const byte JOTUNN_PACKAGE = 1;
        private const byte FRAGMENTED_PACKAGE = 2;
        private const byte COMPRESSED_PACKAGE = 4;

        /// <summary>
        ///     Name of the custom RPC as defined at instantiation
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        ///     True, if this RPC is currently sending data
        /// </summary>
        public bool IsSending => SendCount > 0;
        /// <summary>
        ///     True, if this RPC is currently receiving data
        /// </summary>
        public bool IsReceiving => PackageCache.Count > 0;
        /// <summary>
        ///     True, if this RPC is currently processing received data.
        ///     This is always true while executing the registered delegates.
        /// </summary>
        public bool IsProcessing => ProcessingCount > 0;
        /// <summary>
        ///     True, if this RPC is processing received data outside the current delegate call.
        ///     This should only be used in the registered delegate methods to determine
        ///     if this RPC is already processing another package.
        /// </summary>
        public bool IsProcessingOther => ProcessingCount-1 > 0;

        /// <summary>
        ///     Unique ID of this RPC to prevent name clashes between mods
        /// </summary>
        internal string ID => $"{SourceMod.GUID}!{Name}";
        
        /// <summary>
        ///     Delegate called when a package is received on the server
        /// </summary>
        internal NetworkManager.CoroutineHandler OnServerReceive;

        /// <summary>
        ///     Delegate called when a package is received on the client
        /// </summary>
        internal NetworkManager.CoroutineHandler OnClientReceive;

        private short SendCount;
        private short ProcessingCount;
        private long PackageCount;
        private readonly Dictionary<string, SortedDictionary<int, byte[]>> PackageCache =
            new Dictionary<string, SortedDictionary<int, byte[]>>();
        private readonly List<KeyValuePair<long, string>> CacheExpirations =
            new List<KeyValuePair<long, string>>(); // avoid leaking memory

        /// <summary>
        ///     Internal constructor only, CustomRPCs are instantiated via <see cref="NetworkManager"/>
        /// </summary>
        /// <param name="sourceMod">Reference to the <see cref="BepInPlugin"/> which created this RPC.</param>
        /// <param name="name"></param>
        /// <param name="serverReceive"></param>
        /// <param name="clientReceive"></param>
        internal CustomRPC(BepInPlugin sourceMod, string name, NetworkManager.CoroutineHandler serverReceive,
            NetworkManager.CoroutineHandler clientReceive) : base(sourceMod)
        {
            Name = name;
            OnServerReceive = serverReceive;
            OnClientReceive = clientReceive;
        }

        /// <summary>
        ///     Initiates a RPC exchange with the server by sending an empty package.
        /// </summary>
        public void Initiate() =>
            ZNet.instance?.StartCoroutine(SendPackageRoutine(
                ZRoutedRpc.instance.GetServerPeerID(),
                new ZPackage(new[] { JOTUNN_PACKAGE })));
        
        /// <summary>
        ///     Send a package to a single target. Compresses and fragments the package if necessary.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="package"></param>
        public void SendPackage(long target, ZPackage package) =>
            ZNet.instance?.StartCoroutine(SendPackageRoutine(target, package));

        /// <summary>
        ///     Send a package to a list of peers. Compresses and fragments the package if necessary.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="package"></param>
        public void SendPackage(List<ZNetPeer> peers, ZPackage package) =>
            ZNet.instance?.StartCoroutine(SendPackageRoutine(peers, package));

        /// <summary>
        ///     Coroutine to send a package to a single target. Compresses and fragments the package if necessary.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        public IEnumerator SendPackageRoutine(long target, ZPackage package)
        {
            if (!ZNet.instance)
            {
                return Enumerable.Empty<object>().GetEnumerator();
            }

            List<ZNetPeer> peers = ZRoutedRpc.instance.m_peers;
            if (target != ZRoutedRpc.Everybody)
            {
                peers = peers.Where(p => p.m_uid == target).ToList();
            }

            return SendPackageRoutine(peers, package);
        }

        /// <summary>
        ///     Coroutine to send a package to a list of peers. Compresses and fragments the package if necessary.
        /// </summary>
        /// <param name="peers"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        public IEnumerator SendPackageRoutine(List<ZNetPeer> peers, ZPackage package)
        {
            if (!ZNet.instance)
            {
                yield break;
            }
            
            try
            {
                ++SendCount;

                byte[] originalData = package.GetArray();
                ZPackage jotunnpackage = new ZPackage();
                jotunnpackage.Write(JOTUNN_PACKAGE);
                jotunnpackage.Write(originalData);
                package = jotunnpackage;

                const int compressMinSize = 10000;

                if (package.Size() > compressMinSize)
                {
                    byte[] rawData = package.GetArray();
                    Logger.LogDebug($"[{ID}] Compressing package with length {rawData.Length}");

                    ZPackage compressedPackage = new ZPackage();
                    compressedPackage.Write(COMPRESSED_PACKAGE);
                    MemoryStream output = new MemoryStream();
                    using (DeflateStream deflateStream = new DeflateStream(output, CompressionLevel.Optimal))
                    {
                        deflateStream.Write(rawData, 0, rawData.Length);
                    }

                    compressedPackage.Write(output.ToArray());
                    package = compressedPackage;
                }

                List<IEnumerator<bool>> writers =
                    peers.Where(peer => peer.IsReady()).Select(p => SendToPeer(p, package)).ToList();
                writers.RemoveAll(writer => !writer.MoveNext());
                while (writers.Count > 0)
                {
                    yield return null;
                    writers.RemoveAll(writer => !writer.MoveNext());
                }
            }
            finally
            {
                --SendCount;
            }
        }

        /// <summary>
        ///     Coroutine to send a package to an actual peer.
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        private IEnumerator<bool> SendToPeer(ZNetPeer peer, ZPackage package)
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null)
            {
                yield break;
            }

            const int packageSliceSize = 250000;
            const int maximumSendQueueSize = 20000;

            IEnumerable<bool> WaitForQueue()
            {
                float timeout = Time.time + 30;
                while (peer.m_socket.GetSendQueueSize() > maximumSendQueueSize)
                {
                    if (Time.time > timeout)
                    {
                        Logger.LogInfo($"Disconnecting {peer.m_uid} after 30 seconds sending timeout");
                        peer.m_rpc.Invoke("Error", ZNet.ConnectionStatus.ErrorConnectFailed);
                        ZNet.instance.Disconnect(peer);
                        yield break;
                    }

                    yield return false;
                }
            }

            void Send(ZPackage pkg)
            {
                rpc.InvokeRoutedRPC(peer.m_uid, ID, pkg);
            }

            if (package.Size() > packageSliceSize)
            {
                byte[] data = package.GetArray();
                int fragments = (int)(1 + (data.LongLength - 1) / packageSliceSize);
                long packageIdentifier = ++PackageCount;
                for (int fragment = 0; fragment < fragments; fragment++)
                {
                    foreach (bool wait in WaitForQueue())
                    {
                        yield return wait;
                    }

                    if (!peer.m_socket.IsConnected())
                    {
                        yield break;
                    }

                    ZPackage fragmentedPackage = new ZPackage();
                    fragmentedPackage.Write(FRAGMENTED_PACKAGE);
                    fragmentedPackage.Write(packageIdentifier);
                    fragmentedPackage.Write(fragment);
                    fragmentedPackage.Write(fragments);
                    fragmentedPackage.Write(data.Skip(packageSliceSize * fragment).Take(packageSliceSize).ToArray());

                    Logger.LogDebug($"[{ID}] Sending fragmented package {packageIdentifier}:{fragment}");
                    Send(fragmentedPackage);

                    if (fragment != fragments - 1)
                    {
                        yield return true;
                    }
                }
            }
            else
            {
                foreach (bool wait in WaitForQueue())
                {
                    yield return wait;
                }

                Logger.LogDebug($"[{ID}] Sending package");
                Send(package);
            }
        }

        /// <summary>
        ///     Receive and handle an incoming package
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="package"></param>
        internal void ReceivePackage(long sender, ZPackage package)
        {
            if (package == null || package.Size() <= 0)
            {
                return;
            }

            Logger.LogDebug($"[{ID}] Received package");
            try
            {
                CacheExpirations.RemoveAll(kv =>
                {
                    if (kv.Key < DateTimeOffset.Now.Ticks)
                    {
                        PackageCache.Remove(kv.Value);
                        return true;
                    }

                    return false;
                });

                byte packageFlags = package.ReadByte();

                if ((packageFlags & FRAGMENTED_PACKAGE) != 0)
                {
                    long uniqueIdentifier = package.ReadLong();
                    string cacheKey = sender.ToString() + uniqueIdentifier;
                    int fragment = package.ReadInt();
                    int fragments = package.ReadInt();

                    if (!PackageCache.TryGetValue(cacheKey, out SortedDictionary<int, byte[]> dataFragments))
                    {
                        dataFragments = new SortedDictionary<int, byte[]>();
                        PackageCache[cacheKey] = dataFragments;
                        CacheExpirations.Add(new KeyValuePair<long, string>(DateTimeOffset.Now.AddSeconds(60).Ticks, cacheKey));
                    }

                    dataFragments.Add(fragment, package.ReadByteArray());

                    if (dataFragments.Count < fragments)
                    {
                        return;
                    }

                    PackageCache.Remove(cacheKey);

                    package = new ZPackage(dataFragments.Values.SelectMany(a => a).ToArray());
                    packageFlags = package.ReadByte();
                }

                ZNet.instance.StartCoroutine(HandlePackageRoutine(sender, package, packageFlags));
            }
            catch (Exception e)
            {
                Logger.LogWarning($"[{ID}] Error caught while applying package: {e}");
            }
        }

        private IEnumerator HandlePackageRoutine(long sender, ZPackage package, byte packageFlags)
        {
            try
            {
                ++ProcessingCount;

                if ((packageFlags & COMPRESSED_PACKAGE) != 0)
                {
                    byte[] data = package.ReadByteArray();

                    MemoryStream input = new MemoryStream(data);
                    MemoryStream output = new MemoryStream();
                    using (DeflateStream deflateStream = new DeflateStream(input, CompressionMode.Decompress))
                    {
                        deflateStream.CopyTo(output);
                    }

                    package = new ZPackage(output.ToArray());
                    packageFlags = package.ReadByte();

                    Logger.LogDebug($"[{ID}] Decompressed package to length {output.Length}");
                }
                
                if ((packageFlags & JOTUNN_PACKAGE) != JOTUNN_PACKAGE)
                {
                    Logger.LogWarning($"[{ID}] Package flag does not equal {JOTUNN_PACKAGE} ({packageFlags:X4})");
                    yield break;
                }

                byte[] finalBytes = package.ReadByteArray();
                ZPackage finalPackage = new ZPackage(finalBytes);
                package = finalPackage;

                if (ZNet.instance.IsServer())
                {
                    yield return OnServerReceive(sender, package);
                }
                else
                {
                    yield return OnClientReceive(sender, package);
                }
            }
            finally
            {
                --ProcessingCount;
            }
        }
    }
}
