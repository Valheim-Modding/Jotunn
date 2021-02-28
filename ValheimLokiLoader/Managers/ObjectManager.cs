using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimLokiLoader.Managers
{
    public static class ObjectManager
    {
        public static event EventHandler ObjectLoad;
        internal static List<GameObject> Items = new List<GameObject>();
        internal static List<Recipe> Recipes = new List<Recipe>();

        internal static void LoadObjects()
        {
            Debug.Log("---- Registering custom objects ----");

            ObjectLoad?.Invoke(null, EventArgs.Empty);

            foreach (GameObject obj in Items)
            {
                ObjectDB.instance.m_items.Add(obj);
                Debug.Log("Added item: " + obj.name);
            }

            foreach (Recipe recipe in Recipes)
            {
                ObjectDB.instance.m_recipes.Add(recipe);
                Debug.Log("Added item recipe: " + recipe.m_item.m_itemData.m_shared.m_name);
            }

            Util.InvokePrivate(ObjectDB.instance, "UpdateItemHashes");
        }

        public static void AddItem(string prefabName)
        {
            Items.Add(PrefabManager.GetPrefab(prefabName));
        }

        public static void AddItem(GameObject item)
        {
            Items.Add(item);
        }

        public static void AddRecipe(Recipe recipe)
        {
            Recipes.Add(recipe);
        }
    }
}
