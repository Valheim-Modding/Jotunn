using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using JotunnLib.Managers;
using JotunnDoc.Docs;
using System.Text.RegularExpressions;

namespace JotunnDoc
{
    [BepInPlugin("com.bepinex.plugins.jotunndoc", "JotunnDoc", "0.1.0")]
    [BepInDependency(JotunnLib.JotunnLib.ModGuid)]
    public class JotunnDoc : BaseUnityPlugin
    {
        private List<Doc> docs;

        private void Awake()
        {
            // Harmony harmony = new Harmony("jotunndoc");
            // harmony.PatchAll();

            docs = new List<Doc>()
            {
                new PrefabDoc(),
                new ItemDoc(),
                new RecipeDoc(),
                new InputDoc(),
                new PieceTableDoc(),
                new PieceDoc(),
                new StatusEffectDoc(),
                new RPCDoc()
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
