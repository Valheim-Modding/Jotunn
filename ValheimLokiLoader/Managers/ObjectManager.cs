using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimLokiLoader.Utils;
using ValheimLokiLoader.Entities;

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
                Debug.Log("Registered item: " + obj.name);
            }

            foreach (Recipe recipe in Recipes)
            {
                ObjectDB.instance.m_recipes.Add(recipe);
                Debug.Log("Registered item recipe: " + recipe.m_item.m_itemData.m_shared.m_name);
            }

            ReflectionUtils.InvokePrivate(ObjectDB.instance, "UpdateItemHashes");
        }

        public static void RegisterItem(string prefabName)
        {
            Items.Add(PrefabManager.GetPrefab(prefabName));
        }

        public static void RegisterItem(GameObject item)
        {
            Items.Add(item);
        }

        public static void RegisterRecipe(RecipeConfig recipe)
        {
            Recipes.Add(recipe.GetRecipe());
        }

        public static void RegisterRecipe(Recipe recipe)
        {
            Recipes.Add(recipe);
        }

        public static GameObject GetItemPrefab(string name)
        {
            return ObjectDB.instance.GetItemPrefab(name);
        }

        public static ItemDrop GetItemDrop(string name)
        {
            return GetItemPrefab(name)?.GetComponent<ItemDrop>();
        }
    }
}
