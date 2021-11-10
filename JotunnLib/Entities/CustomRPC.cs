using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace Jotunn.Entities
{
    public class CustomRPC : CustomEntity
    {
        public string Name { get; }
        
        internal string ID => $"{SourceMod.GUID}!{Name}";

        public event Action<long, ZPackage> OnServerReceive;
        public event Action<long, ZPackage> OnClientReceive;

        private const byte INIT_PACKAGE = 0;
        private const byte FRAGMENTED_PACKAGE = 64;
        private const byte COMPRESSED_PACKAGE = 128;
        
        private long PackageCounter;
        private readonly Dictionary<string, SortedDictionary<int, byte[]>> PackageCache = new Dictionary<string, SortedDictionary<int, byte[]>>();
        private readonly List<KeyValuePair<long, string>> CacheExpirations = new List<KeyValuePair<long, string>>(); // avoid leaking memory
        
        internal CustomRPC(BepInPlugin sourceMod, string name) : base(sourceMod)
        {
            Name = name;
        }

        /// <summary>
        ///     Initiates a RPC exchange with the server by sending an empty package.
        /// </summary>
        public void Initiate() =>
            ZNet.instance?.StartCoroutine(SendPackageRoutine(
                ZRoutedRpc.instance.GetServerPeerID(),
                new ZPackage(new[] {INIT_PACKAGE})));

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

            const int compressMinSize = 10000;

            if (package.Size() > compressMinSize)
            {
                byte[] rawData = package.GetArray();
                Logger.LogDebug($"Compressing package with length {rawData.Length}");

                ZPackage compressedPackage = new ZPackage();
                compressedPackage.Write(COMPRESSED_PACKAGE);
                MemoryStream output = new MemoryStream();
                using (DeflateStream deflateStream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
                {
                    deflateStream.Write(rawData, 0, rawData.Length);
                }
                compressedPackage.Write(output.ToArray());
                package = compressedPackage;
            }

            List<IEnumerator<bool>> writers = peers.Where(peer => peer.IsReady()).Select(p => SendToPeer(p, package)).ToList();
            writers.RemoveAll(writer => !writer.MoveNext());
            while (writers.Count > 0)
            {
                yield return null;
                writers.RemoveAll(writer => !writer.MoveNext());
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

            IEnumerable<bool> waitForQueue()
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

            void SendPackage(ZPackage pkg)
            {
                rpc.InvokeRoutedRPC(peer.m_uid, ID, pkg);
            }

            if (package.Size() > packageSliceSize)
            {
                byte[] data = package.GetArray();
                int fragments = (int)(1 + (data.LongLength - 1) / packageSliceSize);
                long packageIdentifier = ++PackageCounter;
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

                    ZPackage fragmentedPackage = new ZPackage();
                    fragmentedPackage.Write(FRAGMENTED_PACKAGE);
                    fragmentedPackage.Write(packageIdentifier);
                    fragmentedPackage.Write(fragment);
                    fragmentedPackage.Write(fragments);
                    fragmentedPackage.Write(data.Skip(packageSliceSize * fragment).Take(packageSliceSize).ToArray());

                    Logger.LogDebug($"Sending fragmented package {packageIdentifier}:{fragment}");
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

                Logger.LogDebug("Sending package");
                SendPackage(package);
            }
        }
        
        /// <summary>
        ///     Receive and handle an incoming package
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="package"></param>
        internal void HandlePackage(long sender, ZPackage package)
        {
            if (package == null || package.Size() <= 0)
            {
                return;
            }

            Logger.LogDebug($"Received package for custom RPC {ID}");
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
                    if (!PackageCache.TryGetValue(cacheKey, out SortedDictionary<int, byte[]> dataFragments))
                    {
                        dataFragments = new SortedDictionary<int, byte[]>();
                        PackageCache[cacheKey] = dataFragments;
                        CacheExpirations.Add(new KeyValuePair<long, string>(DateTimeOffset.Now.AddSeconds(60).Ticks, cacheKey));
                    }

                    int fragment = package.ReadInt();
                    int fragments = package.ReadInt();

                    dataFragments.Add(fragment, package.ReadByteArray());

                    if (dataFragments.Count < fragments)
                    {
                        return;
                    }

                    PackageCache.Remove(cacheKey);

                    package = new ZPackage(dataFragments.Values.SelectMany(a => a).ToArray());
                    packageFlags = package.ReadByte();
                }

                //ProcessingServerUpdate = true;

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
                }

                package.SetPos(0);

                if (ZNet.instance.IsServer())
                {
                    InvokeOnServerReceive(sender, package);
                }
                else
                {
                    InvokeOnClientReceive(sender, package);
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Error caught while applying package: {e}");
            }
            finally
            {
                //ProcessingServerUpdate = false;
            }
        }

        private void InvokeOnServerReceive(long sender, ZPackage package)
        {
            OnServerReceive?.SafeInvoke(sender, package);
        }
        
        private void InvokeOnClientReceive(long sender, ZPackage package)
        {
            OnClientReceive?.SafeInvoke(sender, package);
        }
    }
}
