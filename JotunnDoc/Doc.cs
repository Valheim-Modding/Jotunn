using System.IO;
using System.Text;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;

namespace JotunnDoc
{
    public class Doc
    {
        public bool Generated { get; private set; }
        public string FilePath { get; protected set; }
        internal static ConfigEntry<string> DocumentationDirConfig { get => documentationDirConfig; set => documentationDirConfig = value; }

        private StreamWriter writer;
        private static ConfigEntry<string> documentationDirConfig;

        public Doc(string filePath)
        {
            FilePath = Path.Combine(documentationDirConfig.Value, "data", filePath);

            // Ensure we only create markdown files
            if (!FilePath.EndsWith(".md"))
            {
                FilePath += ".md";
            }

            // Create directory if it doesn't exist
            (new FileInfo(FilePath)).Directory.Create();

            writer = File.CreateText(FilePath);
        }

        public void AddText(string text)
        {
            writer.WriteLine(text);
            writer.Flush();
        }

        public void AddHeader(int size, string headerText)
        {
            StringBuilder text = new StringBuilder();

            for (int i = 0; i < size; i++)
            {
                text.Append("#");
            }

            text.Append(" " + headerText);
            writer.WriteLine(text);
            writer.Flush();
        }

        public void AddTableHeader(params string[] columns)
        {
            StringBuilder text = new StringBuilder("\n|");

            foreach (string col in columns)
            {
                text.Append(col + " |");
            }

            text.Append("\n|");

            for (int i = 0; i < columns.Length; i++)
            {
                text.Append("---|");
            }

            writer.WriteLine(text);
            writer.Flush();
        }

        public void AddTableRow(params string[] vals)
        {
            StringBuilder text = new StringBuilder("|");

            foreach (string val in vals)
            {
                if (!string.IsNullOrEmpty(val))
                {
                    text.Append(string.Join("<br/>", val.TrimEnd('\r','\n').Split('\r','\n')));
                }
                text.Append('|');
            }

            writer.WriteLine(text);
            writer.Flush();
        }

        public void Save()
        {
            writer.Flush();
            writer.Close();
            Generated = true;
        }

        internal static string RangeString(float m_min, float m_max)
        {
            if (m_min == m_max)
            {
                return m_min.ToString();
            }
            return $"{m_min} - {m_max}";
        }

        internal static bool RequestSprite(string path, GameObject prefab, Quaternion rotation)
        {
            if (File.Exists(path))
            {
                Jotunn.Logger.LogDebug($"Image at {path} already exists, not recreating");
                return true;
            }

            var sprite = RenderManager.Instance.Render(new RenderManager.RenderRequest(prefab)
            {
                Rotation = rotation,
                FieldOfView = 20f,
                DistanceMultiplier = 1.1f
            });

            if (!sprite)
            {
                return false;
            }

            var texture = sprite.texture;
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            return true;
        }

    }
}
