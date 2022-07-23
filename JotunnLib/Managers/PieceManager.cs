using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers.MockSystem;
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
        public static PieceManager Instance => _instance ??= new PieceManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private PieceManager() { }

        /// <summary>
        ///     Event that gets fired after all pieces were added to their respective PieceTables.
        ///     Your code will execute every time a new ObjectDB is created (on every game start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnPiecesRegistered;

        internal readonly Dictionary<string, CustomPiece> Pieces = new Dictionary<string, CustomPiece>();
        internal readonly List<CustomPieceTable> PieceTables = new List<CustomPieceTable>();

        internal readonly Dictionary<string, PieceTable> PieceTableMap = new Dictionary<string, PieceTable>();
        internal readonly Dictionary<string, string> PieceTableNameMap = new Dictionary<string, string>();
        internal readonly Dictionary<string, PieceTableCategories> PieceTableCategoriesMap = new Dictionary<string, PieceTableCategories>();

        internal readonly Dictionary<string, Piece.PieceCategory> PieceCategories = new Dictionary<string, Piece.PieceCategory>();
        private Piece.PieceCategory PieceCategoryMax = Piece.PieceCategory.Max;

        /// <summary>
        ///     Settings of the hammer UI tab selection.
        /// </summary>
        public static class PieceCategorySettings
        {
            /// <summary>
            ///     Piece table tab header width.
            /// </summary>
            public static float HeaderWidth { get; set; } = 700f;

            /// <summary>
            ///     Minimum size of a piece table tab. The tab can grow bigger than this the name doesn't fit.
            /// </summary>
            public static float MinTabSize { get; set; } = 140f;

            /// <summary>
            ///     Tab size per name character. This determines how fast the tab size grows.
            /// </summary>
            public static float TabSizePerCharacter { get; set; } = 11f;

            /// <summary>
            ///     Minimum left/right space that is visible for not selected adjacent tabs.
            /// </summary>
            public static float TabMargin { get; set; } = 50f;
        }

        /// <summary>
        ///     Creates the piece table container and registers all hooks.
        /// </summary>
        public void Init()
        {
            Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.SetCategory)), HarmonyPostfix]
            public static void PieceTable_SetCategory(PieceTable __instance, int index)
            {
                if (PieceTableCategories.currentActive != null)
                {
                    PieceTableCategories.currentActive.PieceTable_SetCategory(__instance, index);
                }
            }

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.NextCategory)), HarmonyPrefix]
            public static void PieceTable_NextCategory_Prefix(PieceTable __instance, ref Piece.PieceCategory __state)
            {
                __state = __instance.m_selectedCategory;
            }

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.NextCategory)), HarmonyPostfix]
            public static void PieceTable_NextCategory_Postfix(PieceTable __instance, ref Piece.PieceCategory __state)
            {
                if (PieceTableCategories.currentActive != null)
                {
                    PieceTableCategories.currentActive.PieceTable_NextCategory(__instance, __state);
                }
            }

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.PrevCategory)), HarmonyPrefix]
            public static void PieceTable_PrevCategory_Prefix(PieceTable __instance, ref Piece.PieceCategory __state)
            {
                __state = __instance.m_selectedCategory;
            }

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.PrevCategory)), HarmonyPostfix]
            public static void PieceTable_PrevCategory_Postfix(PieceTable __instance, ref Piece.PieceCategory __state)
            {
                if (PieceTableCategories.currentActive != null)
                {
                    PieceTableCategories.currentActive.PieceTable_PrevCategory(__instance, __state);
                }
            }

            [HarmonyPatch(typeof(Hud), nameof(Hud.OnLeftClickCategory)), HarmonyPostfix]
            public static void Hud_OnLeftClickCategory(Hud __instance)
            {
                if (PieceTableCategories.currentActive != null)
                {
                    PieceTableCategories.currentActive.Hud_OnLeftClickCategory(__instance);
                }
            }

            [HarmonyPatch(typeof(Hud), nameof(Hud.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Low)]
            private static void Hud_Awake()
            {
                Instance.CreateCategoryTabs();
            }

            [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBuild)), HarmonyPrefix]
            private static void Hud_UpdateBuild()
            {
                Instance.TogglePieceCategories();
            }

            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Low)]
            private static void RegisterCustomData(ObjectDB __instance) => Instance.RegisterCustomData(__instance);

            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void InvokeOnPiecesRegistered(ObjectDB __instance) => Instance.InvokeOnPiecesRegistered(__instance);

            [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned)), HarmonyPostfix]
            private static void ReloadKnownRecipes(Player __instance) => Instance.ReloadKnownRecipes(__instance);

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable)), HarmonyPostfix]
            public static void PieceTable_UpdateAvailable_Postfix(PieceTable __instance) => Instance.AddPieceWithAllCategoryToAvailablePieces(__instance);
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
            if (!PrefabManager.Instance.AddPrefab(customPieceTable.PieceTablePrefab, customPieceTable.SourceMod))
            {
                return false;
            }

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
        ///     Returns all <see cref="global::PieceTable"/> instances in the game.
        ///     The list is gathered on every ObjectDB.Awake() from all items in it,
        ///     so depending on the timing of the call, the list might not be complete.
        /// </summary>
        /// <returns>A list of <see cref="global::PieceTable"/> instances</returns>
        public List<PieceTable> GetPieceTables()
        {
            return PieceTableMap.Values.ToList();
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

            // When new categories are inserted in-game, directly create and update categories
            if (isNew)
            {
                if (SceneManager.GetActiveScene().name == "main")
                {
                    CreatePieceTableCategories();
                }
                if (Hud.instance != null)
                {
                    CreateCategoryTabs();
                }
                if (Player.m_localPlayer != null)
                {
                    UpdatePieceCategories();
                }
            }

            return categoryID;
        }
        
        /// <summary>
        ///     Get a <see cref="Piece.PieceCategory"/> by name. Translates
        ///     vanilla or custom Piece Categories to their current integer value.
        /// </summary>
        /// <param name="name">Name of the category.</param>
        /// <returns>int value of the vanilla or custom category</returns>
        public Piece.PieceCategory? GetPieceCategory(string name)
        {
            // Get or create global category ID
            if (Enum.IsDefined(typeof(Piece.PieceCategory), name))
            {
                return (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), name);
            }
            if (PieceCategories.ContainsKey(name))
            {
                return PieceCategories[name];
            }
            
            return null;
        }

        /// <summary>
        ///     Remove a <see cref="Piece.PieceCategory"/> from a table by name.
        ///     This does not remove the category from the game but "hides" it
        ///     in the given table.
        /// </summary>
        /// <param name="table">Prefab or item name of the PieceTable.</param>
        /// <param name="name">Name of the category.</param>
        public void RemovePieceCategory(string table, string name)
        {
            var actualTable = table;
            bool changed = false;

            if (PieceTableNameMap.ContainsKey(table))
            {
                actualTable = PieceTableNameMap[table];
            }

            if (PieceTableCategoriesMap.ContainsKey(actualTable))
            {
                if (PieceTableCategoriesMap[actualTable].ContainsKey(name))
                {
                    changed = true;
                }

                PieceTableCategoriesMap[actualTable].Remove(name);
            }

            if (changed)
            {
                if (SceneManager.GetActiveScene().name == "main")
                {
                    CreatePieceTableCategories();
                }
                if (Hud.instance != null)
                {
                    CreateCategoryTabs();
                }
                if (Player.m_localPlayer != null)
                {
                    UpdatePieceCategories();
                }
            }
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
            if (Pieces.ContainsKey(customPiece.PiecePrefab.name))
            {
                Logger.LogWarning($"Custom piece {customPiece} already added");
                return false;
            }

            // Add the prefab to the PrefabManager
            if (!PrefabManager.Instance.AddPrefab(customPiece.PiecePrefab, customPiece.SourceMod))
            {
                return false;
            }

            // Add to the right layer if necessary
            if (customPiece.PiecePrefab.layer == 0)
            {
                customPiece.PiecePrefab.layer = LayerMask.NameToLayer("piece");
            }

            // Add the custom piece to the PieceManager
            Pieces.Add(customPiece.PiecePrefab.name, customPiece);

            return true;
        }

        /// <summary>
        ///     Get a custom piece by its name.
        /// </summary>
        /// <param name="pieceName">Name of the piece to search.</param>
        /// <returns></returns>
        public CustomPiece GetPiece(string pieceName)
        {
            return Pieces.TryGetValue(pieceName, out CustomPiece piece) ? piece : null;
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
            string name = piece.PiecePrefab.name;

            if (!Pieces.ContainsKey(name))
            {
                Logger.LogWarning($"Could not remove piece {piece}: Not found");
                return;
            }

            Pieces.Remove(name);

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
        }

        private void CreateCategoryTabs()
        {
            // Only touch categories when new ones were added
            if (!PieceCategories.Any())
            {
                return;
            }

            // Get the GUI elements
            GameObject root = Hud.instance.m_pieceCategoryRoot;
            if (root.GetComponent<RectMask2D>() == null)
            {
                root.AddComponent<RectMask2D>();
                root.SetWidth(PieceCategorySettings.HeaderWidth);

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

            // Replace the HUD arrays
            Hud.instance.m_buildCategoryNames = newNames.ToList();
            Hud.instance.m_pieceCategoryTabs = newTabs.ToArray();
        }

        /// <summary>
        ///     Reorder piece table tabs if the table if opened currently
        /// </summary>
        private void UpdatePieceCategories()
        {
            if (!Player.m_localPlayer)
            {
                return;
            }

            var table = Player.m_localPlayer.m_buildPieces;
            if (table != null && table.m_useCategories && PieceTableCategoriesMap.TryGetValue(table.name, out var categories))
            {
                categories.ReorderTableTabs();
            }
        }

        /// <summary>
        ///     Hook for piece table toggle
        /// </summary>
        private void TogglePieceCategories()
        {
            // Only touch categories when new ones were added
            if (!PieceCategories.Any())
            {
                return;
            }

            // Get currently selected tool and toggle PieceTableCategories
            try
            {
                var table = Player.m_localPlayer.m_buildPieces;
                if (table != null && table.m_useCategories && PieceTableCategoriesMap.TryGetValue(table.name, out var categories))
                {
                    categories.Toggle(Hud.instance.m_pieceSelectionWindow.activeSelf);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error toggling piece selection window: {ex}");
            }
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

            foreach (var pair in Pieces)
            {
                CustomPiece customPiece = pair.Value;

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
                catch (MockResolveException ex)
                {
                    Logger.LogWarning(customPiece?.SourceMod, $"Skipping piece {customPiece}: could not resolve mock {ex.MockType.Name} {ex.FailedMockName}");
                    toDelete.Add(customPiece);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(customPiece?.SourceMod, $"Error caught while adding piece {customPiece}: {ex}");
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

        private void RegisterCustomData(ObjectDB self)
        {
            if (SceneManager.GetActiveScene().name == "main")
            {
                LoadPieceTables();
                CreatePieceTableCategories();
                RegisterInPieceTables();
            }
        }

        private void InvokeOnPiecesRegistered(ObjectDB self)
        {
            if (SceneManager.GetActiveScene().name == "main")
            {
                OnPiecesRegistered?.SafeInvoke();
            }
        }

        /// <summary>
        ///     Hook on <see cref="Player.OnSpawned"/> to refresh recipes for the custom items.
        /// </summary>
        /// <param name="self"></param>
        private void ReloadKnownRecipes(Player self)
        {
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

        private void AddPieceWithAllCategoryToAvailablePieces(PieceTable pieceTable)
        {
            // pieces with All category are always available at the first category
            List<Piece> firstCategory = pieceTable.m_availablePieces.FirstOrDefault();

            if (firstCategory == null)
            {
                return;
            }

            List<Piece> piecesWithAllCategory = firstCategory.FindAll(i => i.m_category == Piece.PieceCategory.All);

            for (int i = 0; i < (int)PieceCategoryMax; i++)
            {
                int index = 0;

                foreach (var piece in piecesWithAllCategory)
                {
                    // m_availablePieces are already populated. Add pieces at the beginning of the list, to replicate vanilla behaviour

                    if (pieceTable.m_availablePieces[i].Contains(piece))
                    {
                        pieceTable.m_availablePieces[i].Remove(piece);
                    }

                    pieceTable.m_availablePieces[i].Insert(Mathf.Min(index, pieceTable.m_availablePieces.Count), piece);
                    index++;
                }
            }
        }

        internal class PieceTableCategories : Dictionary<string, Piece.PieceCategory>
        {
            public static PieceTableCategories currentActive;

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

                    currentActive = null;
                }
            }

            internal void ReorderTableTabs()
            {
                if (currentActive != null && currentActive == this)
                {
                    // Activate all tabs for this categories
                    foreach (GameObject tab in Hud.instance.m_pieceCategoryTabs)
                    {
                        tab.SetActive(Keys.Contains(tab.name));
                    }

                    // Reorder tabs
                    ReorderActiveTabs();
                }
            }

            private void ReorderActiveTabs()
            {
                float offset = 0f;
                foreach (GameObject go in Hud.instance.m_pieceCategoryTabs.Where(x => x.activeSelf))
                {
                    float width = CalculateTabWidth(go);
                    go.SetMiddleLeft();
                    go.SetHeight(30f);
                    go.SetWidth(width);
                    RectTransform tf = (go.transform as RectTransform);
                    tf.anchoredPosition = new Vector2(offset, 0f);
                    offset += width;
                }
            }

            public void PieceTable_SetCategory(PieceTable self, int index)
            {
                if (self.m_useCategories)
                {
                    if (ContainsValue((Piece.PieceCategory)index))
                    {
                        self.m_selectedCategory = (Piece.PieceCategory)index;
                    }
                }
            }

            public void PieceTable_NextCategory(PieceTable self, Piece.PieceCategory oldCat)
            {
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

            public void PieceTable_PrevCategory(PieceTable self, Piece.PieceCategory oldCat)
            {
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

            public void Hud_OnLeftClickCategory(Hud self)
            {
                PieceCategoryScroll(Player.m_localPlayer.m_buildPieces.m_selectedCategory);
            }

            private void PieceCategoryScroll(Piece.PieceCategory selectedCategory)
            {
                var tab = Hud.instance.m_pieceCategoryTabs[(int)selectedCategory];
                lastCategory = selectedCategory;

                float minX = tab.GetComponent<RectTransform>().anchoredPosition.x - PieceCategorySettings.TabMargin;
                if (minX < 0f)
                {
                    float offsetX = selectedCategory == Values.Min() ? minX * -1 - PieceCategorySettings.TabMargin : minX * -1;
                    foreach (GameObject go in Hud.instance.m_pieceCategoryTabs)
                    {
                        go.GetComponent<RectTransform>().anchoredPosition += new Vector2(offsetX, 0);
                    }
                }
                float maxX = tab.GetComponent<RectTransform>().anchoredPosition.x + CalculateTabWidth(tab) + PieceCategorySettings.TabMargin;
                if (maxX > PieceCategorySettings.HeaderWidth)
                {
                    float offsetX = selectedCategory == Values.Max() ? maxX - PieceCategorySettings.HeaderWidth - PieceCategorySettings.TabMargin : maxX - PieceCategorySettings.HeaderWidth;
                    foreach (GameObject go in Hud.instance.m_pieceCategoryTabs)
                    {
                        go.GetComponent<RectTransform>().anchoredPosition -= new Vector2(offsetX, 0);
                    }
                }
            }

            private static float CalculateTabWidth(GameObject tab)
            {
                return Mathf.Max(PieceCategorySettings.MinTabSize, PieceCategorySettings.TabSizePerCharacter * (tab.name.Length + 6f));
            }
        }
    }
}
