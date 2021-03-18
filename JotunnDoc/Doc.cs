using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BepInEx;

namespace JotunnDoc
{
    public class Doc
    {
        public string FilePath { get; protected set; }
        private StreamWriter writer;

        public Doc(string filePath)
        {
            FilePath = Path.Combine(Paths.PluginPath, filePath);

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
            string text = "";

            for (int i = 0; i < size; i++)
            {
                text += "#";
            }

            text += " " + headerText;
            writer.WriteLine(text);
            writer.Flush();
        }

        public void AddTableHeader(params string[] columns)
        {
            string text = "";

            foreach (string col in columns)
            {
                text += col + " |";
            }

            text += "\n";
            
            for (int i = 0; i < columns.Length; i++)
            {
                text += "---|";
            }

            writer.WriteLine(text);
            writer.Flush();
        }

        public void AddTableRow(params string[] vals)
        {
            string text = "";

            foreach (string val in vals)
            {
                text += val + "|";
            }

            writer.WriteLine(text);
            writer.Flush();
        }

        public void Save()
        {
            writer.Flush();
            writer.Close();
        }
    }
}
