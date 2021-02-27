using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ValheimLokiLoader.Managers
{
    public static class PieceManager
    {
        public static event EventHandler PieceLoad;

        private static Dictionary<string, PieceTable> pieceTables = new Dictionary<string, PieceTable>();
        private static Dictionary<string, string> pieceTableNameMap = new Dictionary<string, string>()
        {
            { "cultivator", "_CultivatorPieceTable" },
            { "hammer", "_HammerPieceTable" },
            { "hoe", "_HoePieceTable" }
        };
        private static bool loaded = false;

        internal static void LoadPieces()
        {
            pieceTables.Clear();
            Debug.Log("---- Loading piece tables ----");

            foreach (PieceTable table in Resources.FindObjectsOfTypeAll(typeof(PieceTable)))
            {
                string name = table.gameObject.name;
                pieceTables.Add(name, table);

                Debug.Log("Loaded piece table: " + name);
            }

            Debug.Log("---- Loading pieces ----");
            PieceLoad?.Invoke(null, EventArgs.Empty);
            loaded = true;
        }

        public static void AddToPieceTable(string pieceTable, string prefabName)
        {
            PieceTable table = getPieceTable(pieceTable);
            GameObject prefab = PrefabManager.GetPrefab(prefabName);

            if (!table)
            {
                Debug.LogError("Piece table does not exist: " + pieceTable);
                return;
            }

            if (!prefab)
            {
                Debug.LogError("Prefab does not exist: " + prefabName);
                return;
            }

            table.m_pieces.Add(prefab);
            Debug.Log("Added piece: " + prefabName + " to " + pieceTable);
        }

        public static void AddToPieceTable(string pieceTable, GameObject prefab)
        {
            PieceTable table = getPieceTable(pieceTable);

            if (!table)
            {
                Debug.LogError("Piece table does not exist: " + pieceTable);
                return;
            }

            table.m_pieces.Add(prefab);
            Debug.Log("Added piece: " + prefab.name + " to " + pieceTable);
        }

        private static PieceTable getPieceTable(string name)
        {
            if (pieceTables.ContainsKey(name))
            {
                return pieceTables[name];
            }

            if (pieceTableNameMap.ContainsKey(name))
            {
                return pieceTables[pieceTableNameMap[name]];
            }

            return null;
        }
    }
}
