using System.Collections.Generic;
using System.Linq;
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
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ModInfo"/> for all loaded mods</returns>
        public static IEnumerable<ModInfo> GetMods(bool includingJotunn = false) =>
            BepInExUtils.GetDependentPlugins(includingJotunn)
                .Values
                .Select(mod => new ModInfo
                {
                    GUID = mod.Info.Metadata.GUID,
                    Name = mod.Info.Metadata.Name,
                    Version = mod.Info.Metadata.Version
                });

        /// <summary>
        ///     Get all added <see cref="CustomPrefab">CustomPrefabs</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomPrefab"/> from all loaded mods</returns>
        public static IEnumerable<CustomPrefab> GetPrefabs() =>
            PrefabManager.Instance.Prefabs.Values;

        /// <summary>
        ///     Get all added <see cref="CustomPrefab">CustomPrefabs</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomPrefab"/> from a specific mod</returns>
        public static IEnumerable<CustomPrefab> GetPrefabs(string modGuid) =>
            PrefabManager.Instance.Prefabs.Values.Where(x => x.SourceMod.GUID.Equals(modGuid));
        
        /// <summary>
        ///     Get all added <see cref="CustomItem">CustomItems</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomItem"/> from all loaded mods</returns>
        public static IEnumerable<CustomItem> GetItems() => 
            ItemManager.Instance.Items.AsReadOnly();
        
        /// <summary>
        ///     Get all added <see cref="CustomItem">CustomItems</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomItem"/> from a specific mod</returns>
        public static IEnumerable<CustomItem> GetItems(string modGuid) => 
            ItemManager.Instance.Items.Where(x => x.SourceMod.GUID.Equals(modGuid));
        
        /// <summary>
        ///     Get all added <see cref="CustomRecipe">CustomRecipes</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomRecipe"/> from all loaded mods</returns>
        public static IEnumerable<CustomRecipe> GetRecipes() => 
            ItemManager.Instance.Recipes.AsReadOnly();
        
        /// <summary>
        ///     Get all added <see cref="CustomRecipe">CustomRecipes</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomRecipe"/> from a specific mod</returns>
        public static IEnumerable<CustomRecipe> GetRecipes(string modGuid) => 
            ItemManager.Instance.Recipes.Where(x => x.SourceMod.GUID.Equals(modGuid));
        
        /// <summary>
        ///     Get all added <see cref="CustomItemConversion">CustomItemConversions</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomItemConversion"/> from all loaded mods</returns>
        public static IEnumerable<CustomItemConversion> GetItemConversions() => 
            ItemManager.Instance.ItemConversions.AsReadOnly();
        
        /// <summary>
        ///     Get all added <see cref="CustomItemConversion">CustomItemConversions</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomItemConversion"/> from a specific mod</returns>
        public static IEnumerable<CustomItemConversion> GetItemConversions(string modGuid) => 
            ItemManager.Instance.ItemConversions.Where(x => x.SourceMod.GUID.Equals(modGuid));
        
        /// <summary>
        ///     Get all added <see cref="CustomStatusEffect">CustomStatusEffects</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomStatusEffect"/> from all loaded mods</returns>
        public static IEnumerable<CustomStatusEffect> GetStatusEffects() => 
            ItemManager.Instance.StatusEffects.AsReadOnly();
        
        /// <summary>
        ///     Get all added <see cref="CustomStatusEffect">CustomStatusEffects</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomStatusEffect"/> from a specific mod</returns>
        public static IEnumerable<CustomStatusEffect> GetStatusEffects(string modGuid) => 
            ItemManager.Instance.StatusEffects.Where(x => x.SourceMod.GUID.Equals(modGuid));
        
        /// <summary>
        ///     Get all added <see cref="CustomPieceTable">CustomPieceTables</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomPieceTable"/> from all loaded mods</returns>
        public static IEnumerable<CustomPieceTable> GetPieceTables() => 
            PieceManager.Instance.PieceTables.AsReadOnly();
        
        /// <summary>
        ///     Get all added <see cref="CustomPieceTable">CustomPieceTables</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomPieceTable"/> from a specific mod</returns>
        public static IEnumerable<CustomPieceTable> GetPieceTables(string modGuid) => 
            PieceManager.Instance.PieceTables.Where(x => x.SourceMod.GUID.Equals(modGuid));
        
        /// <summary>
        ///     Get all added <see cref="CustomPiece">CustomPieces</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomPiece"/> from all loaded mods</returns>
        public static IEnumerable<CustomPiece> GetPieces() => 
            PieceManager.Instance.Pieces.AsReadOnly();
        
        /// <summary>
        ///     Get all added <see cref="CustomPiece">CustomPieces</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomPiece"/> from a specific mod</returns>
        public static IEnumerable<CustomPiece> GetPieces(string modGuid) => 
            PieceManager.Instance.Pieces.Where(x => x.SourceMod.GUID.Equals(modGuid));
        
        /// <summary>
        ///     Get all added <see cref="CustomLocation">CustomLocations</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomLocation"/> from all loaded mods</returns>
        public static IEnumerable<CustomLocation> GetLocations() => 
            ZoneManager.Instance.Locations.Values;
        
        /// <summary>
        ///     Get all added <see cref="CustomLocation">CustomLocations</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomLocation"/> from a specific mod</returns>
        public static IEnumerable<CustomLocation> GetLocations(string modGuid) => 
            ZoneManager.Instance.Locations.Values.Where(x => x.SourceMod.GUID.Equals(modGuid));
        
        /// <summary>
        ///     Get all added <see cref="CustomVegetation">CustomVegetations</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomVegetation"/> from all loaded mods</returns>
        public static IEnumerable<CustomVegetation> GetVegetation() => 
            ZoneManager.Instance.Vegetations.Values;
        
        /// <summary>
        ///     Get all added <see cref="CustomVegetation">CustomVegetations</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomVegetation"/> from a specific mod</returns>
        public static IEnumerable<CustomVegetation> GetVegetation(string modGuid) => 
            ZoneManager.Instance.Vegetations.Values.Where(x => x.SourceMod.GUID.Equals(modGuid));
        
        /// <summary>
        ///     Get all added <see cref="CustomCreature">CustomCreatures</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomCreature"/> from all loaded mods</returns>
        public static IEnumerable<CustomCreature> GetCreatures() => 
            CreatureManager.Instance.Creatures;
        
        /// <summary>
        ///     Get all added <see cref="CustomCreature">CustomCreatures</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomCreature"/> from a specific mod</returns>
        public static IEnumerable<CustomCreature> GetCreatures(string modGuid) => 
            CreatureManager.Instance.Creatures.Where(x => x.SourceMod.GUID.Equals(modGuid));

        /// <summary>
        ///     Get all added <see cref="ConsoleCommand">ConsoleCommands</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ConsoleCommand"/> from all loaded mods</returns>
        public static IEnumerable<ConsoleCommand> GetCommands() => 
            CommandManager.Instance.CustomCommands;
        
        /// <summary>
        ///     Get all added <see cref="ConsoleCommand">ConsoleCommands</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ConsoleCommand"/> from a specific mod</returns>
        public static IEnumerable<ConsoleCommand> GetCommands(string modGuid) => 
            CommandManager.Instance.CustomCommands.Where(x => x.SourceMod.GUID.Equals(modGuid));
        
        /// <summary>
        ///     Get all added <see cref="CustomLocalization">CustomLocalizations</see>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomLocalization"/> from all loaded mods</returns>
        public static IEnumerable<CustomLocalization> GetTranslations() => 
            LocalizationManager.Instance.Localizations.AsReadOnly();
        
        /// <summary>
        ///     Get all added <see cref="CustomLocalization">CustomLocalizations</see> of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CustomLocalization"/> from a specific mod</returns>
        public static IEnumerable<CustomLocalization> GetTranslations(string modGuid) =>
            LocalizationManager.Instance.Localizations.Where(x => x.SourceMod.GUID.Equals(modGuid));

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
            public IEnumerable<CustomPrefab> Prefabs => GetPrefabs(GUID);

            /// <summary>
            ///     Custom items added by that mod
            /// </summary>
            public IEnumerable<CustomItem> Items => GetItems(GUID);

            /// <summary>
            ///     Custom recipes added by that mod
            /// </summary>
            public IEnumerable<CustomRecipe> Recipes => GetRecipes(GUID);

            /// <summary>
            ///     Custom item conversions added by that mod
            /// </summary>
            public IEnumerable<CustomItemConversion> ItemConversions => GetItemConversions(GUID);

            /// <summary>
            ///     Custom status effects added by that mod
            /// </summary>
            public IEnumerable<CustomStatusEffect> StatusEffects => GetStatusEffects(GUID);

            /// <summary>
            ///     Custom piece tables added by that mod
            /// </summary>
            public IEnumerable<CustomPieceTable> PieceTables => GetPieceTables(GUID);

            /// <summary>
            ///     Custom pieces added by that mod
            /// </summary>
            public IEnumerable<CustomPiece> Pieces => GetPieces(GUID);
            
            /// <summary>
            ///     Custom locations added by that mod
            /// </summary>
            public IEnumerable<CustomLocation> Locations => GetLocations(GUID);
            
            /// <summary>
            ///     Custom Vegetation added by that mod
            /// </summary>
            public IEnumerable<CustomVegetation> Vegetation => GetVegetation(GUID);
            
            /// <summary>
            ///     Custom Creatures added by that mod
            /// </summary>
            public IEnumerable<CustomCreature> Creatures => GetCreatures(GUID);

            /// <summary>
            ///     Custom commands added by that mod
            /// </summary>
            public IEnumerable<ConsoleCommand> Commands => GetCommands(GUID);

            /// <summary>
            ///     Custom commands added by that mod
            /// </summary>
            public IEnumerable<CustomLocalization> Translations => GetTranslations(GUID);
        }
    }
}
