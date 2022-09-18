using Xunit;
using static Jotunn.Managers.SynchronizationManager;

namespace Jotunn.Utils
{
    public class SynchronizationManagerTest
    {
        [Fact]
        public void PeerInfoBlockingSocket_GetMethodHash()
        {
            ZPackage package = new ZPackage();
            package.Write(3);
            package.Write("...");
            int methodHash = PeerInfoBlockingSocket.GetMethodHash(package);

            Assert.Equal(3, methodHash);
        }

        [Fact]
        public void PeerInfoBlockingSocket_GetMethodHash_KeepPackageIntact()
        {
            ZPackage package = new ZPackage();
            package.Write(3);
            package.Write("...");
            int pos = package.GetPos();
            PeerInfoBlockingSocket.GetMethodHash(package);

            Assert.Equal(pos, package.GetPos());
        }

        [Fact]
        public void PeerInfoBlockingSocket_CopyZPackage()
        {
            ZPackage package = new ZPackage();
            package.Write(3);
            package.Write("...");
            ZPackage copy = PeerInfoBlockingSocket.CopyZPackage(package);

            Assert.NotSame(package, copy);
            Assert.Equal(package.GetArray(), copy.GetArray());
            Assert.Equal(package.GetPos(), copy.GetPos());
        }
    }
}
