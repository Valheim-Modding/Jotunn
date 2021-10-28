namespace Jotunn
{
    /// <summary>
    ///     Extends ZNet with a clear diversion between local, client and server instances.
    /// </summary>
    public static class ZNetExtension
    {
        /// <summary>
        ///     Possible states of the game regarding to networking.
        /// </summary>
        public enum ZNetInstanceType
        {
            /// <summary>
            ///     A local game instance playing on a local world.
            /// </summary>
            Local,
            /// <summary>
            ///     A local game instance playing on a dedicated server.
            /// </summary>
            Client,
            /// <summary>
            ///     A dedicated server instance.
            /// </summary>
            Server
        }

        /// <summary>
        ///     Returns true if the game was started locally and a local world was started.
        /// </summary>
        /// <param name="znet"></param>
        /// <returns></returns>
        public static bool IsLocalInstance(this ZNet znet)
        {
            return znet.IsServer() && !znet.IsDedicated();
        }

        /// <summary>
        ///     Returns true if the game was started locally and is connected to a server.
        /// </summary>
        /// <param name="znet"></param>
        /// <returns></returns>
        public static bool IsClientInstance(this ZNet znet)
        {
            return !znet.IsServer() && !znet.IsDedicated();
        }

        /// <summary>
        ///     Returns true if the game was started as a dedicated server.
        /// </summary>
        /// <param name="znet"></param>
        /// <returns></returns>
        public static bool IsServerInstance(this ZNet znet)
        {
            return znet.IsServer() && znet.IsDedicated();
        }

        /// <summary>
        ///     Determine the current game instance type regarding to networking.
        /// </summary>
        /// <param name="znet"></param>
        /// <returns></returns>
        public static ZNetInstanceType GetInstanceType(this ZNet znet)
        {
            if (znet.IsLocalInstance())
            {
                return ZNetInstanceType.Local;
            }

            if (znet.IsClientInstance())
            {
                return ZNetInstanceType.Client;
            }

            return ZNetInstanceType.Server;
        }

        /// <summary>
        ///     Determine if a peer uid is in the admin list on the current <see cref="ZNet"/>
        /// </summary>
        /// <param name="znet"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static bool IsAdmin(this ZNet znet, long uid)
        {
            return znet.m_adminList.Contains(znet.GetPeer(uid).m_socket.GetHostName());
        }
    }
}
