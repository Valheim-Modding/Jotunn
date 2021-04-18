using JotunnLib.Entities;
using UnityEngine;

namespace JotunnLib.Configs
{
    public class PieceConfig
    {
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public bool AllowedInDungeons { get; set; } = false;
        public string PieceTable { get; set; } = string.Empty;
        public string CraftingStation { get; set; } = string.Empty;
        public string ExtendStation { get; set; } = string.Empty;
        public Sprite Icon { get; set; } = null;
        public RequirementConfig[] Requirements { get; set; } = new RequirementConfig[0];

        public Piece.Requirement[] GetRequirements()
        {
            Piece.Requirement[] reqs = new Piece.Requirement[Requirements.Length];

            for (int i = 0; i < reqs.Length; i++)
            {
                reqs[i] = Requirements[i].GetRequirement();
            }

            return reqs;
        }

        public void Apply(GameObject prefab)
        {
            var piece = prefab.GetComponent<Piece>();
            if (piece == null)
            {
                Logger.LogWarning($"GameObject has no Piece attached");
                return;
            }

            piece.enabled = Enabled;
            piece.m_allowedInDungeons = AllowedInDungeons;
            
            // Set icon if overriden
            if (Icon != null)
            {
                piece.m_icon = Icon;
            }

            // Assign the CraftingStation for this piece, if needed
            if (!string.IsNullOrEmpty(CraftingStation))
            {
                piece.m_craftingStation = Mock<CraftingStation>.Create(CraftingStation);
            }

            // Assign all needed resources for this piece
            piece.m_resources = GetRequirements();

            //TODO: resolve that stuff
            /*// Try to assign the effect prefabs of another extension defined in ExtendStation
            var stationExt = prefab.GetComponent<StationExtension>();
            if (stationExt != null && !string.IsNullOrEmpty(ExtendStation))
            {
                var stationPrefab = pieceTable.m_pieces.Find(x => x.name == ExtendStation);
                if (stationPrefab != null)
                {
                    var station = stationPrefab.GetComponent<CraftingStation>();
                    stationExt.m_craftingStation = station;
                }

                var otherExt = pieceTable.m_pieces.Find(x => x.GetComponent<StationExtension>() != null);
                if (otherExt != null)
                {
                    var otherStationExt = otherExt.GetComponent<StationExtension>();
                    var otherPiece = otherExt.GetComponent<Piece>();

                    stationExt.m_connectionPrefab = otherStationExt.m_connectionPrefab;
                    piece.m_placeEffect.m_effectPrefabs = otherPiece.m_placeEffect.m_effectPrefabs.ToArray();
                }
            }
            // Otherwise just copy the effect prefabs of any piece within the table
            else
            {
                var otherPiece = pieceTable.m_pieces.Find(x => x.GetComponent<Piece>() != null).GetComponent<Piece>();
                piece.m_placeEffect.m_effectPrefabs.AddRangeToArray(otherPiece.m_placeEffect.m_effectPrefabs);
            }*/
        }
    }
}
