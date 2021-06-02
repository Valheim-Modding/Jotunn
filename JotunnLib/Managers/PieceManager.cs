using Jotunn.Entities;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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

        /// <summary>
        ///     Event that gets fired after all pieces were added to their respective PieceTables.
        ///     Your code will execute every time a new ObjectDB is created (on every game start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnPiecesRegistered;

        internal readonly List<CustomPiece> Pieces = new List<CustomPiece>();
        internal readonly List<CustomPieceTable> PieceTables = new List<CustomPieceTable>();

        internal readonly Dictionary<string, PieceTable> PieceTableMap = new Dictionary<string, PieceTable>();
        internal readonly Dictionary<string, string> PieceTableNameMap = new Dictionary<string, string>();
        internal readonly Dictionary<string, PieceTableCategories> PieceTableCategoriesMap = new Dictionary<string, PieceTableCategories>();

        internal readonly Dictionary<string, Piece.PieceCategory> PieceCategories = new Dictionary<string, Piece.PieceCategory>();
        private Piece.PieceCategory PieceCategoryMax = Piece.PieceCategory.Max;

        /// <summary>
        ///     Creates the piece table container and registers all hooks.
        /// </summary>
        public void Init()
        {
            // Setup Hooks
            On.ObjectDB.Awake += RegisterCustomData;
            On.Player.Load += ReloadKnownRecipes;

            // Fire events as a late action in the detour so all mods can load before
            // Leave space for mods to forcefully run after us. 1000 is an arbitrary "good amount" of space.
            using (new DetourContext(int.MaxValue - 1000))
            {
                On.ObjectDB.Awake += InvokeOnPiecesRegistered;
            }

            /*// Setup vanilla tables
            PieceTableNameMap.Add("Hammer", "_HammerPieceTable");
            PieceTableCategoriesMap.Add("_HammerPieceTable", new PieceTableCategories
            {
                { Piece.PieceCategory.Misc.ToString(), Piece.PieceCategory.Misc },
                { Piece.PieceCategory.Crafting.ToString(), Piece.PieceCategory.Crafting },
                { Piece.PieceCategory.Building.ToString(), Piece.PieceCategory.Building },
                { Piece.PieceCategory.Furniture.ToString(), Piece.PieceCategory.Furniture }
            });
            PieceTableNameMap.Add("Hoe", "_HoePieceTable");
            PieceTableNameMap.Add("Cultivator", "_CultivatorPieceTable");*/
        }

        /// <summary>
        ///     Add a <see cref="CustomPieceTable"/> to the game.<br />
        ///     Checks if the custom piece table is valid and unique and adds it to the list of custom piece tables.
        /// </summary>
        /// <param name="customPieceTable">The custom piece table to add.</param>
        /// <returns>true if the custom piece table was added to the manager.</returns>
        public bool AddPieceTable(CustomPieceTable customPieceTable)
        {
            // Assert
            if (!customPieceTable.IsValid())
            {
                Logger.LogWarning($"Custom piece {customPieceTable} is not valid");
                return false;
            }
            if (PieceTables.Contains(customPieceTable))
            {
                Logger.LogWarning($"Piece table {customPieceTable} already added");
                return false;
            }

            // Add the prefab to the PrefabManager
            PrefabManager.Instance.AddPrefab(customPieceTable.PieceTablePrefab);

            // Create all custom categories on that table
            if (customPieceTable.Categories.Length > 0)
            {
                foreach (var category in customPieceTable.Categories)
                {
                    AddPieceCategory(customPieceTable.ToString(), category);
                }
            }

            // Add the custom table to the PieceManager
            PieceTables.Add(customPieceTable);
            PieceTableMap.Add(customPieceTable.ToString(), customPieceTable.PieceTable);

            return true;
        }

        /// <summary>
        ///     Add a new <see cref="PieceTable"/> from <see cref="GameObject"/>.<br />
        ///     Creates a <see cref="CustomPieceTable"/> and adds it to the manager.
        /// </summary>
        /// <param name="prefab">The <see cref="GameObject"/> to add.</param>
        [Obsolete("Use CustomPieceTable instead")]
        public void AddPieceTable(GameObject prefab)
        {
            AddPieceTable(new CustomPieceTable(prefab));
        }

        /// <summary>
        ///     Add a new <see cref="PieceTable"/> from string.<br />
        ///     Creates a <see cref="CustomPieceTable"/> and adds it to the manager.
        /// </summary>
        /// <param name="name">Name of the new piece table.</param>
        [Obsolete("Use CustomPieceTable instead")]
        public void AddPieceTable(string name)
        {
            GameObject obj = new GameObject(name);
            obj.AddComponent<PieceTable>();
            AddPieceTable(new CustomPieceTable(obj));
        }

        /// <summary>
        ///     Get a <see cref="global::PieceTable"/> by name.<br /><br />
        ///     Search hierarchy:<br />
        ///     <list type="number">
        ///         <item>PieceTable with the exact name (e.g. "_HammerPieceTable")</item>
        ///         <item>PieceTable via "item" name (e.g. "Hammer")</item>
        ///     </list>
        /// </summary>
        /// <param name="name">Prefab or item name of the PieceTable</param>
        /// <returns>PieceTable prefab</returns>
        public PieceTable GetPieceTable(string name)
        {
            if (PieceTableMap.ContainsKey(name))
            {
                return PieceTableMap[name];
            }

            if (PieceTableNameMap.ContainsKey(name))
            {
                return PieceTableMap[PieceTableNameMap[name]];
            }

            //return PrefabManager.Cache.GetPrefab<PieceTable>(name);
            return null;
        }

        /// <summary>
        ///     Add a new <see cref="global::Piece.PieceCategory"/> by name. A new category
        ///     gets assigned a random integer for internal use. If you pass a vanilla category
        ///     the actual integer value of the enum is returned. 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="name"></param>
        /// <returns>int value of the vanilla or custom category</returns>
        public Piece.PieceCategory AddPieceCategory(string table, string name)
        {
            if (Enum.IsDefined(typeof(Piece.PieceCategory), name))
            {
                return (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), name);
            }

            if (PieceCategories.ContainsKey(name))
            {
                return PieceCategories[name];
            }

            Piece.PieceCategory categoryID = PieceCategories.Count() + Piece.PieceCategory.Max;
            PieceCategories.Add(name, categoryID);
            PieceCategoryMax++;

            if (!PieceTableCategoriesMap.ContainsKey(table))
            {
                PieceTableCategoriesMap.Add(table, new PieceTableCategories());
            }
            PieceTableCategoriesMap[table].Add(name, categoryID);

            return categoryID;
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
            // Assert
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
            return Pieces.FirstOrDefault(x => x.PiecePrefab.name.Equals(pieceName));
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

            RemovePiece(piece);
        }

        /// <summary>
        ///     Remove a custom piece by its ref.
        /// </summary>
        /// <param name="piece"><see cref="CustomPiece"/> to remove.</param>
        public void RemovePiece(CustomPiece piece)
        {
            if (!Pieces.Contains(piece))
            {
                Logger.LogWarning($"Could not remove piece {piece}: Not found");
                return;
            }

            Pieces.Remove(piece);

            if (piece.PiecePrefab && PrefabManager.Instance.GetPrefab(piece.PiecePrefab.name))
            {
                PrefabManager.Instance.RemovePrefab(piece.PiecePrefab.name);
            }
        }

        /// <summary>
        ///     Loop all items in the game and get all PieceTables used (vanilla and custom ones).
        /// </summary>
        private void LoadPieceTables()
        {
            foreach (var item in ObjectDB.instance.m_items)
            {
                var table = item.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;

                if (table != null)
                {
                    if (!PieceTableMap.ContainsKey(table.name))
                    {
                        PieceTableMap.Add(table.name, table);
                    }
                    if (!PieceTableNameMap.ContainsKey(item.name))
                    {
                        PieceTableNameMap.Add(item.name, table.name);
                    }
                }
            }
        }

        /// <summary>
        ///     Create piece categories per table if custom categories were added.
        /// </summary>
        private void CreatePieceTableCategories()
        {
            if (PieceTableCategoriesMap.Count > 0)
            {
                Logger.LogInfo($"---- Adding custom piece table categories ----");

                // All piece tables using categories
                foreach (var table in PieceTableMap.Values.Where(x => x.m_useCategories))
                {
                    // Create category map if not present
                    PieceTableCategoriesMap.TryGetValue(table.name, out var categories);
                    if (categories == null)
                    {
                        categories = new PieceTableCategories();
                        PieceTableCategoriesMap.Add(table.name, categories);
                    }

                    // Add vanilla categories for vanilla tables
                    if (PieceTables.Count(x => x.PieceTable.name.Equals(table.name)) == 0)
                    {
                        for (int i = 0; i < (int)Piece.PieceCategory.Max; i++)
                        {
                            categories.Add(Enum.GetName(typeof(Piece.PieceCategory), i), (Piece.PieceCategory)i);
                        }
                    }

                    // Add empty lists up to the max categories count
                    if (table.m_availablePieces.Count < (int)PieceCategoryMax)
                    {
                        for (int i = table.m_availablePieces.Count; i < (int)PieceCategoryMax; i++)
                        {
                            table.m_availablePieces.Add(new List<Piece>());
                        }
                    }

                    // Resize selectedPiece array
                    Array.Resize(ref table.m_selectedPiece, table.m_availablePieces.Count);

                    Logger.LogInfo($"Added categories for table {table}");
                }

                // Hook piece table GUI toogle
                On.Hud.TogglePieceSelection += Hud_TogglePieceSelection;
            }
        }

        /// <summary>
        ///     Hook for piece table GUI toggle. Only active when custom categories were added.
        /// </summary>
        private void Hud_TogglePieceSelection(On.Hud.orig_TogglePieceSelection orig, Hud self)
        {
            // Get currently selected tool and toggle PieceTableCategories
            try
            {
                var table = Player.m_localPlayer.m_buildPieces;
                if (table != null && PieceTableCategoriesMap.ContainsKey(table.name))
                {
                    PieceTableCategoriesMap[table.name].Toggle(!Hud.instance.m_pieceSelectionWindow.activeSelf);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error toggling piece selection window: {ex}");
            }

            orig(self);
        }

        private void RegisterInPieceTables()
        {
            if (Pieces.Count > 0)
            {
                Logger.LogInfo($"---- Adding custom pieces to the PieceTables ----");

                List<CustomPiece> toDelete = new List<CustomPiece>();

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

                        // Assign vfx_ExtensionConnection for StationExtensions
                        var extension = customPiece.PiecePrefab.GetComponent<StationExtension>();
                        if (extension != null && !extension.m_connectionPrefab)
                        {
                            extension.m_connectionPrefab = PrefabManager.Cache.GetPrefab<GameObject>("vfx_ExtensionConnection");
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
                        Logger.LogError($"Error while adding piece {customPiece}: {ex}");
                        toDelete.Add(customPiece);
                    }
                }

                // Delete custom pieces with errors
                foreach (var piece in toDelete)
                {
                    RemovePiece(piece);
                }
            }
        }

        private void RegisterCustomData(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            if (SceneManager.GetActiveScene().name == "main" && self.IsValid())
            {
                LoadPieceTables();
                CreatePieceTableCategories();
                RegisterInPieceTables();
            }
        }

        private void InvokeOnPiecesRegistered(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            if (SceneManager.GetActiveScene().name == "main" && self.IsValid())
            {
                OnPiecesRegistered?.SafeInvoke();
            }
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

        internal class PieceTableCategories : Dictionary<string, Piece.PieceCategory>
        {
            private static GameObject baseTab;

            private const float PieceCategorySize = 540f;
            private const float PieceCategoryTabSize = 120f;

            private Piece.PieceCategory PieceCategoryMax = 0;

            internal void Toggle(bool active)
            {
                if (active)
                {
                    CreateCategoryTabs();
                    On.PieceTable.SetCategory += PieceTable_SetCustomCategory;
                    On.PieceTable.NextCategory += PieceTable_NextCategory;
                    On.PieceTable.PrevCategory += PieceTable_PrevCategory;
                    On.Hud.OnLeftClickCategory += Hud_OnLeftClickCategory;
                }
                else
                {
                    On.PieceTable.SetCategory -= PieceTable_SetCustomCategory;
                    On.PieceTable.NextCategory -= PieceTable_NextCategory;
                    On.PieceTable.PrevCategory -= PieceTable_PrevCategory;
                    On.Hud.OnLeftClickCategory -= Hud_OnLeftClickCategory;
                }
            }

            /// <summary>
            ///     Create tabs for the ingame GUI for every piece table with categories.
            /// </summary>
            private void CreateCategoryTabs()
            {
                // Get the GUI elements
                GameObject root = Hud.instance.m_pieceCategoryRoot;
                if (root.GetComponent<RectMask2D>() == null)
                {
                    root.AddComponent<RectMask2D>();
                    root.SetWidth(PieceCategorySize);
                }

                // Save baseTab prefab
                if (!baseTab)
                {
                    baseTab = Object.Instantiate(Hud.instance.m_pieceCategoryTabs[0]);
                    baseTab.SetActive(false);
                }

                // Remove previous category tabs
                foreach (var tab in Hud.instance.m_pieceCategoryTabs)
                {
                    Object.DestroyImmediate(tab);
                }

                // Append tabs and their names to the GUI for every custom category
                //List<string> newNames = new List<string>(Hud.instance.m_buildCategoryNames);
                //List<GameObject> newTabs = new List<GameObject>(Hud.instance.m_pieceCategoryTabs);
                List<string> newNames = new List<string>(Count);
                List<GameObject> newTabs = new List<GameObject>(Count);

                foreach (var category in this)
                {
                    GameObject newTab = Object.Instantiate(baseTab, root.transform);
                    newTab.SetActive(true);
                    newTab.name = category.Key;

                    UIInputHandler component = newTab.GetComponent<UIInputHandler>();
                    component.m_onLeftDown += Hud.instance.OnLeftClickCategory;

                    newNames.Add(category.Key);
                    newTabs.Add(newTab);
                }

                // Reorder tabs
                float offset = 0f;
                foreach (GameObject go in newTabs)
                {
                    go.SetMiddleLeft();
                    go.SetHeight(30f);
                    RectTransform tf = (go.transform as RectTransform);
                    tf.anchoredPosition = new Vector2(offset, 0f);
                    offset += PieceCategoryTabSize;
                }

                // Replace the HUD arrays
                Hud.instance.m_buildCategoryNames = newNames.ToList();
                Hud.instance.m_pieceCategoryTabs = newTabs.ToArray();
            }

            private void PieceTable_SetCustomCategory(On.PieceTable.orig_SetCategory orig, PieceTable self, int index)
            {
                orig(self, index);

                if (self.m_useCategories)
                {
                    if (ContainsValue((Piece.PieceCategory)index))
                    {
                        self.m_selectedCategory = (Piece.PieceCategory)index;
                    }
                }
            }

            private void PieceTable_NextCategory(On.PieceTable.orig_NextCategory orig, PieceTable self)
            {
                if (self.m_useCategories)
                {
                    self.m_selectedCategory++;

                    if (self.m_selectedCategory == PieceCategoryMax)
                    {
                        self.m_selectedCategory = 0;
                    }
                }

                PieceCategoryScroll(self.m_selectedCategory);
            }

            private void PieceTable_PrevCategory(On.PieceTable.orig_PrevCategory orig, PieceTable self)
            {
                if (self.m_useCategories)
                {
                    self.m_selectedCategory--;

                    if (self.m_selectedCategory < 0)
                    {
                        self.m_selectedCategory = PieceCategoryMax - 1;
                    }
                }

                PieceCategoryScroll(self.m_selectedCategory);
            }

            private void Hud_OnLeftClickCategory(On.Hud.orig_OnLeftClickCategory orig, Hud self, UIInputHandler ih)
            {
                orig(self, ih);

                PieceCategoryScroll(Player.m_localPlayer.m_buildPieces.m_selectedCategory);
            }

            private void PieceCategoryScroll(Piece.PieceCategory selectedCategory)
            {
                var tab = Hud.instance.m_pieceCategoryTabs[(int)selectedCategory];

                float minX = (tab.transform as RectTransform).anchoredPosition.x;
                if (minX < 0f)
                {
                    float offsetX = selectedCategory == 0 ? minX * -1 : PieceCategoryTabSize;
                    foreach (GameObject go in Hud.instance.m_pieceCategoryTabs)
                    {
                        (go.transform as RectTransform).anchoredPosition += new Vector2(offsetX, 0);
                    }
                }
                float maxX = (tab.transform as RectTransform).anchoredPosition.x + PieceCategoryTabSize;
                if (maxX > PieceCategorySize)
                {
                    float offsetX = selectedCategory == PieceCategoryMax - 1 ? maxX - PieceCategorySize : PieceCategoryTabSize;
                    foreach (GameObject go in Hud.instance.m_pieceCategoryTabs)
                    {
                        (go.transform as RectTransform).anchoredPosition -= new Vector2(offsetX, 0);
                    }
                }
            }
        }
    }
}
