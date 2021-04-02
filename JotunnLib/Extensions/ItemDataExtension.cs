using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using JotunnLib.Utils;

namespace JotunnLib
{
    public static class ItemDataExtension
    {
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

            return Hashes.ComputeSha256Hash(stringBuilder.ToString());
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
}
