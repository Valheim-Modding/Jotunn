using JotunnLib.Configs;
using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Entities
{
    public class CustomPiece
    {
        public GameObject PiecePrefab { get; set; }
        public Piece Piece { get; set; } = null;
        public bool FixReference { get; set; } = false;

        public CustomPiece(GameObject piecePrefab, bool fixReference)
        {
            PiecePrefab = piecePrefab;
            Piece = piecePrefab.GetComponent<Piece>();
            FixReference = fixReference;
        }

        public CustomPiece(GameObject piecePrefab, PieceConfig pieceConfig)
        {
            PiecePrefab = piecePrefab;
            Piece = piecePrefab.GetComponent<Piece>();
            pieceConfig.Apply(piecePrefab);
            FixReference = true;
        }

        //TODO: constructors for cloned / empty prefabs with configs.
        //TODO: cloning must use cache. everything must be usable before znetscene

        public CustomPiece(string name, bool addZNetView = true)
        {
            PiecePrefab = PrefabManager.Instance.CreateEmptyPrefab(name, addZNetView);
            // add Piece?
        }

        public CustomPiece(string name, string baseName)
        {
            PiecePrefab = PrefabManager.Instance.CreateClonedPrefab(name, baseName);
            Piece = PiecePrefab.GetComponent<Piece>();
        }

        public CustomPiece(AssetBundle assetBundle, string assetName, bool fixReference)
        {
            PiecePrefab = (GameObject)assetBundle.LoadAsset(assetName);
            Piece = PiecePrefab.GetComponent<Piece>();
            FixReference = fixReference;
        }

        public bool IsValid()
        {
            return PiecePrefab && Piece && Piece.IsValid();
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
