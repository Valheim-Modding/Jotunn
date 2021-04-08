using JotunnLib.Configs;
using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Entities
{
    public class CustomPiece
    {
        /// <summary>
        ///     The prefab for this custom piece.
        /// </summary>
        public GameObject PiecePrefab { get; set; }

        /// <summary>
        ///     The <see cref="global::Piece"/> component for this custom piece as a shortcut. 
        ///     Will not be added again to the prefab when replaced.
        /// </summary>
        public Piece Piece { get; set; } = null;

        /// <summary>
        ///     Name of the <see cref="global::PieceTable"/> this custom piece belongs to.
        /// </summary>
        public string PieceTable { get; set; } = string.Empty;

        /// <summary>
        ///     Indicator if references from <see cref="Mock"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; } = false;

        /// <summary>
        ///     Custom piece from a prefab.<br />
        ///     Will be added to the <see cref="global::PieceTable"/> provided by name.<br />
        ///     Can fix references from <see cref="Mock"/>s or not.
        /// </summary>
        /// <param name="piecePrefab">The prefab for this custom piece.</param>
        /// <param name="pieceTable">
        ///     Name of the <see cref="global::PieceTable"/> the custom piece should be added to.
        ///     Can by the "internal" or the <see cref="GameObject"/>s name (e.g. "_PieceTableHammer" or "Hammer")
        /// </param>
        /// <param name="fixReference">If true references for <see cref="Mock"/> objects get resolved at runtime by Jötunn.</param>
        public CustomPiece(GameObject piecePrefab, string pieceTable, bool fixReference)
        {
            PiecePrefab = piecePrefab;
            Piece = piecePrefab.GetComponent<Piece>();
            PieceTable = pieceTable;
            FixReference = fixReference;
        }

        /// <summary>
        ///     Custom piece from a prefab with a <see cref="PieceConfig"/> attached.<br />
        ///     The members and references from the <see cref="PieceConfig"/> will be referenced by Jötunn at runtime.
        /// </summary>
        /// <param name="piecePrefab">The prefab for this custom piece.</param>
        /// <param name="pieceConfig">The <see cref="PieceConfig"/> for this custom piece.</param>
        public CustomPiece(GameObject piecePrefab, PieceConfig pieceConfig)
        {
            PiecePrefab = piecePrefab;
            Piece = piecePrefab.GetComponent<Piece>();
            PieceTable = pieceConfig.PieceTable;
            FixReference = true;

            pieceConfig.Apply(piecePrefab);
        }

        /// <summary>
        ///     Custom piece from a prefab loaded from an <see cref="AssetBundle"/>.<br />
        ///     Will be added to the <see cref="global::PieceTable"/> provided by name.<br />
        ///     Can fix references from <see cref="Mock"/>s or not.
        /// </summary>
        /// <param name="assetBundle">A preloaded <see cref="AssetBundle"/></param>
        /// <param name="assetName">Name of the prefab in the bundle.</param>
        /// <param name = "pieceTable" >
        ///     Name of the <see cref="global::PieceTable"/> the custom piece should be added to.
        ///     Can by the "internal" or the <see cref="GameObject"/>s name (e.g. "_PieceTableHammer" or "Hammer")
        /// </param>
        /// <param name="fixReference">If true references for <see cref="Mock"/> objects get resolved at runtime by Jötunn.</param>
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

        /// <summary>
        ///     Custom piece from a prefab loaded from an <see cref="AssetBundle"/> with a PieceConfig attached.<br />
        ///     Will be added to the <see cref="global::PieceTable"/> provided by name.
        /// </summary>
        /// <param name="assetBundle">A preloaded <see cref="AssetBundle"/></param>
        /// <param name="assetName">Name of the prefab in the bundle.</param>
        /// <param name="pieceConfig">The <see cref="PieceConfig"/> for this custom piece.</param>
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


        /// <summary>
        ///     Custom piece created as an "empty" primitive.<br />
        ///     Will be added to the <see cref="global::PieceTable"/> provided by name.<br />
        ///     At least the name and the icon of the ItemDrop must be edited after creation.
        /// </summary>
        /// <param name="name">Name of the new prefab. Must be unique.</param>
        /// <param name = "pieceTable" >
        ///     Name of the <see cref="global::PieceTable"/> the custom piece should be added to.
        ///     Can by the "internal" or the <see cref="GameObject"/>s name (e.g. "_PieceTableHammer" or "Hammer")
        /// </param>
        /// <param name="addZNetView">If true a ZNetView component will be added to the prefab for network sync.</param>
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

        /// <summary>
        ///     Custom piece created as a copy of a vanilla Valheim prefab.<br />
        ///     Will be added to the <see cref="global::PieceTable"/> provided by name.
        /// </summary>
        /// <param name="name">The new name of the prefab after cloning.</param>
        /// <param name="baseName">The name of the base prefab the custom item is cloned from.</param>
        /// <param name = "pieceTable" >
        ///     Name of the <see cref="global::PieceTable"/> the custom piece should be added to.
        ///     Can by the "internal" or the <see cref="GameObject"/>s name (e.g. "_PieceTableHammer" or "Hammer")
        /// </param>
        public CustomPiece(string name, string baseName, string pieceTable)
        {
            PiecePrefab = PrefabManager.Instance.CreateClonedPrefab(name, baseName);
            if (PiecePrefab)
            {
                Piece = PiecePrefab.GetComponent<Piece>();
            }
            PieceTable = pieceTable;
        }

        /// <summary>
        ///     Checks if a custom piece is valid (i.e. has a prefab, a target PieceTable is set,
        ///     has a <see cref="global::Piece"/> component and that component has an icon set).
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            return PiecePrefab && Piece && Piece.IsValid() && !string.IsNullOrEmpty(PieceTable);
        }

        /// <summary>
        ///     Helper method to determine if a prefab with a given name is a custom piece created with Jötunn.
        /// </summary>
        /// <param name="prefabName">Name of the prefab to test.</param>
        /// <returns>true if the prefab is added as a custom piece to the <see cref="PieceManager"/>.</returns>
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
            return PiecePrefab.name;
        }
    }
}
