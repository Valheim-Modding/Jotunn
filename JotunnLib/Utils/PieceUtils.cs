using System;
using System.Linq;

namespace Jotunn.Utils
{
    internal static class PieceUtils
    {
        public static int VanillaMaxPieceCategory { get; } = GetMaxPieceCategory();

        private static int GetMaxPieceCategory()
        {
            var indexOfMax = Array.IndexOf(Enum.GetNames(typeof(Piece.PieceCategory)), nameof(Piece.PieceCategory.Max));
            var values = Enum.GetValues(typeof(Piece.PieceCategory)).Cast<int>().ToArray();

            if (indexOfMax >= 0)
            {
                return values[indexOfMax];
            }

            Logger.LogWarning("Could not find Piece.PieceCategory.Max, using fallback value 4");
            return 4;
        }
    }
}
