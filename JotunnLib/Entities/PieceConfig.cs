using JotunnLib.Managers;
using System.Linq;
using UnityEngine;

namespace JotunnLib.Entities
{
    public class PieceConfig
    {
        public string Name { get; set; }
        public string Description { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public string PieceTable { get; set; } = string.Empty;
        public string CraftingStation { get; set; } = string.Empty;
        public string ExtendStation { get; set; } = string.Empty;
        public bool AllowedInDungeons { get; set; } = false;
        public Sprite Icon { get; set; }
        public PieceRequirementConfig[] Requirements { get; set; } = new PieceRequirementConfig[0];

        public Piece.Requirement[] GetRequirements()
        {
            Piece.Requirement[] reqs = new Piece.Requirement[Requirements.Length];

            for (int i = 0; i < reqs.Length; i++)
            {
                reqs[i] = Requirements[i].GetPieceRequirement();
            }

            return reqs;
        }

        public Piece GetPiece()
        {
            var prefab = new GameObject(Name);
            var piece = prefab.AddComponent<Piece>();

            // Assign the piece to the actual PieceTable if not already in there
            var pieceTable = PieceManager.Instance.GetPieceTable(PieceTable);
            if (pieceTable == null)
            {
                Logger.LogWarning($"Could not find piecetable: {PieceTable}");
                return null;
            }

            if (pieceTable.m_pieces.Contains(prefab))
            {
                Logger.LogInfo($"Piece already added to PieceTable {PieceTable}");
                return null;
            }

            pieceTable.m_pieces.Add(prefab);

            // Assign the CraftingStation for this piece, if needed
            if (!string.IsNullOrEmpty(CraftingStation))
            {
                GameObject craftingStationPrefab = PrefabManager.Instance.GetPrefab(CraftingStation);
                CraftingStation craftingStation = craftingStationPrefab.GetComponent<CraftingStation>();
                if (craftingStation == null)
                {
                    Logger.LogWarning($"Could not find crafting station: {CraftingStation}");
                }
                else
                {
                    piece.m_craftingStation = craftingStation;
                }
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

            return piece;
        }
    }
}
