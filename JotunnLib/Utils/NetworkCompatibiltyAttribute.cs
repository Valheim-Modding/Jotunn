using System;

namespace Jotunn.Utils
{
    /// <summary>
    /// Enum used for telling whether or not the mod should be needed by everyone in multiplayer games.
    /// Also can specify if the mod does not work in multiplayer.
    /// </summary>
    public enum CompatibilityLevel
    {
        NoNeedForSync = 0,
        EveryoneMustHaveMod = 1
    }

    /// <summary>
    /// Enum used for telling whether or not the same mod version should be used by both the server and the clients.
    /// This enum is only useful if CompatibilityLevel.EveryoneMustHaveMod was chosen.
    /// </summary>
    public enum VersionStrictness : int
    {
        None = 0,
        Major = 1,
        Minor = 2,
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
    public class NetworkCompatibiltyAttribute : Attribute
    {
        public CompatibilityLevel EnforceModOnClients { get; set; }

        public VersionStrictness EnforceSameVersion { get; set; }

        public NetworkCompatibiltyAttribute(CompatibilityLevel enforceMod, VersionStrictness enforceVersion)
        {
            EnforceModOnClients = enforceMod;
            EnforceSameVersion = enforceVersion;
        }
    }
}
