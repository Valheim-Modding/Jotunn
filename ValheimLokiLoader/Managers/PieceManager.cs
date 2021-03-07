using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ValheimLokiLoader.Managers
{
    public static class PieceManager
    {
        public static event EventHandler PieceTableLoad;
        public static event EventHandler PieceLoad;

        private static Dictionary<string, PieceTable> pieceTables = new Dictionary<string, PieceTable>();
        private static Dictionary<string, string> pieceTableNameMap = new Dictionary<string, string>()
        {
            { "Cultivator", "_CultivatorPieceTable" },
            { "Hammer", "_HammerPieceTable" },
            { "Hoe", "_HoePieceTable" }
        };
        private static GameObject pieceTableContainer;
        private static bool loaded = false;

        internal static void Init()
        {
            pieceTableContainer = new GameObject("_LokiPieceTables");
            UnityEngine.Object.DontDestroyOnLoad(pieceTableContainer);

            Debug.Log("Initialized PieceTableManager");
        }

        internal static void LoadPieces()
        {
            pieceTables.Clear();
            
            foreach (Transform child in pieceTableContainer.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            List<string> loadedTables = new List<string>();
            Debug.Log("---- Loading piece tables ----");

            foreach (PieceTable table in Resources.FindObjectsOfTypeAll(typeof(PieceTable)))
            {
                string name = table.gameObject.name;
                pieceTables.Add(name, table);
                loadedTables.Add(name);

                Debug.Log("Loaded existing piece table: " + name);
            }

            PieceTableLoad?.Invoke(null, EventArgs.Empty);

            foreach (var pair in pieceTables)
            {
                PieceTable table = pair.Value;
                string name = table.gameObject.name;

                if (loadedTables.Contains(name))
                {
                    continue;
                }

                pieceTables.Add(name, table);

                Debug.Log("Registered piece table: " + name);
            }

            Debug.Log("---- Loading pieces ----");
            PieceLoad?.Invoke(null, EventArgs.Empty);
            loaded = true;
        }

        public static void RegisterPieceTable(string name)
        {
            if (pieceTables.ContainsKey(name))
            {
                Debug.Log("Cannot register piece table with existing name" + name);
                return;
            }

            GameObject obj = new GameObject(name);
            obj.transform.parent = pieceTableContainer.transform;

            PieceTable table = obj.AddComponent<PieceTable>();
            pieceTables.Add(name, table);
        }

        public static void RegisterPiece(string pieceTable, string prefabName)
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
            Debug.Log("Registered piece: " + prefabName + " to " + pieceTable);
        }

        public static void RegisterPiece(string pieceTable, GameObject prefab)
        {
            PieceTable table = getPieceTable(pieceTable);

            if (!table)
            {
                Debug.LogError("Piece table does not exist: " + pieceTable);
                return;
            }

            table.m_pieces.Add(prefab);
            Debug.Log("Registered piece: " + prefab.name + " to " + pieceTable);
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
