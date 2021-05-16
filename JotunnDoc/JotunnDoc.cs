using System.Text.RegularExpressions;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using JotunnDoc.Docs;

namespace JotunnDoc
{
    [BepInPlugin("com.bepinex.plugins.jotunndoc", "JotunnDoc", "0.1.0")]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public class JotunnDoc : BaseUnityPlugin
    {
        private List<Doc> docs;

        private void Awake()
        {
            docs = new List<Doc>()
            {
                new InputDoc(),
                new ItemDoc(),
                new PrefabDoc(),
                new PieceTableDoc(),
                new PieceDoc(),
                new RecipeDoc(),
                new RPCDoc(),
                new SpriteDoc(),
                new StatusEffectDoc()
            };

            Debug.Log("Initialized JotunnDoc");
        }

        // Localize and strip text from text
        public static string Localize(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "NULL";
            }

            string text = Localization.instance.Localize(key).Replace("\n", "<br/>");
            text = Regex.Replace(text, @"[^\u0000-\u007F]+", string.Empty);

            return text;
        }
    }
}
