# Registering custom console commands
Custom console commands can be created by creating a class that inherits from [ConsoleCommand](xref:JotunnLib.Entities.ConsoleCommand). The command can then be registered by calling [RegisterConsoleCommand](JotunnLib.Managers.CommandManager.RegisterConsoleCommand(ConsoleCommand)). The command should be registered when your mod is loaded, in `Awake`.  

This will register your custom console command into the game, and your command will be shown when the user types `help` into their console.

_Note: Console command names **must** be unique._

## Example
From the [TestMod](https://github.com/jotunnlib/jotunnlib/blob/main/TestMod/ConsoleCommands/PrintItemsCommand.cs), creating a custom console command which will print all registered item names.  

The custom console command:
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

Registering the console command:

```cs
private void Awake()
{
    CommandManager.Instance.RegisterConsoleCommand(new PrintItemsCommand());
}
```