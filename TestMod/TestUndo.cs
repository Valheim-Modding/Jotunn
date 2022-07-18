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

        private const string QueueName = "TestUndo";

        private void Awake()
        {
            CommandManager.Instance.AddConsoleCommand(new TestCreateCommand());
            CommandManager.Instance.AddConsoleCommand(new TestRemoveCommand());
            CommandManager.Instance.AddConsoleCommand(new TestUndoCommand());
            CommandManager.Instance.AddConsoleCommand(new TestRedoCommand());
        }

        public class TestCreateCommand : ConsoleCommand
        {
            public override string Name => "undotest.create";

            public override string Help => "Creates stuff to test the undo manager";

            public override void Run(string[] args)
            {
                if (!Player.m_localPlayer)
                {
                    Console.instance.Print("Can be used in game only!");
                    return;
                }

                GameObject prefab = PrefabManager.Instance.GetPrefab("Hammer");
                if (!prefab)
                {
                    Console.instance.Print("Can't find prefab");
                    return;
                }

                var obj = Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity);
                var action = new UndoActions.UndoCreate(new[] { obj.GetComponent<ZNetView>().GetZDO() });
                UndoManager.Instance.Add(QueueName, action);
            }
        }

        public class TestRemoveCommand : ConsoleCommand
        {
            public override string Name => "undotest.remove";

            public override string Help => "Remove hovered stuff to test the undo manager";

            public override void Run(string[] args)
            {
                if (!Player.m_localPlayer)
                {
                    Console.instance.Print("Can be used in game only!");
                    return;
                }

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
                
                var action = new UndoActions.UndoRemove(new[] { zNetView.GetZDO() });
                UndoManager.Instance.Add(QueueName, action);

                zNetView.GetZDO().SetOwner(ZDOMan.instance.GetMyID());
                ZNetScene.instance.Destroy(zNetView.gameObject);
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

                UndoManager.Instance.Redo(QueueName);
            }
        }
    }
}
