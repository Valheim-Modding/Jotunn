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
        //internal static readonly List<CustomItem> CustomItems = new List<CustomItem>();
        //internal static readonly List<CustomRecipe> CustomRecipes = new List<CustomRecipe>();
        //internal static readonly List<CustomStatusEffect> CustomStatusEffects = new List<CustomStatusEffect>();
        internal List<CustomItem> Items = new List<CustomItem>();
        internal List<CustomRecipe> Recipes = new List<CustomRecipe>();
        internal List<CustomStatusEffect> StatusEffects = new List<CustomStatusEffect>();

        /// <summary>
        /// Event that get fired after the ObjectDB get init and before its filled with custom items.
        /// Your code will execute once unless you resub, the event get cleared after each fire.
        /// </summary>
        public static Action OnBeforeCustomItemsAdded;

        /// <summary>
        /// Event that get fired after the ObjectDB get init and filled with custom items.
        /// Your code will execute once unless you resub, the event get cleared after each fire.
        /// </summary>
        public static Action OnAfterInit;

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
            //foreach (CustomItem obj in Items)
            //{
            //    ObjectDB.instance.m_items.Add(obj);
            //    Debug.Log("Loaded item: " + obj.name);
            //}

            //// Load recipes
            //foreach (Recipe recipe in Recipes)
            //{
            //    ObjectDB.instance.m_recipes.Add(recipe);
            //    Debug.Log("Loaded item recipe: " + recipe.name);
            //}
            On.ObjectDB.Awake += AddCustomData;
            On.Player.Load += ReloadKnownRecipes;

            SaveCustomData.Init();

            ItemDropMockFix.Switch(true);

            // Update hashes
            ReflectionHelper.InvokePrivate(ObjectDB.instance, "UpdateItemHashes");
        }

        /// <summary>
        /// Registers a new item from given prefab name
        /// </summary>
        /// <param name="prefabName">Name of prefab to use for item</param>
        //public void RegisterItem(string prefabName)
        //{
        //    Items.Add(PrefabManager.Instance.GetPrefab(prefabName));
        //}

        /// <summary>
        /// Registers new item from given GameObject. GameObject MUST be also registered as a prefab
        /// </summary>
        /// <param name="item">GameObject to use for item</param>
        //public void RegisterItem(GameObject item)
        //{
        //    // Set layer if not already set
        //    if (item.layer == 0)
        //    {
        //        item.layer = LayerMask.NameToLayer("item");
        //    }

        //    Items.Add(item);
        //}

        /// <summary>
        /// Registers a new recipe
        /// </summary>
        /// <param name="recipeConfig">Recipe details</param>

        //public void RegisterRecipe(RecipeConfig recipeConfig)
        //{
        //    Recipe recipe = recipeConfig.GetRecipe();

        //    if (recipe == null)
        //    {
        //        Debug.LogError("Failed to add recipe for item: " + recipeConfig.Item);
        //        return;
        //    }

        //    Recipes.Add(recipe);
        //}
        public bool Add(CustomItem customItem)
        {
            if (customItem.IsValid())
            {
                Items.Add(customItem);
                customItem.ItemPrefab.NetworkRegister();

                return true;
            }

            return false;
        }

        public bool Add(CustomRecipe customRecipe)
        {
            Recipes.Add(customRecipe);

            return true;
        }

        public bool Add(CustomStatusEffect customStatusEffect)
        {
            StatusEffects.Add(customStatusEffect);

            return true;
        }

        public void RegisterRecipe(CustomRecipe recipe)
        {
            if (recipe == null)
            {
                Debug.LogError("Failed to add null recipe");
                return;
            }

            Recipes.Add(recipe);
        }

        public GameObject GetItemPrefab(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return ObjectDB.instance.GetItemPrefab(name);
        }

        public ItemDrop GetItemDrop(string name)
        {
            return GetItemPrefab(name)?.GetComponent<ItemDrop>();
        }


        private void AddCustomItems(ObjectDB self)
        {
            foreach (var customItem in Items)
            {
                var itemDrop = customItem.ItemDrop;
                if (customItem.FixReference)
                {
                    customItem.ItemPrefab.FixReferences();

                    itemDrop.m_itemData.m_dropPrefab = customItem.ItemPrefab;
                    itemDrop.m_itemData.m_shared.FixReferences();
                    customItem.FixReference = false;
                }

                self.m_items.Add(customItem.ItemPrefab);
                JotunnLib.Logger.LogInfo($"Added custom item : Prefab Name : {customItem.ItemPrefab.name} | Token : {customItem.ItemDrop.TokenName()}");
            }
        }

        private void AddCustomRecipes(ObjectDB self)
        {
            foreach (var customRecipe in Recipes)
            {
                var recipe = customRecipe.Recipe;

                if (customRecipe.FixReference)
                {
                    recipe.FixReferences();
                    customRecipe.FixReference = false;
                }

                if (customRecipe.FixRequirementReferences)
                {
                    foreach (var requirement in recipe.m_resources)
                    {
                        requirement.FixReferences();
                    }

                    customRecipe.FixRequirementReferences = false;
                }

                self.m_recipes.Add(recipe);
                JotunnLib.Logger.LogInfo($"Added recipe for : {recipe.m_item.TokenName()}");
            }
        }

        private void AddCustomStatusEffects(ObjectDB self)
        {
            foreach (var customStatusEffect in StatusEffects)
            {
                var statusEffect = customStatusEffect.StatusEffect;
                if (customStatusEffect.FixReference)
                {
                    statusEffect.FixReferences();
                    customStatusEffect.FixReference = false;
                }

                self.m_StatusEffects.Add(statusEffect);
                JotunnLib.Logger.LogInfo($"Added status effect : {statusEffect.m_name}");
            }
        }

        private void AddCustomData(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            var isValid = self.IsValid();
            ItemDropMockFix.Switch(!isValid);

            orig(self);

            if (isValid)
            {
                OnBeforeCustomItemsAdded.SafeInvoke();
                OnBeforeCustomItemsAdded = null;
                AddCustomItems(self);
                AddCustomRecipes(self);
                AddCustomStatusEffects(self);

                self.UpdateItemHashes();

                OnAfterInit.SafeInvoke();
                OnAfterInit = null;
            }
        }

        private void ReloadKnownRecipes(On.Player.orig_Load orig, Player self, ZPackage pkg)
        {
            orig(self, pkg);

            if (Game.instance == null)
            {
                return;
            }

            self.UpdateKnownRecipesList();
        }
    }
}
