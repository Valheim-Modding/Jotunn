using System;
using System.Collections.Generic;

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

        /// <summary>
        ///     GUID for each Plugin that received configuration data.
        /// </summary>
        public HashSet<string> UpdatedPluginGUIDs { get; set; }
    }
}
