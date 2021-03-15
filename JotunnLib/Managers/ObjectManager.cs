using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Utils;
using JotunnLib.Entities;

namespace JotunnLib.Managers
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

        internal override void Register()
        {
            Debug.Log("---- Registering custom objects ----");

            // Clear existing items and recipes
            Items.Clear();
            Recipes.Clear();

            // Register new items and recipes
            ObjectRegister?.Invoke(null, EventArgs.Empty);
        }

        internal override void Load()
        {
            Debug.Log("---- Loading custom objects ----");

            // Load items
            foreach (GameObject obj in Items)
            {
                ObjectDB.instance.m_items.Add(obj);
                Debug.Log("Loaded item: " + obj.name);
            }

            // Load recipes
            foreach (Recipe recipe in Recipes)
            {
                ObjectDB.instance.m_recipes.Add(recipe);
                Debug.Log("Loaded item recipe: " + recipe.name);
            }

            // Update hashes
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

        public void RegisterRecipe(RecipeConfig recipeConfig)
        {
            Recipe recipe = recipeConfig.GetRecipe();

            if (recipe == null)
            {
                Debug.LogError("Failed to add recipe for item: " + recipeConfig.Item);
                return;
            }

            Recipes.Add(recipe);
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
