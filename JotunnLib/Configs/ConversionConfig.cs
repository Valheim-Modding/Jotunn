namespace Jotunn.Configs
{
    /// <summary>
    ///     Base class for adding new ItemConversions to various Valheim stations
    /// </summary>
    public abstract class ConversionConfig
    {
        /// <summary>
        ///     The name of the station prefab this conversion is added to.
        /// </summary>
        public string Station { get; set; }

        /// <summary>
        ///     The name of the item prefab you need to put ín the station.
        /// </summary>
        public string FromItem { get; set; }

        /// <summary>
        ///     The name of the item prefab that your "FromItem" will be turned into.
        /// </summary>
        public string ToItem { get; set; }
    }
}
