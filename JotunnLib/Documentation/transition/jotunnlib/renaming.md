# Renaming
If you're familiar with the current naming of the Manager type classes in JotunnLib, then you should be fairly comfortable using Jotunn. Most of the functionality of the Managers is kept the same. However, some have been renamed, and some have had their methods renamed.  

For a more in-depth look at all the new naming, please check out the [tutorials](../../home/tutorials/overview.md) section.

## Method names
In JotunnLib, most methods that were used to add new items to the game all began with `Register`, such as `PieceManager.Instance.RegisterPiece`. In Jotunn, these have all been changed to begin with `Add` instead. So, for example:
- `PieceManager.Instance.RegisterPiece` becomes `PieceManager.Instance.AddPiece`
- `SkillManager.Instance.RegisterSkill` becomes `SkillManager.Instance.AddSkill`
and so on.  

In addition, a few of these methods have had some minor changes to what arguments they take.

## Manager names
The only notable change here at the moment has to do with the `ObjectManager` from JotunnLib. This has been renamed to `ItemManager`. It retains all of the functionality it had in JotunnLib, and also has more, but just under a new name. Recipes and items can be added via the following functions:

```cs
ItemManager.Instance.AddItem(...);
ItemManager.Instance.AddRecipe(...);

```