using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Helper to get existing piece usage tag names
    /// </summary>
    public static class PieceUsages
    {
        /// <summary>
        ///     Piece 'Misc' usage tag
        /// </summary>
        public static string Misc => nameof(Piece.UsageTagFlags.Misc);

        /// <summary>
        ///     Piece 'Crafting' usage tag
        /// </summary>
        public static string Crafting => nameof(Piece.UsageTagFlags.Crafting);

        /// <summary>
        ///     Piece 'Building' usage tag
        /// </summary>
        public static string Building => nameof(Piece.UsageTagFlags.Building);

        /// <summary>
        ///     Piece 'Floor' usage tag
        /// </summary>
        public static string Floor => nameof(Piece.UsageTagFlags.Floor);

        /// <summary>
        ///     Piece 'Wall' usage tag
        /// </summary>
        public static string Wall => nameof(Piece.UsageTagFlags.Wall);

        /// <summary>
        ///     Piece 'Roof' usage tag
        /// </summary>
        public static string Roof => nameof(Piece.UsageTagFlags.Roof);

        /// <summary>
        ///     Piece 'Architecture' usage tag
        /// </summary>
        public static string Architecture => nameof(Piece.UsageTagFlags.Architecture);

        /// <summary>
        ///     Piece 'Furniture' usage tag
        /// </summary>
        public static string Furniture => nameof(Piece.UsageTagFlags.Furniture);

        /// <summary>
        ///     Piece 'Lighting' usage tag
        /// </summary>
        public static string Lighting => nameof(Piece.UsageTagFlags.Lighting);

        /// <summary>
        ///     Piece 'Decor' usage tag
        /// </summary>
        public static string Decor => nameof(Piece.UsageTagFlags.Decor);

        /// <summary>
        ///     Piece 'Storage' usage tag
        /// </summary>
        public static string Storage => nameof(Piece.UsageTagFlags.Storage);

        /// <summary>
        ///     Piece 'Transport' usage tag
        /// </summary>
        public static string Transport => nameof(Piece.UsageTagFlags.Transport);

        /// <summary>
        ///     Piece 'Food' usage tag
        /// </summary>
        public static string Food => nameof(Piece.UsageTagFlags.Food);

        /// <summary>
        ///     Piece 'Meads' usage tag
        /// </summary>
        public static string Meads => nameof(Piece.UsageTagFlags.Meads);

        /// <summary>
        ///     Piece 'Feasts' usage tag
        /// </summary>
        public static string Feasts => nameof(Piece.UsageTagFlags.Feasts);

        /// <summary>
        ///     Piece 'Defense' usage tag
        /// </summary>
        public static string Defense => nameof(Piece.UsageTagFlags.Defense);

        /// <summary>
        ///     Piece 'Stacks' usage tag
        /// </summary>
        public static string Stacks => nameof(Piece.UsageTagFlags.Stacks);

        /// <summary>
        ///     Piece 'Stairs' usage tag
        /// </summary>
        public static string Stairs => nameof(Piece.UsageTagFlags.Stairs);

        /// <summary>
        ///     Piece 'Doors' usage tag
        /// </summary>
        public static string Doors => nameof(Piece.UsageTagFlags.Doors);

        /// <summary>
        ///     Piece 'Seasonal' usage tag
        /// </summary>
        public static string Seasonal => nameof(Piece.UsageTagFlags.Seasonal);

        /// <summary>
        ///     Gets the human readable name to internal names map
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetNames()
        {
            return NamesMap;
        }

        /// <summary>
        ///     Get a <see cref="BepInEx.Configuration.AcceptableValueList{T}"/> of all piece usage tag names.<br/>
        ///     Since a piece can have any number of tags, store them as one comma separated <see cref="string"/>
        ///     Example:
        ///     <code>
        ///         var pieceUsageConfig = Config.Bind("Piece", "Hammer Usages", $"{PieceUsages.Storage},{PieceUsages.Doors}", $"Comma separated usage tags.\n{PieceUsages.GetAcceptableValueList().ToDescriptionString()}");
        ///         pieceConfig.Usage = pieceUsageConfig.Value.Split(',');
        ///     </code>
        /// </summary>
        /// <returns></returns>
        public static AcceptableValueList<string> GetAcceptableValueList()
        {
            return AcceptableValues;
        }

        /// <summary>
        ///     Get the internal name for a piece usage tag from its human readable name.
        /// </summary>
        /// <param name="pieceUsage"></param>
        /// <returns>
        ///     The matched internal name.
        ///     If the pieceUsage parameter is null or empty, an empty string is returned.
        ///     Otherwise the unchanged pieceUsage parameter is returned.
        /// </returns>
        public static string GetInternalName(string pieceUsage)
        {
            if (string.IsNullOrEmpty(pieceUsage))
            {
                return string.Empty;
            }

            if (NamesMap.TryGetValue(pieceUsage, out string internalName))
            {
                return internalName;
            }

            return pieceUsage;
        }

        private static readonly Dictionary<string, string> NamesMap = new Dictionary<string, string>
        {
            { nameof(Misc), Misc },
            { nameof(Crafting), Crafting },
            { nameof(Building), Building },
            { nameof(Floor), Floor },
            { nameof(Wall), Wall },
            { nameof(Roof), Roof },
            { nameof(Architecture), Architecture },
            { nameof(Furniture), Furniture },
            { nameof(Lighting), Lighting },
            { nameof(Decor), Decor },
            { nameof(Storage), Storage },
            { nameof(Transport), Transport },
            { nameof(Food), Food },
            { nameof(Meads), Meads },
            { nameof(Feasts), Feasts },
            { nameof(Defense), Defense },
            { nameof(Stacks), Stacks },
            { nameof(Stairs), Stairs },
            { nameof(Doors), Doors },
            { nameof(Seasonal), Seasonal },
        };

        private static readonly AcceptableValueList<string> AcceptableValues = new AcceptableValueList<string>(NamesMap.Keys.ToArray());
    }
}
