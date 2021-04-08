using JotunnLib.Configs;
using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Entities
{
    public class CustomPiece
    {
        public GameObject PiecePrefab { get; set; }
        public Piece Piece { get; set; } = null;
        public string PieceTable { get; set; } = string.Empty;
        public bool FixReference { get; set; } = false;

        public CustomPiece(GameObject piecePrefab, string pieceTable, bool fixReference)
        {
            PiecePrefab = piecePrefab;
            Piece = piecePrefab.GetComponent<Piece>();
            PieceTable = pieceTable;
            FixReference = fixReference;
        }

        public CustomPiece(GameObject piecePrefab, PieceConfig pieceConfig)
        {
            PiecePrefab = piecePrefab;
            Piece = piecePrefab.GetComponent<Piece>();
            PieceTable = pieceConfig.PieceTable;
            FixReference = true;

            pieceConfig.Apply(piecePrefab);
        }

        public CustomPiece(AssetBundle assetBundle, string assetName, string pieceTable, bool fixReference)
        {
            PiecePrefab = (GameObject)assetBundle.LoadAsset(assetName);
            if (PiecePrefab)
            {
                Piece = PiecePrefab.GetComponent<Piece>();
            }
            PieceTable = pieceTable;
            FixReference = fixReference;
        }

        public CustomPiece(AssetBundle assetBundle, string assetName, PieceConfig pieceConfig)
        {
            var piecePrefab = (GameObject)assetBundle.LoadAsset(assetName);
            if (piecePrefab)
            {
                PiecePrefab = piecePrefab;
                Piece = piecePrefab.GetComponent<Piece>();
                PieceTable = pieceConfig.PieceTable;

                pieceConfig.Apply(piecePrefab);
            }
            FixReference = true;
        }

        //TODO: constructors for cloned / empty prefabs with configs.

        public CustomPiece(string name, string pieceTable, bool addZNetView = true)
        {
            PiecePrefab = PrefabManager.Instance.CreateEmptyPrefab(name, addZNetView);
            if (PiecePrefab)
            {
                Piece = PiecePrefab.AddComponent<Piece>();
                if (name[0] != LocalizationManager.TokenFirstChar)
                {
                    Piece.m_name = LocalizationManager.TokenFirstChar + name;
                }
                else
                {
                    Piece.m_name = name;
                }
            }
            PieceTable = pieceTable;
        }

        public CustomPiece(string name, string baseName, string pieceTable)
        {
            PiecePrefab = PrefabManager.Instance.CreateClonedPrefab(name, baseName);
            if (PiecePrefab)
            {
                Piece = PiecePrefab.GetComponent<Piece>();
            }
            PieceTable = pieceTable;
        }

        public bool IsValid()
        {
            return PiecePrefab && Piece && Piece.IsValid() && !string.IsNullOrEmpty(PieceTable);
        }

        public static bool IsCustomPiece(string prefabName)
        {
            foreach (var customPiece in PieceManager.Instance.Pieces)
            {
                if (customPiece.PiecePrefab.name == prefabName)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return PiecePrefab.name.GetStableHashCode();
        }

        public override string ToString()
        {
            return PiecePrefab.ToString();
        }
    }
}
