using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Utility class to query metadata about loaded mods and their added content
    /// </summary>
    public static class ModRegistry
    {
        /// <summary>
        ///     Get all loaded mod's metadata
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ModInfo> GetMods(bool includingJotunn = false)
        {
            foreach (
                BepInPlugin mod in BepInExUtils.GetDependentPlugins(includingJotunn)
                .Values
                .Select(mod => mod.Info.Metadata))
            {
                yield return new ModInfo()
                {
                    GUID = mod.GUID,
                    Name = mod.Name,
                    Version = mod.Version
                };
            }
        }

        /// <summary>
        ///     Get all added <see cref="CustomPrefab"/>s
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CustomPrefab> GetPrefabs()
        {
            return PrefabManager.Instance.Prefabs.Values;
        }

        /// <summary>
        ///     Get all added <see cref="CustomPrefab"/>s of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns></returns>
        public static IEnumerable<CustomPrefab> GetPrefabs(string modGuid)
        {
            return PrefabManager.Instance.Prefabs.Values.Where(x => x.SourceMod.GUID.Equals(modGuid));
        }

        /// <summary>
        ///     Get all added <see cref="CustomItem"/>s
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CustomItem> GetItems()
        {
            return ItemManager.Instance.Items.AsReadOnly();
        }

        /// <summary>
        ///     Get all added <see cref="CustomItem"/>s of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns></returns>
        public static IEnumerable<CustomItem> GetItems(string modGuid)
        {
            return ItemManager.Instance.Items.Where(x => x.SourceMod.GUID.Equals(modGuid));
        }

        /// <summary>
        ///     Get all added <see cref="CustomRecipe"/>s
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CustomRecipe> GetRecipes()
        {
            return ItemManager.Instance.Recipes.AsReadOnly();
        }

        /// <summary>
        ///     Get all added <see cref="CustomRecipe"/>s of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns></returns>
        public static IEnumerable<CustomRecipe> GetRecipes(string modGuid)
        {
            return ItemManager.Instance.Recipes.Where(x => x.SourceMod.GUID.Equals(modGuid));
        }

        /// <summary>
        ///     Get all added <see cref="CustomItemConversion"/>s of a mod by GUID
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CustomItemConversion> GetItemConversions()
        {
            return ItemManager.Instance.ItemConversions.AsReadOnly();
        }

        /// <summary>
        ///     Get all added <see cref="CustomItemConversion"/>s of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns></returns>
        public static IEnumerable<CustomItemConversion> GetItemConversions(string modGuid)
        {
            return ItemManager.Instance.ItemConversions.Where(x => x.SourceMod.GUID.Equals(modGuid));
        }

        /// <summary>
        ///     Get all added <see cref="CustomStatusEffect"/>s
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CustomStatusEffect> GetStatusEffects()
        {
            return ItemManager.Instance.StatusEffects.AsReadOnly();
        }

        /// <summary>
        ///     Get all added <see cref="CustomStatusEffect"/>s of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns></returns>
        public static IEnumerable<CustomStatusEffect> GetStatusEffects(string modGuid)
        {
            return ItemManager.Instance.StatusEffects.Where(x => x.SourceMod.GUID.Equals(modGuid));
        }

        /// <summary>
        ///     Get all added <see cref="CustomPieceTable"/>s
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CustomPieceTable> GetPieceTables()
        {
            return PieceManager.Instance.PieceTables.AsReadOnly();
        }

        /// <summary>
        ///     Get all added <see cref="CustomPieceTable"/>s of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns></returns>
        public static IEnumerable<CustomPieceTable> GetPieceTables(string modGuid)
        {
            return PieceManager.Instance.PieceTables.Where(x => x.SourceMod.GUID.Equals(modGuid));
        }

        /// <summary>
        ///     Get all added <see cref="CustomPiece"/>s
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CustomPiece> GetPieces()
        {
            return PieceManager.Instance.Pieces.AsReadOnly();
        }
        /// <summary>
        ///     Get all added <see cref="CustomPiece"/>s of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns></returns>
        public static IEnumerable<CustomPiece> GetPieces(string modGuid)
        {
            return PieceManager.Instance.Pieces.Where(x => x.SourceMod.GUID.Equals(modGuid));
        }

        /// <summary>
        ///     Get all added <see cref="ConsoleCommand"/>s
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ConsoleCommand> GetCommands()
        {
            return CommandManager.Instance.CustomCommands;
        }
        /// <summary>
        ///     Get all added <see cref="ConsoleCommand"/>s of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns></returns>
        public static IEnumerable<ConsoleCommand> GetCommands(string modGuid)
        {
            return CommandManager.Instance.CustomCommands.Where(x => x.SourceMod.GUID.Equals(modGuid));
        }

        /// <summary>
        ///     Get all added <see cref="CustomLocalization"/>s
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CustomLocalization> GetTranslations()
        {
            return LocalizationManager.Instance.GetRaw();
        }
        /// <summary>
        ///     Get all added <see cref="CustomLocalization"/>s of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns></returns>
        public static IEnumerable<CustomLocalization> GetTranslations(string modGuid)
        {
            return LocalizationManager.Instance.GetRaw().Where(x => x.SourceMod.GUID.Equals(modGuid));
        }

        /// <summary>
        ///     Model class holding metadata of Jötunn mods.
        /// </summary>
        public class ModInfo
        {
            /// <summary>
            ///     The mod GUID
            /// </summary>
            public string GUID { get; internal set; }

            /// <summary>
            ///     Human readable name
            /// </summary>
            public string Name { get; internal set; }

            /// <summary>
            ///     Current version
            /// </summary>
            public System.Version Version { get; internal set; }

            /// <summary>
            ///     Custom prefabs added by that mod
            /// </summary>
            public IEnumerable<CustomPrefab> Prefabs
            {
                get
                {
                    return GetPrefabs(GUID);
                }
            }

            /// <summary>
            ///     Custom items added by that mod
            /// </summary>
            public IEnumerable<CustomItem> Items
            {
                get
                {
                    return GetItems(GUID);
                }
            }

            /// <summary>
            ///     Custom recipes added by that mod
            /// </summary>
            public IEnumerable<CustomRecipe> Recipes
            {
                get
                {
                    return GetRecipes(GUID);
                }
            }

            /// <summary>
            ///     Custom item conversions added by that mod
            /// </summary>
            public IEnumerable<CustomItemConversion> ItemConversions => GetItemConversions(GUID);

            /// <summary>
            ///     Custom status effects added by that mod
            /// </summary>
            public IEnumerable<CustomStatusEffect> StatusEffects
            {
                get
                {
                    return GetStatusEffects(GUID);
                }
            }

            /// <summary>
            ///     Custom piece tables added by that mod
            /// </summary>
            public IEnumerable<CustomPieceTable> PieceTables
            {
                get
                {
                    return GetPieceTables(GUID);
                }
            }

            /// <summary>
            ///     Custom pieces added by that mod
            /// </summary>
            public IEnumerable<CustomPiece> Pieces
            {
                get
                {
                    return GetPieces(GUID);
                }
            }

            /// <summary>
            ///     Custom commands added by that mod
            /// </summary>
            public IEnumerable<ConsoleCommand> Commands
            {
                get
                {
                    return GetCommands(GUID);
                }
            }

            /// <summary>
            ///     Custom commands added by that mod
            /// </summary>
            public IEnumerable<CustomLocalization> Translations
            {
                get
                {
                    return GetTranslations(GUID);
                }
            }
        }
    }
}
