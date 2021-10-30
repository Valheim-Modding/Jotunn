﻿using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using MonoMod.RuntimeDetour;
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

        private const float PieceCategorySize = 700f;
        private const float PieceCategoryTabSize = 150f;

        /// <summary>
        ///     Creates the piece table container and registers all hooks.
        /// </summary>
        public void Init()
        {
            // Setup Hooks
            On.ObjectDB.Awake += RegisterCustomData;
            On.Player.OnSpawned += ReloadKnownRecipes;

            // Fire events as a late action in the detour so all mods can load before
            // Leave space for mods to forcefully run after us. 1000 is an arbitrary "good amount" of space.
            using (new DetourContext(int.MaxValue - 1000))
            {
                On.ObjectDB.Awake += InvokeOnPiecesRegistered;
            }
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
            PrefabManager.Instance.AddPrefab(customPieceTable.PieceTablePrefab, customPieceTable.SourceMod);

            // Create all custom categories on that table
            foreach (var category in customPieceTable.Categories)
            {
                AddPieceCategory(customPieceTable.ToString(), category);
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
            AddPieceTable(new CustomPieceTable(obj, new PieceTableConfig()));
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
        /// <returns><see cref="PieceTable"/> component</returns>
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

            return null;
        }

        /// <summary>
        ///     Add a new <see cref="Piece.PieceCategory"/> by name. A new category
        ///     gets assigned a random integer for internal use. If you pass a vanilla category
        ///     the actual integer value of the enum is returned. 
        /// </summary>
        /// <param name="table">Prefab or item name of the PieceTable.</param>
        /// <param name="name">Name of the category.</param>
        /// <returns>int value of the vanilla or custom category</returns>
        public Piece.PieceCategory AddPieceCategory(string table, string name)
        {
            Piece.PieceCategory categoryID;
            bool isNew = false;

            // Get or create global category ID
            if (Enum.IsDefined(typeof(Piece.PieceCategory), name))
            {
                categoryID = (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), name);
            }
            else if (PieceCategories.ContainsKey(name))
            {
                categoryID = PieceCategories[name];
            }
            else
            {
                categoryID = PieceCategories.Count + Piece.PieceCategory.Max;
                PieceCategories.Add(name, categoryID);
                PieceCategoryMax++;
                isNew = true;
            }

            // Add category to table map
            if (!PieceTableCategoriesMap.ContainsKey(table))
            {
                PieceTableCategoriesMap.Add(table, new PieceTableCategories());
            }
            if (!PieceTableCategoriesMap[table].ContainsKey(name))
            {
                PieceTableCategoriesMap[table].Add(name, categoryID);
            }

            // When called in-game, directly create newly added categories
            if (isNew & SceneManager.GetActiveScene().name == "main")
            {
                CreatePieceTableCategories();
            }
            if (Hud.instance != null)
            {
                CreateCategoryTabs();
            }

            return categoryID;
        }
        
        /// <summary>
        ///     Get a list of all custom piece category names
        /// </summary>
        /// <returns></returns>
        public List<string> GetPieceCategories()
        {
            return PieceCategories.Keys.ToList();
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
            PrefabManager.Instance.AddPrefab(customPiece.PiecePrefab, customPiece.SourceMod);

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
            if (!PieceCategories.Any())
            {
                return;
            }

            //Logger.LogInfo($"Adding {PieceCategories.Count} custom piece table categories");

            // All piece tables using categories
            foreach (var table in PieceTableMap.Values.Where(x => x.m_useCategories))
            {
                try
                {
                    // Create category map if not present
                    PieceTableCategoriesMap.TryGetValue(table.name, out var categories);
                    if (categories == null)
                    {
                        categories = new PieceTableCategories();
                        PieceTableCategoriesMap.Add(table.name, categories);
                    }

                    // Remap shortcut categories to actual table (e.g. categories added to "Hammer" must be added to "_HammerPieceTable")
                    if (PieceTableNameMap.ContainsValue(table.name))
                    {
                        string tableItemName = PieceTableNameMap.FirstOrDefault(x => x.Value.Equals(table.name)).Key;
                        if (PieceTableCategoriesMap.ContainsKey(tableItemName))
                        {
                            foreach (var cat in PieceTableCategoriesMap[tableItemName])
                            {
                                if (!categories.ContainsKey(cat.Key))
                                {
                                    categories.Add(cat.Key, cat.Value);
                                }
                            }
                            PieceTableCategoriesMap.Remove(tableItemName);
                        }
                    }

                    // Add vanilla categories for vanilla tables
                    CustomPieceTable customTable = PieceTables.FirstOrDefault(x => x.PieceTable.name.Equals(table.name));
                    if (customTable == null)
                    {
                        for (int i = 0; i < (int)Piece.PieceCategory.Max; i++)
                        {
                            string categoryName = Enum.GetName(typeof(Piece.PieceCategory), i);
                            if (!categories.ContainsKey(categoryName))
                            {
                                categories.Add(categoryName, (Piece.PieceCategory)i);
                            }
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

                    // Set first available category
                    table.m_selectedCategory = categories.Values.Min();

                    Logger.LogDebug($"Added categories for table {table.name}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding categories for table {table.name}: {ex}");
                }
            }

            // Hook for generation of custom category tabs
            On.Hud.Awake -= CreateCategoryTabs;
            On.Hud.Awake += CreateCategoryTabs;

            // Hook for custom category tab toggle
            On.Hud.UpdateBuild -= TogglePieceCategories;
            On.Hud.UpdateBuild += TogglePieceCategories;
        }
        
        /// <summary>
        ///     Create tabs for the ingame GUI for every piece table category.
        ///     Only executes when custom piece table catego-ries were added.
        /// </summary>
        private void CreateCategoryTabs(On.Hud.orig_Awake orig, Hud self)
        {
            orig(self);
            CreateCategoryTabs();
        }

        private void CreateCategoryTabs()
        {
            // Get the GUI elements
            GameObject root = Hud.instance.m_pieceCategoryRoot;
            if (root.GetComponent<RectMask2D>() == null)
            {
                root.AddComponent<RectMask2D>();
                root.SetWidth(PieceCategorySize);

                Transform border = root.transform.Find("TabBorder");
                border?.SetParent(root.transform.parent, true);
            }

            List<string> newNames = new List<string>(Hud.instance.m_buildCategoryNames);
            List<GameObject> newTabs = new List<GameObject>(Hud.instance.m_pieceCategoryTabs);

            // Append tabs and their names to the GUI for every custom category not already added
            foreach (var category in PieceCategories)
            {
                if (!newNames.Contains(category.Key))
                {
                    GameObject newTab = Object.Instantiate(Hud.instance.m_pieceCategoryTabs[0], root.transform);
                    newTab.name = category.Key;
                    UIInputHandler handler = newTab.GetOrAddComponent<UIInputHandler>();
                    handler.m_onLeftDown += Hud.instance.OnLeftClickCategory;

                    char[] forbiddenCharsArray = LocalizationManager.ForbiddenChars.ToCharArray();
                    string tokenCategory = string.Concat(category.Key.ToLower().Split(forbiddenCharsArray));
                    string tokenName = $"jotunn_cat_{tokenCategory}";
                    LocalizationManager.Instance.JotunnLocalization.AddTranslation(tokenName, category.Key);

                    newNames.Add(LocalizationManager.Instance.TryTranslate(tokenName));
                    newTabs.Add(newTab);
                }
            }

            // Reorder tabs
            float offset = 0f;
            foreach (GameObject go in newTabs)
            {
                go.SetMiddleLeft();
                go.SetWidth(PieceCategoryTabSize);
                RectTransform tf = go.transform as RectTransform;
                tf.anchoredPosition = new Vector2(offset, 0f);
                offset += PieceCategoryTabSize;
            }

            // Replace the HUD arrays
            Hud.instance.m_buildCategoryNames = newNames.ToList();
            Hud.instance.m_pieceCategoryTabs = newTabs.ToArray();
        }

        /// <summary>
        ///     Hook for piece table toggle. Only active when custom categories were added.
        /// </summary>
        private void TogglePieceCategories(On.Hud.orig_UpdateBuild orig, Hud self, Player player, bool forceUpdateAllBuildStatuses)
        {
            // Get currently selected tool and toggle PieceTableCategories
            try
            {
                var table = Player.m_localPlayer.m_buildPieces;
                if (table != null && table.m_useCategories && PieceTableCategoriesMap.ContainsKey(table.name))
                {
                    PieceTableCategoriesMap[table.name].Toggle(Hud.instance.m_pieceSelectionWindow.activeSelf);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error toggling piece selection window: {ex}");
            }

            orig(self, player, forceUpdateAllBuildStatuses);
        }

        /// <summary>
        ///     Registers all custom pieces to their respective piece tables.
        ///     Removes erroneous ones from the manager.
        /// </summary>
        private void RegisterInPieceTables()
        {
            if (!Pieces.Any())
            {
                return;
            }

            Logger.LogInfo($"Adding {Pieces.Count} custom pieces to the PieceTables");

            List<CustomPiece> toDelete = new List<CustomPiece>();

            foreach (var customPiece in Pieces)
            {
                try
                {
                    // Fix references if needed
                    if (customPiece.FixReference | customPiece.FixConfig)
                    {
                        customPiece.PiecePrefab.FixReferences(customPiece.FixReference);
                        customPiece.FixReference = false;
                        customPiece.FixConfig = false;
                    }
                    // Assign vfx_ExtensionConnection for StationExtensions
                    var extension = customPiece.PiecePrefab.GetComponent<StationExtension>();
                    if (extension != null && !extension.m_connectionPrefab)
                    {
                        extension.m_connectionPrefab = PrefabManager.Cache.GetPrefab<GameObject>("vfx_ExtensionConnection");
                    }

                    // Assign the piece to the actual PieceTable if not already in there
                    RegisterPieceInPieceTable(customPiece.PiecePrefab, customPiece.PieceTable);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error caught while adding piece {customPiece}: {ex}");
                    toDelete.Add(customPiece);
                }
            }

            // Delete custom pieces with errors
            foreach (var piece in toDelete)
            {
                if (piece.PiecePrefab)
                {
                    PrefabManager.Instance.DestroyPrefab(piece.PiecePrefab.name);
                }
                RemovePiece(piece);
            }
        }

        /// <summary>
        ///     Register a single piece prefab into a piece table by name.<br />
        ///     Also adds the prefab to the <see cref="PrefabManager"/> and <see cref="ZNetScene"/> if necessary.<br />
        ///     Custom categories can be referenced if they have been added to the manager before.<br />
        ///     No mock references are fixed.
        /// </summary>
        /// <param name="prefab"><see cref="GameObject"/> with a <see cref="Piece"/> component to add to the table</param>
        /// <param name="pieceTable">Prefab or item name of the PieceTable</param>
        /// <param name="category">Optional category string, does not create new custom categories</param>
        public void RegisterPieceInPieceTable(GameObject prefab, string pieceTable, string category = null)
        {
            var piece = prefab.GetComponent<Piece>();
            if (piece == null)
            {
                throw new Exception($"Prefab {prefab.name} has no Piece component attached");
            }

            var table = GetPieceTable(pieceTable);
            if (table == null)
            {
                throw new Exception($"Could not find PieceTable {pieceTable}");
            }

            if (table.m_pieces.Contains(prefab))
            {
                Logger.LogDebug($"Already added piece {prefab.name}");
                return;
            }

            int hash = prefab.name.GetStableHashCode();

            if (!PrefabManager.Instance.Prefabs.ContainsKey(hash))
            {
                PrefabManager.Instance.AddPrefab(prefab);
            }

            if (ZNetScene.instance != null && !ZNetScene.instance.m_namedPrefabs.ContainsKey(hash))
            {
                PrefabManager.Instance.RegisterToZNetScene(prefab);
            }

            if (!string.IsNullOrEmpty(category))
            {
                piece.m_category = AddPieceCategory(pieceTable, category);
            }

            table.m_pieces.Add(prefab);
            Logger.LogDebug($"Added piece {prefab.name} | Token: {piece.TokenName()}");
        }

        private void RegisterCustomData(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            if (SceneManager.GetActiveScene().name == "main")
            {
                LoadPieceTables();
                CreatePieceTableCategories();
                RegisterInPieceTables();
            }
        }

        private void InvokeOnPiecesRegistered(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            if (SceneManager.GetActiveScene().name == "main")
            {
                OnPiecesRegistered?.SafeInvoke();
            }
        }

        /// <summary>
        ///     Hook on <see cref="Player.OnSpawned"/> to refresh recipes for the custom items.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void ReloadKnownRecipes(On.Player.orig_OnSpawned orig, Player self)
        {
            orig(self);

            if (!Pieces.Any())
            {
                return;
            }

            try
            {
                self.UpdateKnownRecipesList();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while reloading player recipes: {ex}");
            }
        }

        internal class PieceTableCategories : Dictionary<string, Piece.PieceCategory>
        {
            private static PieceTableCategories currentActive;

            private Piece.PieceCategory lastCategory = Piece.PieceCategory.All;

            internal void Toggle(bool isActive)
            {
                if (isActive && (currentActive == null || currentActive != this))
                {
                    // Deactivate current active
                    if (currentActive != null && currentActive != this)
                    {
                        currentActive.Toggle(false);
                    }

                    // Activate all tabs for this categories
                    foreach (GameObject tab in Hud.instance.m_pieceCategoryTabs)
                    {
                        tab.SetActive(Keys.Contains(tab.name));
                    }

                    // Reorder tabs
                    ReorderActiveTabs();

                    // Set last selected category
                    if (lastCategory == Piece.PieceCategory.All)
                    {
                        Piece.PieceCategory firstCategory = Values.Min();
                        Player.m_localPlayer.m_buildPieces.m_selectedCategory = firstCategory;
                        PieceCategoryScroll(firstCategory);
                    }
                    else
                    {
                        PieceCategoryScroll(lastCategory);
                    }

                    // Hook navigation
                    On.PieceTable.SetCategory += PieceTable_SetCategory;
                    On.PieceTable.NextCategory += PieceTable_NextCategory;
                    On.PieceTable.PrevCategory += PieceTable_PrevCategory;
                    On.Hud.OnLeftClickCategory += Hud_OnLeftClickCategory;

                    currentActive = this;
                }
                if (!isActive && currentActive != null && currentActive == this)
                {
                    // Activate all vanilla tabs
                    foreach (GameObject tab in Hud.instance.m_pieceCategoryTabs)
                    {
                        tab.SetActive(Enum.GetNames(typeof(Piece.PieceCategory)).Contains(tab.name));
                    }

                    // Reorder tabs
                    ReorderActiveTabs();

                    // Remove hooks
                    On.PieceTable.SetCategory -= PieceTable_SetCategory;
                    On.PieceTable.NextCategory -= PieceTable_NextCategory;
                    On.PieceTable.PrevCategory -= PieceTable_PrevCategory;
                    On.Hud.OnLeftClickCategory -= Hud_OnLeftClickCategory;

                    currentActive = null;
                }
            }

            private void ReorderActiveTabs()
            {
                float offset = 0f;
                foreach (GameObject go in Hud.instance.m_pieceCategoryTabs.Where(x => x.activeSelf))
                {
                    go.SetMiddleLeft();
                    go.SetHeight(30f);
                    RectTransform tf = (go.transform as RectTransform);
                    tf.anchoredPosition = new Vector2(offset, 0f);
                    offset += PieceCategoryTabSize;
                }
            }

            private void PieceTable_SetCategory(On.PieceTable.orig_SetCategory orig, PieceTable self, int index)
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
                var oldCat = self.m_selectedCategory;
                orig(self);

                if (self.m_useCategories)
                {
                    if (self.m_selectedCategory != oldCat)
                    {
                        self.m_selectedCategory = oldCat;
                        do
                        {
                            self.m_selectedCategory++;

                            if (self.m_selectedCategory == Instance.PieceCategoryMax)
                            {
                                self.m_selectedCategory = 0;
                            }
                        }
                        while (!Values.Contains(self.m_selectedCategory));
                    }
                }

                PieceCategoryScroll(self.m_selectedCategory);
            }

            private void PieceTable_PrevCategory(On.PieceTable.orig_PrevCategory orig, PieceTable self)
            {
                var oldCat = self.m_selectedCategory;
                orig(self);

                if (self.m_useCategories)
                {
                    if (self.m_selectedCategory != oldCat)
                    {
                        self.m_selectedCategory = oldCat;
                        do
                        {
                            self.m_selectedCategory--;

                            if (self.m_selectedCategory < 0)
                            {
                                self.m_selectedCategory = Instance.PieceCategoryMax - 1;
                            }
                        }
                        while (!Values.Contains(self.m_selectedCategory));
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
                lastCategory = selectedCategory;

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
                    float offsetX = selectedCategory == Values.Max() ? maxX - PieceCategorySize : PieceCategoryTabSize;
                    foreach (GameObject go in Hud.instance.m_pieceCategoryTabs)
                    {
                        (go.transform as RectTransform).anchoredPosition -= new Vector2(offsetX, 0);
                    }
                }
            }
        }
    }
}
