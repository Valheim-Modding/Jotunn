using JotunnLib.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JotunnLib.Managers
{
    /// <summary>
    ///     Handles all logic to do with adding custom Pieces and PieceTables to the game.
    /// </summary>
    public class PieceManager : Manager
    {
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static PieceManager Instance { get; private set; }

        internal readonly Dictionary<string, PieceTable> PieceTables = new Dictionary<string, PieceTable>();
        internal readonly Dictionary<string, string> PieceTableNameMap = new Dictionary<string, string>()
        {
            { "Cultivator", "_CultivatorPieceTable" },
            { "Hammer", "_HammerPieceTable" },
            { "Hoe", "_HoePieceTable" }
        };

        public event EventHandler OnPiecesRegistered;
        public event EventHandler OnPieceTablesRegistered;
        internal GameObject PieceTableContainer;
        internal List<CustomPiece> Pieces = new List<CustomPiece>();

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Cannot have multiple instances of singleton: {GetType()}");
                return;
            }

            Instance = this;
        }

        internal override void Init()
        {
            // Create PieceTable Container
            PieceTableContainer = new GameObject("PieceTables");
            PieceTableContainer.transform.parent = Main.RootObject.transform;

            // Setup Hooks
            On.ObjectDB.Awake += registerCustomData;
            On.Player.Load += reloadKnownRecipes;
        }

        //TODO: Dont know if needed anymore
        /*internal override void Register()
        {
            // TODO: Split register and load logic
        }

        internal override void Load()
        {
            if (loaded)
            {
                return;
            }

            // Clear piece tables and re-load
            PieceTables.Clear();
            
            foreach (Transform child in PieceTableContainer.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            List<string> loadedTables = new List<string>();
            Logger.LogInfo("---- Loading piece tables ----");

            foreach (PieceTable table in Resources.FindObjectsOfTypeAll(typeof(PieceTable)))
            {
                string name = table.gameObject.name;
                PieceTables.Add(name, table);
                loadedTables.Add(name);

                Logger.LogInfo("Loaded existing piece table: " + name);
            }

            PieceTableRegister?.Invoke(null, EventArgs.Empty);

            foreach (var pair in PieceTables)
            {
                PieceTable table = pair.Value;
                string name = table.gameObject.name;

                if (loadedTables.Contains(name))
                {
                    continue;
                }

                PieceTables.Add(name, table);

                Logger.LogInfo("Registered piece table: " + name);
            }

            Logger.LogInfo("---- Loading pieces ----");
            PieceRegister?.Invoke(null, EventArgs.Empty);
            loaded = true;
        }*/

        public void AddPieceTable(GameObject prefab)
        {
            if (PieceTables.ContainsKey(prefab.name))
            {
                Logger.LogWarning($"Piece table {name} already added");
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

            return null;
        }

        public void AddPiece(CustomPiece customPiece)
        {
            if (customPiece.IsValid())
            {
                // Add to the right layer if necessary
                if (customPiece.PiecePrefab.layer == 0)
                {
                    customPiece.PiecePrefab.layer = LayerMask.NameToLayer("piece");
                }

                // Add the prefab to the PrefabManager
                PrefabManager.Instance.AddPrefab(customPiece.PiecePrefab);

                // Add the custom piece to the PieceManager
                Pieces.Add(customPiece);
            }
        }

        private void registerInObjectDB(ObjectDB objectDB)
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
                        throw new Exception($"Could not find piecetable: {customPiece.PieceTable}");
                    }
                    if (pieceTable.m_pieces.Contains(customPiece.PiecePrefab))
                    {
                        Logger.LogInfo($"Already added custom Piece : {customPiece.PiecePrefab.name} | Token : {customPiece.Piece.TokenName()}");
                    }
                    else
                    {
                        pieceTable.m_pieces.Add(customPiece.PiecePrefab);
                        Logger.LogInfo($"Added custom Piece : {customPiece.PiecePrefab.name} | Token : {customPiece.Piece.TokenName()}");
                    }
                    
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding custom item {customPiece.PiecePrefab.name}: {ex.Message}");
                }

            }
        }

        private void registerCustomData(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            var isValid = self.IsValid();
            ItemDropMockFix.Switch(!isValid);

            if (isValid)
            {
                registerInObjectDB(self);
            }

            // Fire event that everything is added and registered
            OnPieceTablesRegistered?.Invoke(null, EventArgs.Empty);
            OnPiecesRegistered?.Invoke(null, EventArgs.Empty);
        }

        private void reloadKnownRecipes(On.Player.orig_Load orig, Player self, ZPackage pkg)
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
