using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn
{
    /// <summary>
    ///     Extends GameObject with a check if the GameObject is valid
    /// </summary>
    public static class GameObjectExtension
    {
        /// <summary>
        ///     Check for validity
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsValid(this GameObject self)
        {
            try
            {
                var name = self.name;
                if (name.IndexOf('(') > 0)
                {
                    name = name.Substring(self.name.IndexOf('(')).Trim();
                }
                if (string.IsNullOrEmpty(name))
                {
                    throw new Exception($"GameObject must have a name !");
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);

                return false;
            }
        }
    }

    /// <summary>
    ///     Extends ItemDrop with a TokenName
    /// </summary>
    public static class ItemDropExtension
    {
        /// <summary>
        ///     m_itemData.m_shared.m_name
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string TokenName(this ItemDrop self) => self.m_itemData.m_shared.m_name;
    }

    /// <summary>
    ///     Extends ItemData with a TokenName.
    /// </summary>
    public static class ItemDataExtension
    {
        /// <summary>
        ///     m_shared.m_name
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string TokenName(this ItemDrop.ItemData self) => self.m_shared.m_name;

        internal const int LinesToNextEntry = 9;

        internal static string GetUID(this ItemDrop.ItemData self)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(self.m_dropPrefab.name);
            stringBuilder.AppendLine(self.m_stack.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_durability.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_equiped.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_quality.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_variant.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_crafterID.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_crafterName);

            return HashUtils.ComputeSha256Hash(stringBuilder.ToString());
        }

        internal static bool RemoveItemFromFile(this ItemDrop.ItemData self, string inventoryId)
        {
            var inventoryFilePath = Path.Combine(Paths.CustomItemDataFolder, inventoryId);

            if (File.Exists(inventoryFilePath))
            {
                var fileLines = File.ReadAllLines(inventoryFilePath).ToList();

                var itemName = self.m_dropPrefab.name;

                var linesToRemove = new List<int>();

                for (var i = 0; i < fileLines.Count; i += LinesToNextEntry)
                {
                    if (fileLines[i] == itemName)
                    {
                        linesToRemove.Add(i);

                        break;
                    }
                }

                if (linesToRemove.Count > 0)
                {
                    foreach (var lineToRemove in linesToRemove)
                    {
                        fileLines.RemoveRange(lineToRemove, LinesToNextEntry);
                    }

                    File.WriteAllLines(inventoryFilePath, fileLines);

                    return true;
                }

                return false;
            }

            return true;
        }

        internal static void SaveToFile(this ItemDrop.ItemData self, string inventoryId)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(self.m_dropPrefab.name);
            stringBuilder.AppendLine(self.m_stack.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_durability.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_equiped.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_quality.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_variant.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_crafterID.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_crafterName);
            stringBuilder.AppendLine(self.GetUID());

            var inventoryFilePath = Path.Combine(Paths.CustomItemDataFolder, inventoryId);
            File.AppendAllText(inventoryFilePath, stringBuilder.ToString());
        }
    }

    /// <summary>
    ///     Extends StatusEffect with a TokenName and a check if the StatusEffect is valid so it can be added to the game.
    /// </summary>
    public static class RecipeExtension
    {
        /// <summary>
        ///     Check for validity
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsValid(this Recipe self)
        {
            try
            {
                var name = self.name;
                if (name.IndexOf('(') > 0)
                {
                    name = name.Substring(self.name.IndexOf('(')).Trim();
                }
                if (string.IsNullOrEmpty(name))
                {
                    throw new Exception($"Recipe must have a name !");
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);

                return false;
            }
        }
    }

    /// <summary>
    ///     Extends Piece with a TokenName
    /// </summary>
    public static class PieceExtension
    {
        /// <summary>
        ///     m_name
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string TokenName(this Piece self) => self.m_name;
    }

    /// <summary>
    ///     Extends StatusEffect with a TokenName and a check if the StatusEffect is valid so it can be added to the game.
    /// </summary>
    public static class StatusEffectExtension
    {
        /// <summary>
        ///     m_name
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string TokenName(this StatusEffect self) => self.m_name;

        /// <summary>
        ///     Check for validity
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsValid(this StatusEffect self)
        {
            try
            {
                var name = self.name;
                if (name.IndexOf('(') > 0)
                {
                    name = name.Substring(self.name.IndexOf('(')).Trim();
                }
                if (string.IsNullOrEmpty(name))
                {
                    throw new Exception($"StatusEffect must have a name !");
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);

                return false;
            }
        }
    }
}
