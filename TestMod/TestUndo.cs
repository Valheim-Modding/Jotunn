using System.Collections.Generic;
using BepInEx;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace TestMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    internal class TestUndo : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.testundo";
        private const string ModName = "Jotunn Test Undo";
        private const string ModVersion = "0.1.0";

        // Defining a queue name for the UndoManager that is shared by all our actions
        private const string QueueName = "TestUndo";

        private void Awake()
        {
            // Add all our commands to the CommandManager so we can use those in the game's console
            CommandManager.Instance.AddConsoleCommand(new TestCreateCommand());
            CommandManager.Instance.AddConsoleCommand(new TestRemoveCommand());
            CommandManager.Instance.AddConsoleCommand(new TestUndoCommand());
            CommandManager.Instance.AddConsoleCommand(new TestRedoCommand());
            CommandManager.Instance.AddConsoleCommand(new TestListCommand());
        }

        public class TestCreateCommand : ConsoleCommand
        {
            public override string Name => "undotest.create";

            public override string Help => "Creates stuff to test the undo manager";

            public override void Run(string[] args)
            {
                // Do some validation
                if (!Player.m_localPlayer)
                {
                    Console.instance.Print("Can be used in game only!");
                    return;
                }

                // Get a random prefab from the game
                GameObject prefab = PrefabManager.Instance.GetPrefab("Hammer");
                if (!prefab)
                {
                    Console.instance.Print("Can't find prefab");
                    return;
                }

                // Instantiate that prefab in the game
                var obj = Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity);

                // Create an UndoCreate action with the ZDO of the prefab
                var action = new UndoActions.UndoCreate(new[] { obj.GetComponent<ZNetView>().GetZDO() });
                UndoManager.Instance.Add(QueueName, action);

                // Do some console output
                Console.instance.Print("Created Hammer");
            }
        }

        public class TestRemoveCommand : ConsoleCommand
        {
            public override string Name => "undotest.remove";

            public override string Help => "Remove hovered stuff to test the undo manager";

            public override void Run(string[] args)
            {
                // Do some validation
                if (!Player.m_localPlayer)
                {
                    Console.instance.Print("Can be used in game only!");
                    return;
                }

                // Get the current hovered object's ZDO
                if (!Player.m_localPlayer.GetHoverObject())
                {
                    Console.instance.Print("Nothing hovered!");
                    return;
                }
                var hoverObject = Player.m_localPlayer.GetHoverObject();
                var zNetView = hoverObject.GetComponentInParent<ZNetView>();
                if (!zNetView || !zNetView.IsValid())
                {
                    return;
                }
                
                // Create an UndoRemove action with that ZDO
                var action = new UndoActions.UndoRemove(new[] { zNetView.GetZDO() });
                UndoManager.Instance.Add(QueueName, action);

                // Remove the ZDO from the game
                zNetView.GetZDO().SetOwner(ZDOMan.GetSessionID());
                ZNetScene.instance.Destroy(zNetView.gameObject);
                
                // Do some console output
                Console.instance.Print("Removed GameObject");
            }
        }

        public class TestUndoCommand : ConsoleCommand
        {
            public override string Name => "undotest.undo";

            public override string Help => "Undo the stuff";

            public override void Run(string[] args)
            {
                if (!Player.m_localPlayer)
                {
                    Console.instance.Print("Can be used in game only!");
                    return;
                }

                // Calling Undo() on the manager using the queue's name will
                // undo your last added action to that queue
                UndoManager.Instance.Undo(QueueName);
            }
        }

        public class TestRedoCommand : ConsoleCommand
        {
            public override string Name => "undotest.redo";

            public override string Help => "Redo the stuff";

            public override void Run(string[] args)
            {
                if (!Player.m_localPlayer)
                {
                    Console.instance.Print("Can be used in game only!");
                    return;
                }
                
                // Calling Redo() on the manager using the queue's name will
                // redo the last action which was removed by using Undo() from that queue
                UndoManager.Instance.Redo(QueueName);
            }
        }

        public class TestListCommand : ConsoleCommand
        {
            public override string Name => "undotest.list";

            public override string Help => "List a queue's content";

            public override void Run(string[] args)
            {
                string queueName = QueueName;
                if (args.Length == 1 && !string.IsNullOrEmpty(args[0]))
                {
                    queueName = args[0];
                }
                
                // List the queue's content in the console
                Console.instance.Print(UndoManager.Instance.GetQueue(queueName).ToString());
            }

            public override List<string> CommandOptionList()
            {
                return UndoManager.Instance.GetQueueNames();
            }
        }
    }
}
