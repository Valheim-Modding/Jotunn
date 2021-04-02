﻿using System;
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

        internal override void Init()
        {
            //On.ObjectDB.CopyOtherDB += AddCustomDataFejd;  // very broken, need to come back to that
            On.ObjectDB.Awake += AddCustomData;
            On.Player.Load += ReloadKnownRecipes;
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError("Error, two instances of singleton: " + this.GetType().Name);
                
                return;
            }

            Instance = this;
        }

        internal override void Register()
        {
            Logger.LogInfo("---- Registering custom objects ----");

            // Clear existing items and recipes
            Items.Clear();
            Recipes.Clear();

            
            

            SaveCustomData.Init();

            ItemDropMockFix.Switch(true);

            // Register new items and recipes
            ObjectRegister?.Invoke(null, EventArgs.Empty);
        }

        internal override void Load()
        {
            Logger.LogInfo("---- Loading custom objects ----");

            // Load items
            //foreach (CustomItem obj in Items)
            //{
            //    ObjectDB.instance.m_items.Add(obj);
            //    Logger.LogInfo("Loaded item: " + obj.name);
            //}

            //// Load recipes
            //foreach (Recipe recipe in Recipes)
            //{
            //    ObjectDB.instance.m_recipes.Add(recipe);
            //    Logger.LogInfo("Loaded item recipe: " + recipe.name);
            //}
            

            // Update hashes
            ReflectionHelper.InvokePrivate(ObjectDB.instance, "UpdateItemHashes");
        }

        //public void RegisterRecipe(RecipeConfig recipeConfig)
        //{
        //    Recipe recipe = recipeConfig.GetRecipe();

        //    if (recipe == null)
        //    {
        //        Logger.LogError("Failed to add recipe for item: " + recipeConfig.Item);
        //        return;
        //    }

        //    Recipes.Add(recipe);
        //}
        public bool Add(CustomItem customItem)
        {
            if (customItem.IsValid())
            {
                PrefabManager.Instance.AddPrefab(customItem.ItemPrefab);
                Items.Add(customItem);

                //PrefabManager.Instance.NetworkRegister(customItem.ItemPrefab);
                //customItem.ItemPrefab.NetworkRegister();

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
                Logger.LogError("Failed to add null recipe");
                return;
            }

            Recipes.Add(recipe);
        }

        private void AddCustomItems(ObjectDB objectDB)
        {
            Logger.LogInfo("---- Adding custom items to ObjectDB ----");

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

                objectDB.m_items.Add(customItem.ItemPrefab);

                Logger.LogInfo($"Added custom item : {customItem.ItemPrefab.name} | Token : {customItem.ItemDrop.TokenName()}");
            }
        }

        private void AddCustomRecipes(ObjectDB objectDB)
        {
            Logger.LogInfo("---- Adding custom recipes to ObjectDB ----");

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

                objectDB.m_recipes.Add(recipe);

                Logger.LogInfo($"Added recipe for : {recipe.m_item.TokenName()}");
            }
        }

        private void AddCustomStatusEffects(ObjectDB objectDB)
        {
            Logger.LogInfo("---- Adding custom status effects to ObjectDB ----");

            foreach (var customStatusEffect in StatusEffects)
            {
                var statusEffect = customStatusEffect.StatusEffect;
                if (customStatusEffect.FixReference)
                {
                    statusEffect.FixReferences();
                    customStatusEffect.FixReference = false;
                }

                objectDB.m_StatusEffects.Add(statusEffect);
                Logger.LogInfo($"Added status effect : {statusEffect.m_name}");
            }
        }

        /*private void AddCustomDataFejd(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            orig(self, other);

            var isValid = self.IsValid();
            ItemDropMockFix.Switch(!isValid);

            if (isValid)
            {
                OnBeforeCustomItemsAdded.SafeInvoke();
                OnBeforeCustomItemsAdded = null;

                AddCustomItems(self);

                self.UpdateItemHashes();

                OnAfterInit.SafeInvoke();
                OnAfterInit = null;
            }
        }*/

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
