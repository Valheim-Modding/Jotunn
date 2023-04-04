namespace Jotunn.Configs
{
    /// <summary>
    ///     Helper to get existing crafting station names
    /// </summary>
    public static class CraftingStations
    {
        /// <summary>
        ///     No crafting station
        /// </summary>
        public static string None => string.Empty;

        /// <summary>
        ///    Workbench crafting station
        /// </summary>
        public static string Workbench => "piece_workbench";

        /// <summary>
        ///    Forge crafting station
        /// </summary>
        public static string Forge => "forge";

        /// <summary>
        ///     Stonecutter crafting station
        /// </summary>
        public static string Stonecutter => "piece_stonecutter";

        /// <summary>
        ///     Cauldron crafting station
        /// </summary>
        public static string Cauldron => "piece_cauldron";

        /// <summary>
        ///     Artisan table crafting station
        /// </summary>
        public static string ArtisanTable => "piece_artisanstation";

        /// <summary>
        ///     Black forge crafting station
        /// </summary>
        public static string BlackForge => "blackforge";

        /// <summary>
        ///     Galdr table crafting station
        /// </summary>
        public static string GaldrTable => "piece_magetable";
    }
}
