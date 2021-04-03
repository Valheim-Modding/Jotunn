using System;
using System.Collections.Generic;
using JotunnLib;
using JotunnLib.Entities;
using JotunnLib.Managers;
using NUnit.Framework;

namespace JotunnTest
{
    [TestFixture]
    public class ObjectManagerTests
    {
        private List<Manager> managers = new List<Manager>();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Logger.Init();
        }

        [SetUp]
        public void SetUp()
        {
            // Create clean singleton-managers
            JotunnLibMain.CreateAllManagers(managers);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Manager manager in managers)
            {
                manager.Clear();
            }

            managers = new List<Manager>();
        }

        [Test]
        public void SecondInstanceNoSet()
        {
            ObjectManager instance = new ObjectManager();
            Assert.IsFalse(instance == ObjectManager.Instance);
        }

        [Test]
        public void RegisterRecipe()
        {
            CustomRecipe customRecipe = new CustomRecipe(null, false, false);
            ObjectManager.Instance.RegisterRecipe(customRecipe);
            Assert.True(ObjectManager.Instance.Recipes[0] == customRecipe);
        }

        [Test]
        public void RegisterRecipeNullRecipe()
        {
            ObjectManager.Instance.RegisterRecipe(null);
            Assert.True(ObjectManager.Instance.Recipes.Count == 0);
        }

        [Test]
        public void AddCustomItem()
        {
            // Mock customItem IsValid()
            Moq.Mock<CustomItem> customItem = new Moq.Mock<CustomItem>(null, false);
            customItem.Setup(item => item.IsValid()).Returns(true);

            bool success = ObjectManager.Instance.Add(customItem.Object);
            Assert.True(success);
            Assert.True(ObjectManager.Instance.Items.Count == 1);
            Assert.True(PrefabManager.Instance.NetworkedModdedPrefabs.Count == 1);
        }

        [Test]
        public void AddCustomItemNullItemPrefab()
        {
            CustomItem customItem = new CustomItem(null, false);
            bool success = ObjectManager.Instance.Add(customItem);
            Assert.False(success);
            Assert.True(ObjectManager.Instance.Items.Count == 0);
        }

        [Test]
        public void AddCustomRecipe()
        {
            CustomRecipe customRecipe = new CustomRecipe(null, false, false);
            bool success = ObjectManager.Instance.Add(customRecipe);
            Assert.True(success);
            Assert.True(ObjectManager.Instance.Recipes.Count == 1);
        }
    }
}
