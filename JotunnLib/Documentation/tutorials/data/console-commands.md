# Adding custom console commands
Custom console commands can be created by creating a class that inherits from [ConsoleCommand](xref:JotunnLib.Entities.ConsoleCommand). The command can then be added by calling [AddConsoleCommand](xref:JotunnLib.Managers.CommandManager.AddConsoleCommand(JotunnLib.Entities.ConsoleCommand)). The command should be added when your mod is loaded, in `Awake`.  

This will add your custom console command into the game, and your command will be shown when the user types `help` into their console.

_Note: Console command names **must** be unique._

## Example
From the [TestMod](https://github.com/Valheim-Modding/Jotunn/blob/main/TestMod/ConsoleCommands/PrintItemsCommand.cs), creating a custom console command which will print all added item names.  

The custom console command
```cs
public class PrintItemsCommand : ConsoleCommand
{
    public override string Name => "print_items";

    public override string Help => "Prints all existing items";

    public override void Run(string[] args)
    {
        Console.instance.Print("All items:");
        foreach (GameObject obj in ObjectDB.instance.m_items)
        {
            ItemDrop item = obj.GetComponent<ItemDrop>();
            Console.instance.Print(item.m_itemData.m_shared.m_name);
        }
    }
}
```

Finally, adding the console command

```cs
private void Awake()
{
    CommandManager.Instance.AddConsoleCommand(new PrintItemsCommand());
}
```