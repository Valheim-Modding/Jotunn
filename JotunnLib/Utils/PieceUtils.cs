using System;
using Jotunn.Configs;

namespace Jotunn.Utils
{
    internal static class PieceUtils
    {
        public static Piece.PieceCategory VanillaMaxPieceCategory { get; } = GetVanillaPieceCategory(nameof(Piece.PieceCategory.Max), Piece.PieceCategory.Max);
        public static Piece.PieceCategory VanillaAllPieceCategory { get; } = GetVanillaPieceCategory(nameof(Piece.PieceCategory.All), Piece.PieceCategory.All);

        private static Piece.PieceCategory GetVanillaPieceCategory(string name, Piece.PieceCategory fallback)
        {
            try
            {
                return (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), name);
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Could not find Piece.PieceCategory {name}, using fallback value {fallback}");
                return fallback;
            }
        }

        /// <summary>
        ///     Parses an array of human readable piece usage tag names into a combined <see cref="Piece.UsageTagFlags"/> value.
        ///     Invalid or unknown names are logged and skipped.
        /// </summary>
        /// <param name="usage">Array of usage tag names, see <see cref="PieceUsages"/> for valid values</param>
        /// <returns>The combined <see cref="Piece.UsageTagFlags"/> for all recognized names</returns>
        public static Piece.UsageTagFlags UsageTagFlagsFromStrings(string[] usage)
        {
            var flags = (Piece.UsageTagFlags)0;

            foreach (var entry in usage)
            {
                var internalName = PieceUsages.GetInternalName(entry);

                try
                {
                    flags |= (Piece.UsageTagFlags)Enum.Parse(typeof(Piece.UsageTagFlags), internalName);
                }
                catch (Exception)
                {
                    Logger.LogWarning($"Could not find Piece.UsageTagFlags {entry}, ignoring");
                }
            }

            return flags;
        }
    }
}
