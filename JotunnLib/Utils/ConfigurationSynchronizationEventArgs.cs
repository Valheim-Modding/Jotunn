using System;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Event args class for configuration synchronization event
    /// </summary>
    public class ConfigurationSynchronizationEventArgs : EventArgs
    {
        /// <summary>
        ///     Is this the initial synchronization?
        /// </summary>
        public bool InitialSynchronization { get; set; }
    }
}
