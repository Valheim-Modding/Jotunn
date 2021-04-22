using Jotunn.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for handling custom pieces added to the game.
    /// </summary>
    public class PieceManager : IManager
    {
        private static PieceManager _instance;
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static PieceManager Instance
        {
            get 
            {
                if (_instance == null) _instance = new PieceManager();
                return _instance;
            }
        }
        
        public event EventHandler OnPiecesRegistered;
        public event EventHandler OnPieceTablesRegistered;

        internal GameObject PieceTableContainer;
        internal List<CustomPiece> Pieces = new List<CustomPiece>();

        internal readonly Dictionary<string, PieceTable> PieceTables = new Dictionary<string, PieceTable>();
        internal readonly Dictionary<string, string> PieceTableNameMap = new Dictionary<string, string>()
        {
            { "Cultivator", "_CultivatorPieceTable" },
            { "Hammer", "_HammerPieceTable" },
            { "Hoe", "_HoePieceTable" }
        };


        public void Init()
        {
            // Create PieceTable Container
            PieceTableContainer = new GameObject("PieceTables");
            PieceTableContainer.transform.parent = Main.RootObject.transform;

            // Setup Hooks
            On.ObjectDB.Awake += RegisterCustomData;
            On.Player.Load += ReloadKnownRecipes;
        }

        /// <summary>
        ///     Add a new <see cref="PieceTable"/> from <see cref="GameObject"/>.
        /// </summary>
        /// <param name="prefab">The prefab of the <see cref="PieceTable"/></param>
        public void AddPieceTable(GameObject prefab)
        {
            if (PieceTables.ContainsKey(prefab.name))
            {
                Logger.LogWarning($"Piece table {prefab.name} already added");
                return;
            }

            var table = prefab.GetComponent<PieceTable>();

            if (table == null)
            {
                Logger.LogError($"Game object has no PieceTable attached");
                return;
            }

            prefab.transform.parent = PieceTableContainer.transform;

            PieceTables.Add(prefab.name, table);

            //TODO: get the name of the item which has this table attached and add it to the name map
        }

        /// <summary>
        ///     Add a new <see cref="PieceTable"/> from string.<br />
        ///     Creates a <see cref="GameObject"/> with a <see cref="PieceTable"/> component and adds it to the manager.
        /// </summary>
        /// <param name="name">Name of the new piece table.</param>
        public void AddPieceTable(string name)
        {
            if (PieceTables.ContainsKey(name))
            {
                Logger.LogWarning($"Piece table {name} already added");
                return;
            }

            GameObject obj = new GameObject(name);
            obj.transform.parent = PieceTableContainer.transform;

            PieceTable table = obj.AddComponent<PieceTable>();
            PieceTables.Add(name, table);

            PieceTableNameMap.Add(name, $"_{name}PieceTable");
        }

        /// <summary>
        ///     Get a <see cref="PieceTable"/> by name.<br /><br />
        ///     Search hierarchy:<br />
        ///     <list type="number">
        ///         <item>Custom table with the exact name</item>
        ///         <item>Vanilla table via "item" name (e.g. "Hammer")</item>
        ///         <item>Vanilla table with the exact name (e.g. "_HammerPieceTable")</item>
        ///     </list>
        /// </summary>
        /// <param name="name">Name of the PieceTable.</param>
        /// <returns></returns>
        public PieceTable GetPieceTable(string name)
        {
            if (PieceTables.ContainsKey(name))
            {
                return PieceTables[name];
            }

            if (PieceTableNameMap.ContainsKey(name))
            {
                return PrefabManager.Cache.GetPrefab<PieceTable>(PieceTableNameMap[name]);
            }

            return PrefabManager.Cache.GetPrefab<PieceTable>(name);
        }

        /// <summary>
        ///     Add a <see cref="CustomPiece"/> to the game.<br />
        ///     Checks if the custom piece is valid and unique and adds it to the list of custom pieces.<br />
        ///     Custom pieces are added to their respective <see cref="PieceTable"/>s after <see cref="ObjectDB.Awake"/>.
        /// </summary>
        /// <param name="customPiece">The custom piece to add.</param>
        /// <returns>true if the custom piece was added to the manager.</returns>
        public bool AddPiece(CustomPiece customPiece)
        {
            if (!customPiece.IsValid())
            {
                Logger.LogWarning($"Custom piece {customPiece} is not valid");
                return false;
            }
            if (Pieces.Contains(customPiece))
            {
                Logger.LogWarning($"Custom piece {customPiece} already added");
                return false;
            }

            // Add to the right layer if necessary
            if (customPiece.PiecePrefab.layer == 0)
            {
                customPiece.PiecePrefab.layer = LayerMask.NameToLayer("piece");
            }

            // Add the prefab to the PrefabManager
            PrefabManager.Instance.AddPrefab(customPiece.PiecePrefab);

            // Add the custom piece to the PieceManager
            Pieces.Add(customPiece);

            return true;
        }

        /// <summary>
        ///     Get a custom piece by its name.
        /// </summary>
        /// <param name="pieceName">Name of the piece to search.</param>
        /// <returns></returns>
        public CustomPiece GetPiece(string pieceName)
        {
            return Pieces.Find(x => x.PiecePrefab.name.Equals(pieceName));
        }

        /// <summary>
        ///     Remove a custom piece by its name.
        /// </summary>
        /// <param name="pieceName">Name of the piece to remove.</param>
        public void RemovePiece(string pieceName)
        {
            var piece = GetPiece(pieceName);
            if (piece == null)
            {
                Logger.LogWarning($"Could not remove piece {pieceName}: Not found");
                return;
            }

            Pieces.Remove(piece);
        }

        private void RegisterInPieceTables()
        {
            Logger.LogInfo($"---- Adding custom pieces to the PieceTables ----");

            foreach (var customPiece in Pieces)
            {
                try
                { 
                    // Fix references if needed
                    if (customPiece.FixReference)
                    {
                        customPiece.PiecePrefab.FixReferences();
                        customPiece.FixReference = false;
                    }

                    // Assign the piece to the actual PieceTable if not already in there
                    var pieceTable = GetPieceTable(customPiece.PieceTable);
                    if (pieceTable == null)
                    {
                        throw new Exception($"Could not find piecetable {customPiece.PieceTable}");
                    }
                    if (pieceTable.m_pieces.Contains(customPiece.PiecePrefab))
                    {
                        Logger.LogInfo($"Already added piece {customPiece}");
                    }
                    else
                    {
                        pieceTable.m_pieces.Add(customPiece.PiecePrefab);
                        Logger.LogInfo($"Added piece {customPiece} | Token: {customPiece.Piece.TokenName()}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding piece {customPiece}: {ex.Message}");

                    // Remove piece again
                    PrefabManager.Instance.RemovePrefab(customPiece.PiecePrefab.name);
                    Pieces.Remove(customPiece);
                }

            }
        }

        private void RegisterCustomData(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            var isValid = self.IsValid();
            ItemDropMockFix.Switch(!isValid);

            if (isValid)
            {
                RegisterInPieceTables();
            }

            // Fire event that everything is added and registered
            OnPieceTablesRegistered?.Invoke(null, EventArgs.Empty);
            OnPiecesRegistered?.Invoke(null, EventArgs.Empty);
        }

        private void ReloadKnownRecipes(On.Player.orig_Load orig, Player self, ZPackage pkg)
        {
            orig(self, pkg);

            if (Game.instance == null)
            {
                return;
            }

            self.UpdateKnownRecipesList();
        }
    }
}
