# Item Conversions

An _item conversion_ in Valheim is the process of turning one item into another item using a specific station. For example the cooking of raw meat into cooked meat on the cooking station. Or the creation of ingots from ores in the furnace. Jötunn provides a common interface for adding custom conversions with vanilla items or your own custom items. Adding of custom item conversions is done through the [ItemManager](xref:Jotunn.Managers.ItemManager) singleton class and JVLs abstraction [CustomItemConversion](xref:Jotunn.Entities.CustomItemConversion).

All item conversions will always be loaded **after** all items. However, item conversions will be loaded in the order that you call the `AddItemConversion` function.

## Example



## Valheim conversions

Valheim has three different types of conversion components, which are used in one or more pieces:

Component|Piece
----|----
CookingStation|piece_cookingstation
Fermenter|fermenter
Smelter|smelter
Smelter|blastfurnace
Smelter|charcoal_kiln
Smelter|windmill
Smelter|piece_spinningwheel