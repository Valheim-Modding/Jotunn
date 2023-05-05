# RPCs

*RPCs (Remote Procedure Calls)* are used to communicate between two or more game instances to invoke asynchronous events or to exchange data between them. Valheim's integrated RPC system works well but has problems with bigger data packages mods might want to send and getting used to it in the first place also takes some time. Jötunn provides an additional layer for the vanilla RPC system which automatically can compress and disassemble larger packages, works with Coroutines to avoid clogging the main game loop and adds some overall simpler abstraction to the vanilla system. You can create your own [CustomRPC](xref:Jotunn.Entities.CustomRPC) classes using the [NetworkManager](xref:Jotunn.Managers.NetworkManager).

> [!NOTE]
> Mods should not use these CustomRPCs in every situation. When transferring small data packages between item instances, using the built-in RPCs attached to the ZNetView still might be the better choice, for example. CustomRPC are better suited if you want to implement a more complicated message system or transfer bigger sized mod data.

## Example

In this example we will implement a [custom console command](console-commands.md) which transfers random data to the server via RPC and reacts on server responses via the same RPC.

**Note**: The code snippets are taken from our [example mod](https://github.com/Valheim-Modding/JotunnModExample).

### Creating the RPC

To create a custom RPC [NetworkManager.AddRPC](xref:Jotunn.Managers.NetworkManager.AddRPC(System.String,Jotunn.Managers.NetworkManager.CoroutineHandler,Jotunn.Managers.NetworkManager.CoroutineHandler)) has to be called. An unique name has to be provided for every custom RPC in a mod. Internally the mod's GUID is also prepended to the name to make sure that different mods using the same name don't interfere with each other. To be able to react when the RPC receives messages from other instances, we have to provide references to methods from the mod using the signature of the [NetworkManager.CoroutineHandler](xref:Jotunn.Managers.NetworkManager.CoroutineHandler). Custom RPCs have to be registered with the vanilla system on the server as well as all clients. To make sure, the RPC is registered, create it as early as possible in your mod's code, preferably when Awake() is called.

```cs
// Custom RPC
public static CustomRPC UselessRPC;

private void Awake()
{
    // Create your RPC as early as possible so it gets registered with the game
    UselessRPC = NetworkManager.Instance.AddRPC(
        "UselessRPC", UselessRPCServerReceive, UselessRPCClientReceive);
}
```

### Invoking RPC calls

Once the custom RPC is created and the game is connected to a server, we can use [CustomRPC.Initiate](xref:Jotunn.Entities.CustomRPC.Initiate) to initiate a RPC call with an empty package to the server. This merely invokes the RPC receive on the server side for us to react to. We can also send ZPackage data over while invoking the RPC call by using one of the [CustomRPC.SendPackage](xref:Jotunn.Entities.CustomRPC.SendPackage(System.Int64,ZPackage)) methods. In our example we chose to provide a custom console command to invoke the RPC call on the client and to send random data in varying sizes to the server. For this tutorial we focus on the part that actually invokes the RPC and sends over the data to the server. Please refer to the [tutorial on custom commands](console-commands.md) for further reading on this topic.

```cs
public override void Run(string[] args)
{
    // Sanitize user's input
    if (args.Length != 1 || !Sizes.Any(x => x.Equals(int.Parse(args[0]))))
    {
        Console.instance.Print($"Usage: {Name} [{string.Join("|", Sizes)}]");
        return;
    }

    // Create a ZPackage and fill it with random bytes
    ZPackage package = new ZPackage();
    System.Random random = new System.Random();
    byte[] array = new byte[int.Parse(args[0]) * 1024 * 1024];
    random.NextBytes(array);
    package.Write(array);

    // Invoke the RPC with the server as the target and our random data package as the payload
    Jotunn.Logger.LogMessage($"Sending {args[0]}MB blob to server.");
    UselessRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
}
```

Note that every call to SendPackage needs one or more targets to send the call to. Valheim's ZRoutedRpc class provides some shortcuts for common usages, most importantly `ZRoutedRpc.Everybody` (every instance *including your local one* receives the call) and `ZRouteRpc.GetServerPeerID()` which determines the current server target ID.

Also note that SendPackage can either be called without returning an Enumerator to our method and with that return. In both cases the sending is done via a Coroutine. You can decide if you want to implement the coroutine yourself (please refer to the Unity [Coroutine scripting manual](https://docs.unity3d.com/2019.4/Documentation/Manual/Coroutines.html) on how to do that) or let Jötunn handle that for you.

> [!WARNING]
> If you don't handle the coroutine yourself, sending the RPC out might be delayed one or more frames, depending on your package size.

### Receiving RPC calls

The custom RPC calls our provided delegate methods every time a message is received. Jötunn's implementation already determines if the current instance is a client or a server for us and provides a delegate for both. Upon receiving our data package from the client, we delay the handling virtually and broadcast a message to all connected peers using the same custom RPC instance.

```cs
public static readonly WaitForSeconds OneSecondWait = new WaitForSeconds(1f);

// React to the RPC call on a server
private IEnumerator UselessRPCServerReceive(long sender, ZPackage package)
{
    Jotunn.Logger.LogMessage($"Received blob, processing");

    string dot = string.Empty;
    for (int i = 0; i < 5; ++i)
    {
        dot += ".";
        Jotunn.Logger.LogMessage(dot);
        yield return OneSecondWait;
    }

    Jotunn.Logger.LogMessage($"Broadcasting to all clients");
    UselessRPC.SendPackage(ZNet.instance.m_peers, new ZPackage(package.GetArray()));
}

public static readonly WaitForSeconds HalfSecondWait = new WaitForSeconds(0.5f);

// React to the RPC call on a client
private IEnumerator UselessRPCClientReceive(long sender, ZPackage package)
{
    Jotunn.Logger.LogMessage($"Received blob, processing");
    yield return null;

    string dot = string.Empty;
    for (int i = 0; i < 10; ++i)
    {
        dot += ".";
        Jotunn.Logger.LogMessage(dot);
        yield return HalfSecondWait;
    }
}
```

The RPC delegates need to be of type [NetworkManager.CoroutineHandler](xref:Jotunn.Managers.NetworkManager.CoroutineHandler). The return type of this delegate is IEnumerator as the custom RPC receive is internally handled via a Coroutine. This means we need to `yield return` at least once in our receiving methods. Please refer to the Unity [Coroutine scripting manual](https://docs.unity3d.com/2019.4/Documentation/Manual/Coroutines.html) for more information about Coroutines. Jötunn also respects all other Unity [YieldInstructions](https://docs.unity3d.com/2019.4/Documentation/ScriptReference/YieldInstruction.html).

### RPC states

Every [CustomRPC](xref:Jotunn.Entities.CustomRPC) instance provides some properties to expose the current state of the RPC:
*  __IsSending__: True, when the RPC is currently sending data.
* __IsReceiving__: True, when the RPC is currently receiving data.
* __IsProcessing__: True, when the RPC is currently processing received data. This is always true while executing the registered delegates.
* __IsProcessingOther__: True, when the RPC is processing received data outside the current delegate call. This should only be used in the registered delegate methods to determine if this RPC is already processing another package.

## Further RPC Documentation

The Valheim Modding Wiki has [a general introduction article](https://github.com/Valheim-Modding/Wiki/wiki/Server-Validated-RPC-System) about RPCs and a handy [RPC reference sheet](https://github.com/Valheim-Modding/Wiki/wiki/RPC-System-Reference-Sheet). Be sure to check these out, too, if you want to learn more about RPCs.