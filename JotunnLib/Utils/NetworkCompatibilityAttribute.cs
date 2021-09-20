using System;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Determines the level of compatibility of a mod which is enforced by Jötunn.
    ///     Servers disconnect clients with mods which enforce their compatibility when 
    ///     the version does not match as defined by the VersionStrictness attribute.
    /// </summary>
    public enum CompatibilityLevel
    {
        /// <summary>
        ///     Mod is not checked at all, VersionsStrictness does not apply.
        /// </summary>
        [Obsolete("Use NotEnforced instead")]
        NoNeedForSync = 0,
        /// <summary>
        ///     Mod is checked only if the client and server have loaded it and ignores if just one side has it.
        /// </summary>
        [Obsolete("Use VersionCheckOnly")]
        OnlySyncWhenInstalled = 1,
        /// <summary>
        ///     Mod must be loaded on server and client. Version checking depends on the VersionStrictness.
        /// </summary>
        EveryoneMustHaveMod = 2,
        /// <summary>
        ///     If mod is installed on the server, every client has to have it. VersionStrictness does not apply.
        /// </summary>
        ClientMustHaveMod = 3,
        /// <summary>
        ///     If mod is installed on the client, the server has to have it. VersionStrictness does not apply.
        /// </summary>
        ServerMustHaveMod = 4,
        /// <summary>
        ///     Version check is performed when both server and client have the mod, no check if the mod is actually installed.
        /// </summary>
        VersionCheckOnly = 5,
        /// <summary>
        ///     Mod is not checked at all, VersionsStrictness does not apply.
        /// </summary>
        NotEnforced = 6
    }

    /// <summary>
    ///     Enum used for telling whether or not the same mod version should be used by both the server and the clients.
    ///     This enum is only useful if CompatibilityLevel.EveryoneMustHaveMod or OnlySyncWhenInstalled was chosen.
    /// </summary>
    public enum VersionStrictness : int
    {
        /// <summary>
        ///     No version check is done
        /// </summary>
        None = 0,
        /// <summary>
        ///     Mod must have the same Major version
        /// </summary>
        Major = 1,
        /// <summary>
        ///     Mods must have the same Minor version
        /// </summary>
        Minor = 2,
        /// <summary>
        ///     Mods must have the same Patch version
        /// </summary>
        Patch = 3
    }

    /// <summary>
    /// Mod compatibility attribute<br />
    /// <br/>
    /// PLEASE READ<br />
    /// Example usage:<br />
    /// If your mod adds its own RPCs, EnforceModOnClients is likely a must (otherwise clients would just discard the messages from the server), same version you do have to determine, if your sent data changed<br />
    /// If your mod adds items, you always should enforce mods on client and same version (there could be nasty side effects with different versions of an item)<br />
    /// If your mod is just GUI changes (for example bigger inventory, additional equip slots) there is no need to set this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class NetworkCompatibilityAttribute: Attribute
    {
        /// <summary>
        ///     Compatibility Level
        /// </summary>
        public CompatibilityLevel EnforceModOnClients { get; set; }

        /// <summary>
        ///     Version Strictness
        /// </summary>
        public VersionStrictness EnforceSameVersion { get; set; }

        /// <summary>
        ///     Network Compatibility Attribute
        /// </summary>
        /// <param name="enforceMod"></param>
        /// <param name="enforceVersion"></param>
        public NetworkCompatibilityAttribute(CompatibilityLevel enforceMod, VersionStrictness enforceVersion)
        {
            EnforceModOnClients = enforceMod;
            EnforceSameVersion = enforceVersion;
        }
    }
}
