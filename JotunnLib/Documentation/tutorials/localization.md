# Localization
Localizations can be added to the game using the [LocalizationManager](xref:JotunnLib.Managers.LocalizationManager) Singleton class. Currently, you can only add localizations to the language being currently used by the user (This will be fixed soon in a new version).  


## Using in-game
To use a localization, you must use your localization keyword for your item display names/descriptions, prefixed by a `$`.  
For example, if you added a localization like for `"item_desc"` such that it maps to `"item description here"`, then you could use it in the following way:
```csharp
ItemDrop item = Prefab.GetComponent<ItemDrop>();
item.m_itemData.m_shared.m_description = "$item_desc";
```
Valheim's UI system will automatically replace all words (without spaces or punctuation) following a `$` with matches from the Localization if found.

## Example
In your mod's `Awake` function, you can register translation using the [RegisterTranslation](JotunnLib.Managers.LocalizationManager.RegisterTranslation(System.String,System.String)) function
```csharp
private void Awake()
{
    LocalizationManager.Instance.RegisterTranslation("item_desc", "Item description here");
}
```

which can then be used as shown above.