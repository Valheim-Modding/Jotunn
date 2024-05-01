using System;
using System.Linq;

namespace Jotunn.Utils
{
    internal static class PieceUtils
    {
        public static int VanillaMaxPieceCategory { get; } = GetVanillaMaxPieceCategory();

        private static int GetVanillaMaxPieceCategory()
        {
            try
            {
                return (int)Enum.Parse(typeof(Piece.PieceCategory), nameof(Piece.PieceCategory.Max));
            }
            catch (Exception e)
            {
                Logger.LogWarning("Could not find Piece.PieceCategory.Max, using fallback value 4");
                return 4;
            }
        }
    }
}
