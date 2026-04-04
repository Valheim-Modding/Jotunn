# Adding Custom Console Commands
Custom console commands can be created by creating a class that inherits from [ConsoleCommand](xref:Jotunn.Entities.ConsoleCommand). The command can then be added by calling [AddConsoleCommand](xref:Jotunn.Managers.CommandManager.AddConsoleCommand(Jotunn.Entities.ConsoleCommand)) on the [CommandManager](xref:Jotunn.Managers.CommandManager). The command should be added when your mod is loaded, in `Awake`.

> [!NOTE]
> Console command names **must** be unique.

## Example

In this example we will create a new spawn command to spawn prefabs into the world. By overriding the `CommandOptionList` method of the base class, we enable tab auto-completion for this command using the prefab name list of the `ZNetScene`.

**Note**: The code snippets are taken from our [example mod](https://github.com/Valheim-Modding/JotunnModExample).

The custom console command:
```cs
public class BetterSpawnCommand : ConsoleCommand
{
    public override string Name => "better_spawn";

    public override string Help => "like spawn but BETTER";

    public override void Run(string[] args)
    {
        if (args.Length == 0)
        {
            return;
        }

        GameObject prefab = PrefabManager.Instance.GetPrefab(args[0]);
        if (!prefab)
        {
            Console.instance.Print("that doesn't exist: " + args[0]);
            return;
        }

        int cnt = args.Length < 2 ? 1 : int.Parse(args[1]);
        for (int i = 0; i < cnt; i++)
        {
            UnityEngine.Object.Instantiate<GameObject>(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity);
        }
    }

    public override List<string> CommandOptionList()
    {
        return ZNetScene.instance?.GetPrefabNames();
    }
}
```

Adding the console command:

```cs
private void Awake()
{
    CommandManager.Instance.AddConsoleCommand(new BetterSpawnCommand());
}
```

### Getting the Terminal context
If you need to know from which Terminal your command was executed from, you can also overwrite `Run(string[] args, Terminal context)` from the base class. This will give you the `Terminal` instance, which is either the Console or the Chat of Valheim. Note that you will have to use `context.AddString()` for output on the calling Terminal instead of directly writing to `Console.instance`.

```cs
public class EchoCommand : ConsoleCommand
{
    public override string Name => "echo";

    public override string Help => "Echoes all text entered to the console or chat";

    public override void Run(string[] args, Terminal context)
    {
        if (args.Length < 1)
        {
            context.AddString("Usage: echo <text>");
        }

        context.AddString(string.Join(" ", args, 0, args.Length));
    }
}
```