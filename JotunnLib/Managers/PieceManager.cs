﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers.MockSystem;
using Jotunn.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        static PieceManager()
        {
            ((IManager)Instance).Init();
        }

        /// <summary>
        ///     Event that gets fired after all pieces were added to their respective PieceTables.
        ///     Your code will execute every time a new ObjectDB is created (on every game start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnPiecesRegistered;

        internal readonly Dictionary<string, CustomPiece> Pieces = new Dictionary<string, CustomPiece>();
        internal readonly List<CustomPieceTable> PieceTables = new List<CustomPieceTable>();

        private readonly Dictionary<string, PieceTable> PieceTableMap = new Dictionary<string, PieceTable>();
        private readonly Dictionary<string, string> PieceTableNameMap = new Dictionary<string, string>();

        private readonly Dictionary<string, Piece.PieceCategory> PieceCategories = new Dictionary<string, Piece.PieceCategory>();
        private readonly Dictionary<string, Piece.PieceCategory> OtherPieceCategories = new Dictionary<string, Piece.PieceCategory>();
        private static bool categoryRefreshNeeded = false;
        private static string hiddenCategoryMagic = "(HiddenCategory)";

        /// <summary>
        ///     Settings of the hammer UI tab selection.
        /// </summary>
        [Obsolete("No longer used")]
        public static class PieceCategorySettings
        {
            /// <summary>
            ///     Piece table tab header width.
            /// </summary>
            [Obsolete("This setting is no longer used")]
            public static float HeaderWidth { get; set; } = 700f;

            /// <summary>
            ///     Minimum size of a piece table tab. The tab can grow bigger than this the name doesn't fit.
            /// </summary>
            [Obsolete("This setting is no longer used")]
            public static float MinTabSize { get; set; } = 140f;

            /// <summary>
            ///     Tab size per name character. This determines how fast the tab size grows.
            /// </summary>
            [Obsolete("This setting is no longer used")]
            public static float TabSizePerCharacter { get; set; } = 11f;

            /// <summary>
            ///     Minimum left/right space that is visible for not selected adjacent tabs.
            /// </summary>
            [Obsolete("This setting is no longer used")]
            public static float TabMargin { get; set; } = 50f;
        }

        /// <summary>
        ///     Creates the piece table container and registers all hooks.
        /// </summary>
        void IManager.Init()
        {
            Main.LogInit("PieceManager");
            Main.Harmony.PatchAll(typeof(Patches));
            PrefabManager.Instance.Activate();

            if (ObjectDB.instance)
            {
                LoadPieceTables();
            }
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.NextCategory)), HarmonyPostfix]
            public static void PieceTable_NextCategory_Postfix(PieceTable __instance) => Instance.PieceTable_NextCategory(__instance);

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.PrevCategory)), HarmonyPostfix]
            public static void PieceTable_PrevCategory_Postfix(PieceTable __instance) => Instance.PieceTable_PrevCategory(__instance);

            [HarmonyPatch(typeof(Player), nameof(Player.SetPlaceMode)), HarmonyPostfix]
            public static void Player_SetPlaceMode() => Instance.RefreshCategories();

            [HarmonyPatch(typeof(Hud), nameof(Hud.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Low)]
            private static void Hud_Awake() => Instance.RefreshCategories();

            [HarmonyPatch(typeof(Hud), nameof(Hud.LateUpdate)), HarmonyPostfix]
            private static void Hud_LateUpdate()
            {
                if (categoryRefreshNeeded)
                {
                    categoryRefreshNeeded = false;
                    Instance.RefreshCategories();
                }
            }

            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Low)]
            private static void RegisterCustomData(ObjectDB __instance) => Instance.RegisterCustomData(__instance);

            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void InvokeOnPiecesRegistered(ObjectDB __instance) => Instance.InvokeOnPiecesRegistered(__instance);

            [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned)), HarmonyPostfix]
            private static void ReloadKnownRecipes(Player __instance) => Instance.ReloadKnownRecipes(__instance);

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable)), HarmonyPrefix]
            public static void PieceTable_UpdateAvailable_Prefix(PieceTable __instance) => ExpandAvailablePieces(__instance);

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable)), HarmonyPostfix]
            public static void PieceTable_UpdateAvailable_Postfix(PieceTable __instance)
            {
                AdjustPieceTableArray(__instance);
                ReorderAllCategoryPieces(__instance);
            }

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable)), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> UpdateAvailable_Transpiler(IEnumerable<CodeInstruction> instructions) => TranspileMaxCategory(instructions, 0);

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.NextCategory)), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> NextCategory_Transpiler(IEnumerable<CodeInstruction> instructions) => TranspileMaxCategory(instructions, 0);

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.PrevCategory)), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> PrevCategory_Transpiler(IEnumerable<CodeInstruction> instructions) => TranspileMaxCategory(instructions, -1);

            [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.SetCategory)), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> SetCategory_Transpiler(IEnumerable<CodeInstruction> instructions) => TranspileMaxCategory(instructions, -1);

            [HarmonyPatch(typeof(Enum), nameof(Enum.GetValues)), HarmonyPostfix]
            private static void EnumGetValuesPatch(Type enumType, ref Array __result) => Instance.EnumGetValuesPatch(enumType, ref __result);

            [HarmonyPatch(typeof(Enum), nameof(Enum.GetNames)), HarmonyPostfix]
            private static void EnumGetNamesPatch(Type enumType, ref string[] __result) => Instance.EnumGetNamesPatch(enumType, ref __result);
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
                Logger.LogWarning(customPieceTable.SourceMod, $"Custom piece {customPieceTable} is not valid");
                return false;
            }

            if (PieceTables.Contains(customPieceTable))
            {
                Logger.LogWarning(customPieceTable.SourceMod, $"Piece table {customPieceTable} already added");
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
                AddPieceCategory(category);
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
        [Obsolete("Use AddPieceCategory(string name) instead")]
        public Piece.PieceCategory AddPieceCategory(string table, string name)
        {
            return AddPieceCategory(name);
        }

        /// <summary>
        ///     Add a new <see cref="Piece.PieceCategory"/> by name. A new category
        ///     gets assigned a random integer for internal use. If you pass a vanilla category
        ///     the actual integer value of the enum is returned.
        /// </summary>
        /// <param name="name">Name of the category.</param>
        /// <returns>int value of the vanilla or custom category</returns>
        public Piece.PieceCategory AddPieceCategory(string name)
        {
            Piece.PieceCategory categoryID = GetOrCreatePieceCategory(name, out bool isNew);

            // When new categories are inserted in-game, directly create and update categories
            if (isNew)
            {
                CreateCategoryTabs();
            }

            // refresh the categories later. The new category is not yet assigned to the piece
            categoryRefreshNeeded = true;

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
            if (Enum.TryParse(name, true, out Piece.PieceCategory category))
            {
                return category;
            }

            if (PieceCategories.TryGetValue(name, out category))
            {
                return category;
            }

            if (OtherPieceCategories.TryGetValue(name, out category))
            {
                return category;
            }

            return null;
        }

        /// <summary>
        ///     Remove a <see cref="Piece.PieceCategory"/> from a table by name.
        ///     This does noting if a piece is still assigned to the category, remove it before calling this.
        /// </summary>
        /// <param name="table">Prefab or item name of the PieceTable.</param>
        /// <param name="name">Name of the category.</param>
        [Obsolete("Use RemovePieceCategory(string name) instead")]
        public void RemovePieceCategory(string table, string name)
        {
            RemovePieceCategory(name);
        }

        /// <summary>
        ///     Remove a <see cref="Piece.PieceCategory"/> from a table by name.
        ///     This does noting if a piece is still assigned to the category, remove it before calling this.
        /// </summary>
        /// <param name="table">Prefab or item name of the PieceTable.</param>
        /// <param name="name">Name of the category.</param>
        public void RemovePieceCategory(string name)
        {
            categoryRefreshNeeded = true;
        }

        /// <summary>
        ///     Get a list of all custom Jötunn piece category names
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use GetPieceCategoriesMap to get a complete map of all categories, not only Jötunn ones")]
        public List<string> GetPieceCategories()
        {
            return PieceCategories.Keys.ToList();
        }

        /// <summary>
        ///     Get a complete map of all piece categories.
        ///     This includes vanilla, Jötunn and other modded categories that use the same system
        /// </summary>
        /// <returns></returns>
        public Dictionary<Piece.PieceCategory, string> GetPieceCategoriesMap()
        {
            var values = Enum.GetValues(typeof(Piece.PieceCategory));
            var names = Enum.GetNames(typeof(Piece.PieceCategory));

            var map = new Dictionary<Piece.PieceCategory, string>();

            for (int i = 0; i < values.Length; i++)
            {
                map[(Piece.PieceCategory)values.GetValue(i)] = names[i];
            }

            return map;
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
                Logger.LogWarning(customPiece.SourceMod, $"Custom piece {customPiece} is not valid");
                return false;
            }

            if (Pieces.ContainsKey(customPiece.PiecePrefab.name))
            {
                Logger.LogWarning(customPiece.SourceMod, $"Custom piece {customPiece} already added");
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
                Logger.LogWarning(piece.SourceMod, $"Could not remove piece {piece}: Not found");
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
                    PieceTableMap[table.name] = table;
                    PieceTableNameMap[item.name] = table.name;
                }
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
                    RegisterCustomPiece(customPiece);
                }
                catch (MockResolveException ex)
                {
                    Logger.LogWarning(customPiece?.SourceMod, $"Skipping piece {customPiece}: {ex.Message}");
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

        private void RegisterCustomPiece(CustomPiece customPiece) {
            // Fix references if needed
            if (customPiece.FixReference || customPiece.FixConfig) {
                customPiece.PiecePrefab.FixReferences(customPiece.FixReference);
                customPiece.FixReference = false;
                customPiece.FixConfig = false;
            }

            // Assign vfx_ExtensionConnection for StationExtensions
            var extension = customPiece.PiecePrefab.GetComponent<StationExtension>();
            if (extension != null && !extension.m_connectionPrefab) {
                extension.m_connectionPrefab = PrefabManager.Cache.GetPrefab<GameObject>("vfx_ExtensionConnection");
            }

            // Assign the piece to the actual PieceTable if not already in there
            RegisterPieceInPieceTable(customPiece.PiecePrefab, customPiece.PieceTable, null, customPiece.SourceMod);
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
        public void RegisterPieceInPieceTable(GameObject prefab, string pieceTable, string category = null) =>
            RegisterPieceInPieceTable(prefab, pieceTable, category, BepInExUtils.GetSourceModMetadata());

        /// <summary>
        ///     Internal method for adding a prefab to a piece table.
        /// </summary>
        private void RegisterPieceInPieceTable(GameObject prefab, string pieceTable, string category, BepInPlugin sourceMod)
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

            var name = prefab.name;
            var hash = name.GetStableHashCode();

            if (!PrefabManager.Instance.Prefabs.ContainsKey(name))
            {
                PrefabManager.Instance.AddPrefab(prefab, sourceMod);
            }

            if (ZNetScene.instance != null && !ZNetScene.instance.m_namedPrefabs.ContainsKey(hash))
            {
                PrefabManager.Instance.RegisterToZNetScene(prefab);
            }

            if (!string.IsNullOrEmpty(category))
            {
                piece.m_category = AddPieceCategory(category);
            }

            table.m_pieces.Add(prefab);
            Logger.LogDebug($"Added piece {prefab.name} | Token: {piece.TokenName()}");
        }

        private void RegisterCustomData(ObjectDB self)
        {
            if (SceneManager.GetActiveScene().name == "main")
            {
                LoadPieceTables();
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

        #region Internal Custom Categories Handling

        private static int MaxCategory() => Enum.GetValues(typeof(Piece.PieceCategory)).Length - 1;

        private static IEnumerable<CodeInstruction> TranspileMaxCategory(IEnumerable<CodeInstruction> instructions, int maxOffset)
        {
            int number = PieceUtils.VanillaMaxPieceCategory + maxOffset;

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.LoadsConstant(number))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PieceManager), nameof(MaxCategory)));

                    if (maxOffset != 0)
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_I4, maxOffset);
                        yield return new CodeInstruction(OpCodes.Add);
                    }
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        private void EnumGetValuesPatch(Type enumType, ref Array __result)
        {
            if (enumType != typeof(Piece.PieceCategory))
            {
                return;
            }

            if (PieceCategories.Count == 0)
            {
                return;
            }

            var categories = new Piece.PieceCategory[__result.Length + PieceCategories.Count];

            __result.CopyTo(categories, 0);
            PieceCategories.Values.CopyTo(categories, __result.Length);

            __result = categories;
        }

        private void EnumGetNamesPatch(Type enumType, ref string[] __result)
        {
            if (enumType != typeof(Piece.PieceCategory))
            {
                return;
            }

            if (PieceCategories.Count == 0)
            {
                return;
            }

            __result = __result.AddRangeToArray(PieceCategories.Keys.ToArray());
        }

        private static void ExpandAvailablePieces(PieceTable __instance)
        {
            if (__instance.m_availablePieces.Count > 0)
            {
                int missing = MaxCategory() - __instance.m_availablePieces.Count;
                for (int i = 0; i < missing; i++)
                {
                    __instance.m_availablePieces.Add(new List<Piece>());
                }
            }
        }

        private static void AdjustPieceTableArray(PieceTable pieceTable)
        {
            Array.Resize(ref pieceTable.m_selectedPiece, pieceTable.m_availablePieces.Count);
            Array.Resize(ref pieceTable.m_lastSelectedPiece, pieceTable.m_availablePieces.Count);
        }

        private static void ReorderAllCategoryPieces(PieceTable pieceTable)
        {
            List<Piece> pieces = pieceTable.m_pieces.Select(i => i.GetComponent<Piece>()).ToList();
            List<Piece> piecesWithAllCategory = pieces.FindAll(i => i && i.m_category == Piece.PieceCategory.All);

            foreach (List<Piece> availablePieces in pieceTable.m_availablePieces)
            {
                int listPosition = 0;

                foreach (var piece in piecesWithAllCategory)
                {
                    // m_availablePieces are already populated. Add pieces at the beginning of the list, to replicate vanilla behaviour
                    availablePieces.Remove(piece);
                    availablePieces.Insert(Mathf.Min(listPosition, pieceTable.m_availablePieces.Count), piece);
                    listPosition++;
                }
            }
        }

        private static void SetTabActive(GameObject tab, string tabName, bool active)
        {
            tab.SetActive(active);

            if (active)
            {
                tab.name = tabName.Replace(hiddenCategoryMagic, "");
            }
            else
            {
                tab.name = $"{tabName}{hiddenCategoryMagic}";
            }
        }

        private static HashSet<Piece.PieceCategory> CategoriesInPieceTable(PieceTable pieceTable)
        {
            HashSet<Piece.PieceCategory> categories = new HashSet<Piece.PieceCategory>();

            foreach (GameObject piece in pieceTable.m_pieces)
            {
                categories.Add(piece.GetComponent<Piece>().m_category);
            }

            return categories;
        }

        private void CreateCategoryTabs()
        {
            if (!Hud.instance)
            {
                return;
            }

            int maxCategory = MaxCategory();

            // Fill empty category names to prevent index issues, the correct names are set by the respective mods later
            for (int i = Hud.instance.m_buildCategoryNames.Count; i < maxCategory; ++i)
            {
                Hud.instance.m_buildCategoryNames.Add("");
            }

            // Append tabs and their names to the GUI for every custom category not already added
            for (int i = Hud.instance.m_pieceCategoryTabs.Length; i < maxCategory; i++)
            {
                GameObject tab = CreateCategoryTab();
                Hud.instance.m_pieceCategoryTabs = Hud.instance.m_pieceCategoryTabs.AddItem(tab).ToArray();
            }

            if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces)
            {
                Player.m_localPlayer.UpdateAvailablePiecesList();
            }
        }

        private string GetCategoryToken(string name)
        {
            char[] forbiddenCharsArray = LocalizationManager.ForbiddenChars.ToCharArray();
            string tokenCategory = string.Join("_", name.ToLower().Split(forbiddenCharsArray));
            return $"jotunn_cat_{tokenCategory}";
        }

        private Piece.PieceCategory GetOrCreatePieceCategory(string name, out bool isNew)
        {
            Piece.PieceCategory? existingCategory = GetPieceCategory(name);

            if (existingCategory != null)
            {
                isNew = false;
                return existingCategory.Value;
            }

            Piece.PieceCategory category;

            var categories = GetPieceCategoriesMap();

            foreach (var categoryPair in categories)
            {
                if (categoryPair.Value == name)
                {
                    category = categoryPair.Key;
                    OtherPieceCategories[name] = category;
                    isNew = false;
                    return category;
                }
            }

            // create a new category
            category = (Piece.PieceCategory)categories.Count - 1;
            PieceCategories[name] = category;
            var token = GetCategoryToken(name);
            LocalizationManager.Instance.JotunnLocalization.AddTranslation(token, name);

            isNew = true;
            return category;
        }

        private GameObject CreateCategoryTab()
        {
            Transform categoryRoot = Hud.instance.m_pieceCategoryRoot.transform;

            GameObject newTab = Object.Instantiate(Hud.instance.m_pieceCategoryTabs[0], categoryRoot);
            newTab.SetActive(false);

            UIInputHandler handler = newTab.GetOrAddComponent<UIInputHandler>();
            handler.m_onLeftDown += Hud.instance.OnLeftClickCategory;

            foreach (var text in newTab.GetComponentsInChildren<TMP_Text>(true))
            {
                text.rectTransform.offsetMin = new Vector2(3, 1);
                text.rectTransform.offsetMax = new Vector2(-3, -1);
                text.enableAutoSizing = true;
                text.fontSizeMin = 10;
                text.fontSizeMax = 20;
                text.lineSpacing = 0.8f;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Truncate;
            }

            return newTab;
        }

        /// <summary>
        ///     Updates the piece categories, should be called after setting the m_category field of a piece.
        /// </summary>
        private void RefreshCategories()
        {
            // make sure all category tabs are already created correctly
            CreateCategoryTabs();

            if (!Player.m_localPlayer)
            {
                return;
            }

            PieceTable pieceTable = Player.m_localPlayer.m_buildPieces;

            if (!pieceTable)
            {
                return;
            }

            RectTransform firstTab = (RectTransform)Hud.instance.m_pieceCategoryTabs[0].transform;
            RectTransform categoryRoot = (RectTransform)Hud.instance.m_pieceCategoryRoot.transform;
            RectTransform selectionWindow = (RectTransform)Hud.instance.m_pieceSelectionWindow.transform;

            const int verticalSpacing = 1;
            Vector2 tabSize = firstTab.rect.size;

            var visibleCategories = CategoriesInPieceTable(pieceTable);
            var categories = GetPieceCategoriesMap();

            bool onlyMiscActive = visibleCategories.Count == 1 && visibleCategories.First() == Piece.PieceCategory.Misc;
            pieceTable.m_useCategories = !onlyMiscActive;

            int maxHorizontalTabs = Mathf.Max((int)(categoryRoot.rect.width / tabSize.x), 1);
            int visibleTabs = VisibleTabCount(visibleCategories);

            float tabAnchorX = (-tabSize.x * maxHorizontalTabs) / 2f + tabSize.x / 2f;
            float tabAnchorY = (tabSize.y + verticalSpacing) * Mathf.Floor((float)(visibleTabs - 1) / maxHorizontalTabs) + 5f;
            Vector2 tabAnchor = new Vector2(tabAnchorX, tabAnchorY);

            int tabIndex = 0;

            for (int i = 0; i < Hud.instance.m_pieceCategoryTabs.Length; ++i)
            {
                GameObject tab = Hud.instance.m_pieceCategoryTabs[i];
                string categoryName = categories[(Piece.PieceCategory)i];
                bool active = visibleCategories.Contains((Piece.PieceCategory)i);

                SetTabActive(tab, categoryName, active);

                if (active)
                {
                    RectTransform rect = tab.GetComponent<RectTransform>();
                    float x = tabSize.x * (tabIndex % maxHorizontalTabs);
                    float y = -(tabSize.y + verticalSpacing) * (Mathf.Floor((float)tabIndex / maxHorizontalTabs) + 0.5f);
                    rect.anchoredPosition = tabAnchor + new Vector2(x, y);
                    tabIndex++;
                }

                // only update names of own tabs, as translation tokens may be different between mods
                if (PieceCategories.ContainsKey(categoryName))
                {
                    Hud.instance.m_buildCategoryNames[i] = $"${GetCategoryToken(categoryName)}";
                }
            }

            RectTransform background = (RectTransform)selectionWindow.Find("Bkg2")?.transform;

            if (background)
            {
                float height = (tabSize.y + verticalSpacing) * Mathf.Max(0, Mathf.FloorToInt((float)(tabIndex - 1) / maxHorizontalTabs));
                background.offsetMax = new Vector2(background.offsetMax.x, height);
            }
            else
            {
                Logger.LogWarning("Category Refresh: Could not find background image, skipping resize");
            }

            if ((int)Player.m_localPlayer.m_buildPieces.m_selectedCategory >= Hud.instance.m_buildCategoryNames.Count)
            {
                Player.m_localPlayer.m_buildPieces.SetCategory((int)visibleCategories.First());
            }

            Hud.instance.GetComponentInParent<Localize>().RefreshLocalization();
        }

        private static int VisibleTabCount(HashSet<Piece.PieceCategory> visibleCategories) {
            int visibleTabs = 0;

            for (int i = 0; i < Hud.instance.m_pieceCategoryTabs.Length; ++i) {
                bool active = visibleCategories.Contains((Piece.PieceCategory)i);

                if (active) {
                    visibleTabs++;
                }
            }

            return visibleTabs;
        }

        private void PieceTable_NextCategory(PieceTable self)
        {
            if (self.m_pieces.Count == 0 || !self.m_useCategories)
            {
                return;
            }

            var selectedTab = Hud.instance.m_pieceCategoryTabs[(int)self.m_selectedCategory];

            if (selectedTab.name.Contains(hiddenCategoryMagic))
            {
                self.NextCategory();
            }
        }

        private void PieceTable_PrevCategory(PieceTable self)
        {
            if (self.m_pieces.Count == 0 || !self.m_useCategories)
            {
                return;
            }

            var selectedTab = Hud.instance.m_pieceCategoryTabs[(int)self.m_selectedCategory];

            if (selectedTab.name.Contains(hiddenCategoryMagic))
            {
                self.PrevCategory();
            }
        }

        #endregion
    }
}
