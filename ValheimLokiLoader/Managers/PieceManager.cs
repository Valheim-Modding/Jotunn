using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ValheimLokiLoader.Managers
{
    public static class PieceManager
    {
        public static event EventHandler LoadPieces;
        private static Dictionary<string, PieceTable> pieceTables = new Dictionary<string, PieceTable>();
        private static bool piecesLoaded = false;

        internal static void Init()
        {
            SceneManager.sceneLoaded += loadPieceTables;
        }

        private static void loadPieceTables(Scene scene, LoadSceneMode mode)
        {
            // Only load once
            if (scene.name != "main" || piecesLoaded)
            {
                return;
            }

            pieceTables.Clear();
            Debug.Log("---- Loading piece tables ----");

            foreach (PieceTable table in Resources.FindObjectsOfTypeAll(typeof(PieceTable)))
            {
                string name = table.gameObject.name;
                pieceTables.Add(name, table);

                Debug.Log("Loaded piece table: " + name);
            }

            Debug.Log("---- Loading pieces ----");
            LoadPieces(null, null);
            piecesLoaded = true;
        }

        public static void AddToPieceTable(string pieceTable, string prefabName)
        {
            // TODO: Error check if prefab exists
            pieceTables[pieceTable].m_pieces.Add(ZNetScene.instance.GetPrefab(prefabName));
            Debug.Log("Added piece: " + prefabName + " to " + pieceTable);
        }

        public static void AddToPieceTable(string pieceTable, GameObject prefab)
        {
            pieceTables[pieceTable].m_pieces.Add(prefab);
            Debug.Log("Added piece: " + prefab.name + " to " + pieceTable);
        }
    }
}
