using JotunnLib.Configs;
using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Entities
{
    public class CustomPiece
    {
        public GameObject PiecePrefab { get; private set; }
        
        private PieceConfig _config;
        private Piece _piece;
        public Piece Piece
        {
            get => GetPiece();
            set => _piece = value;
        }

        public bool FixReference { get; set; } = false;

        public CustomPiece(GameObject piecePrefab, bool fixReference)
        {
            PiecePrefab = piecePrefab;
            _piece = piecePrefab.GetComponent<Piece>();
            FixReference = fixReference;
        }

        public CustomPiece(GameObject piecePrefab, PieceConfig pieceConfig)
        {
            PiecePrefab = piecePrefab;
            _config = pieceConfig;
        }

        public CustomPiece(string name, bool addZNetView = true)
        {
            PiecePrefab = PrefabManager.Instance.CreateEmptyPrefab(name, addZNetView);
            // add Piece?
        }

        public CustomPiece(string name, string baseName)
        {
            PiecePrefab = PrefabManager.Instance.CreateClonedPrefab(name, baseName);
            _piece = PiecePrefab.GetComponent<Piece>();
        }

        public CustomPiece(AssetBundle assetBundle, string assetName, bool fixReference)
        {
            PiecePrefab = (GameObject)assetBundle.LoadAsset(assetName);
            _piece = PiecePrefab.GetComponent<Piece>();
            FixReference = fixReference;
        }

        private Piece GetPiece()
        {
            if (_piece != null)
            {
                return _piece;
            }

            if (_config != null)
            {
                return _config.GetPiece();
            }

            return null;
        }

        public bool IsValid()
        {
            return PiecePrefab && Piece; // && Piece.IsValid(); implement that?
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
    }
}
