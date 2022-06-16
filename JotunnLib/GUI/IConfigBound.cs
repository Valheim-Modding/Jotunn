using BepInEx.Configuration;

namespace Jotunn.GUI
{
    /// <summary>
    ///     Interface for the generic config bind class used in <see cref="InGameConfig.SaveConfiguration"/>
    /// </summary>
    internal interface IConfigBound
    {
        public ConfigEntryBase Entry { get; set; }

        public void Read();

        public void Write();
    }
}
