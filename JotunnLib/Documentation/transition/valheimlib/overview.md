# Transitioning from ValheimLib
This walkthrough requires previous steps from the [Quick Start](../../tutorials/data/quickstart.md) before proceeding.

Once your Jotunn dependencies have been resolved, its time that we start stripping out ValheimLib. First, we remove the VL binary from our dependencies, and then removing the `using` namespaces.

```cs
//using ValheimLib;
//using ValheimLib.ODB;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
```

### Code Refactor
Now, we can begin to refactor our code. Primarily, VL is used to fix references to vanilla assets at runtime, and therefore we will quickly run through Cinnabun's backpack example to use as a conversion base, describing the process as we go.

Starting asset code:
```cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using UnityEngine;
//using ValheimLib;
//using ValheimLib.ODB;
using Jotunn;
using Jotunn.Entities;
using Resources = Backpack.Properties.Resources;
using Jotunn.Managers;

namespace Backpack
{
    public class ModAssets
    {
        private static ModAssets instance;
        public static ModAssets Instance
        {
            get
            {
                if (instance == null) instance = new ModAssets();
                return instance;
            }
        
        }
        
        private GameObject IronBackpackPrefab;

        private GameObject SilverBackpackPrefab;

        ModAssets()
        {

        }
        

        public void Init()
        {
            var ab = AssetBundle.LoadFromMemory(Properties.Resources.eviesbackpacks);
            IronBackpackPrefab = InitPrefab(ab,
                "Assets/Evie/CapeIronBackpack.prefab");
            LoadCraftedItem(IronBackpackPrefab, new List<Piece.Requirement>
            {
                MockRequirement.Create("LeatherScraps", 10),
                MockRequirement.Create("DeerHide", 2),
                MockRequirement.Create("Iron", 4),
            });
            SilverBackpackPrefab = InitPrefab(ab, 
                "Assets/Evie/CapeSilverBackpack.prefab");
            LoadCraftedItem(SilverBackpackPrefab, new List<Piece.Requirement>
            {
                MockRequirement.Create("LeatherScraps", 5),
                MockRequirement.Create("DeerHide", 10),
                MockRequirement.Create("Silver", 4),
            });
            InitLocalisation();
        }

        private GameObject InitPrefab(AssetBundle ab, string loc)
        {
            var prefab = ab.LoadAsset<GameObject>(loc);
            if(!prefab) Main.log.LogWarning($"Failed to load prefab: {loc}");
            return prefab;
        }

        private void LoadCraftedItem(GameObject prefab, List<Piece.Requirement> ingredients, string craftingStation = "piece_workbench")
        {
            if(prefab) 
            {
                var CI = new CustomItem(prefab, true);
                var recipe = ScriptableObject.CreateInstance<Recipe>();
                recipe.m_item = prefab.GetComponent<ItemDrop>();
                recipe.m_craftingStation = Mock<CraftingStation>.Create(craftingStation);
                recipe.m_resources = ingredients.ToArray();
                var CR = new CustomRecipe(recipe, true, true);
                ObjectDBHelper.Add(CI);
                ObjectDBHelper.Add(CR);
                Main.log.LogDebug($"Successfully loaded new CraftedItem {prefab.name} for {craftingStation}.");
            }
        }

        private static void InitLocalisation()
        {
            ResourceSet resourceSet =   Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            foreach (DictionaryEntry token in resourceSet)
            {
                if (token.Key.ToString().StartsWith("$"))
                {
                    Language.AddToken(token.Key.ToString(), token.Value.ToString(), false);
                    Main.log.LogDebug($"Added language token for {token.Key}:{token.Value}");
                }
            }
        }
    }
}
```

#### Items and Recipes
Items and recipes have experienced some changes, however for the most part the entity namespaces are unchanged. `CustomItem`, `CustomPiece`, and `CustomRecipe` are now accessible through the `Jotunn.Entities` namespace.
Simply by referencing the entities namespace, we have likely resolved the majority of any errors left from conversion.

Now, one of the errors produced now that we lack the ValheimLib dependency, is `The name 'ObjectDBHelper' does not exist in the current context`. Instead of using the ObjectDBHelper interface in ValheimLib, Jotunn has now consolidated item and recipe collection related functions to the `ItemManager` namespace, that is accessed through its `Instance` property; however we will still use the valheim entity abstractions in order to facilitate fixing of references at runtime. In order to implement these changes, we must change lines 78,79 from:
```cs
ObjectDBHelper.Add(CI);
ObjectDBHelper.Add(CR);
```

to:

```cs
ItemManager.Instance.AddItem(CI);
ItemManager.Instance.AddRecipe(CR);
```

#### Localizations

Localizations are another easy one. In the method above, we can see an implementation where the developer was using embedded resources to store language tokens instead of using the json parser. While the file parse needs no specific transition guide as it remains unchanged, adding language tokens manually has again moved to a different namespace.

We will modify line 92 from:

```cs
Language.AddToken(token.Key.ToString(), token.Value.ToString(), false);
```

to:

```cs
LocalizationManager.Instance.AddToken(token.Key.ToString(), token.Value.ToString(), false);
```
Done! Now all thats left is to ensure that copy: `SolutionDir\packages\JotunnLib\lib\net462\Jotunn.dll` to our `Valheim_Install\BepInEx\plugins\` directory, if you do not already have one there.

Our plugin should now build, run, and load our assets ingame as would normally be expected. Transition complete!