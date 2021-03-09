using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimLokiLoader.Utils;
using ValheimLokiLoader.Entities;

namespace ValheimLokiLoader.Managers
{
    public class ObjectManager : Manager
    {
        public static ObjectManager Instance { get; private set; }

        public event EventHandler ObjectRegister;
        internal List<GameObject> Items = new List<GameObject>();
        internal List<Recipe> Recipes = new List<Recipe>();
        
        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        internal override void Load()
        {
            Debug.Log("---- Registering custom objects ----");

            // Clear existing items and recipes
            Items.Clear();
            Recipes.Clear();

            // Register new items and recipes
            ObjectRegister?.Invoke(null, EventArgs.Empty);

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

        public void RegisterItem(string prefabName)
        {
            Items.Add(PrefabManager.Instance.GetPrefab(prefabName));
        }

        public void RegisterItem(GameObject item)
        {
            Items.Add(item);
        }

        public void RegisterRecipe(RecipeConfig recipe)
        {
            Recipes.Add(recipe.GetRecipe());
        }

        public void RegisterRecipe(Recipe recipe)
        {
            Recipes.Add(recipe);
        }

        public GameObject GetItemPrefab(string name)
        {
            return ObjectDB.instance.GetItemPrefab(name);
        }

        public ItemDrop GetItemDrop(string name)
        {
            return GetItemPrefab(name)?.GetComponent<ItemDrop>();
        }
    }
}
