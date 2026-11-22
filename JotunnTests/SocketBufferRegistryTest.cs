using Jotunn.Managers;
using Xunit;

namespace Jotunn.Utils
{
    public class SocketBufferRegistryTest
    {
        // Two clients on one Steam account: same end-point string, and even value-equal sockets.
        private sealed class FakeSocket : ISocket
        {
            private readonly string endpoint;
            public FakeSocket(string endpoint) => this.endpoint = endpoint;
            public override bool Equals(object obj) => obj is FakeSocket other && other.endpoint == endpoint;
            public override int GetHashCode() => endpoint.GetHashCode();
            public string GetEndPointString() => endpoint;
            public bool IsConnected() => true;
            public void Send(ZPackage pkg) { }
            public ZPackage Recv() => null;
            public int GetSendQueueSize() => 0;
            public int GetCurrentSendRate() => 0;
            public bool IsHost() => false;
            public void Dispose() { }
            public bool GotNewData() => false;
            public void Close() { }
            public void GetAndResetStats(out int totalSent, out int totalRecv) { totalSent = 0; totalRecv = 0; }
            public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
            {
                localQuality = remoteQuality = outByteSec = inByteSec = 0f;
                ping = 0;
            }
            public ISocket Accept() => null;
            public int GetHostPort() => 0;
            public bool Flush() => true;
            public string GetHostName() => endpoint;
            public void VersionMatch() { }
        }

        [Fact]
        public void SameEndpoint_OtherSocket_NotCaptured()
        {
            var registry = new SynchronizationManager.SocketBufferRegistry();
            var a = new FakeSocket("76561198000000000");
            var b = new FakeSocket("76561198000000000");

            var bufferB = registry.Start(b);

            Assert.Null(registry.GetActive(a));
            Assert.Same(bufferB, registry.GetActive(b));
        }

        [Fact]
        public void SameEndpoint_BothBuffering_Separate()
        {
            var registry = new SynchronizationManager.SocketBufferRegistry();
            var a = new FakeSocket("x");
            var b = new FakeSocket("x");

            var bufferA = registry.Start(a);
            var bufferB = registry.Start(b);

            Assert.NotSame(bufferA, bufferB);
            Assert.Same(bufferA, registry.GetActive(a));
            Assert.Same(bufferB, registry.GetActive(b));
        }

        [Fact]
        public void Finished_NotActive_AndRemoved()
        {
            var registry = new SynchronizationManager.SocketBufferRegistry();
            var a = new FakeSocket("x");
            var buffer = registry.Start(a);

            buffer.finished = true;
            Assert.Null(registry.GetActive(a));

            registry.Remove(a, buffer);
            Assert.Equal(0, registry.Count);
        }

        [Fact]
        public void EarlyReturn_RemoveWithoutFinish_NoLeak()
        {
            var registry = new SynchronizationManager.SocketBufferRegistry();
            var a = new FakeSocket("x");
            var buffer = registry.Start(a);

            registry.Remove(a, buffer);

            Assert.Equal(0, registry.Count);
            Assert.Null(registry.GetActive(a));
        }

        [Fact]
        public void Remove_StaleBuffer_KeepsNewerEntry()
        {
            var registry = new SynchronizationManager.SocketBufferRegistry();
            var a = new FakeSocket("x");
            var old = registry.Start(a);
            var newer = registry.Start(a);

            registry.Remove(a, old);

            Assert.Same(newer, registry.GetActive(a));
        }

        [Fact]
        public void Remove_NullState_NoThrow()
        {
            var registry = new SynchronizationManager.SocketBufferRegistry();
            registry.Remove(new FakeSocket("x"), null);
            registry.Remove(null, null);
            Assert.Equal(0, registry.Count);
        }
    }
}
