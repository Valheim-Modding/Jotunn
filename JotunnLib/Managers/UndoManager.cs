using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for handling undo and redo actions in mods. Can handle multiple undo queues.<br/>
    ///     Mods can make their own UndoActions using the provided <see cref="IUndoAction">interface</see>
    ///     or use the default ones Jötunn provides in <see cref="Jotunn.Utils.UndoActions"/>.<br />
    ///     Undo queues get automatically reset on every login and logout.
    /// </summary>
    public class UndoManager : IManager
    {
        /// <summary>
        ///     Interface for actions which can be added to the undo queue.
        /// </summary>
        public interface IUndoAction
        {
            /// <summary>
            ///     Description of this action to show on the queue's history.
            /// </summary>
            string Description();
            
            /// <summary>
            ///     Code to revert whatever was executed.
            /// </summary>
            void Undo();

            /// <summary>
            ///     Code to replay whatever was executed.
            /// </summary>
            void Redo();

            /// <summary>
            ///     Message being displayed after a successful undo.
            /// </summary>
            string UndoMessage();

            /// <summary>
            ///     Message being displayed after a successful redo.
            /// </summary>
            string RedoMessage();
        }

        private static UndoManager _instance;

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static UndoManager Instance => _instance ??= new UndoManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private UndoManager() { }

        static UndoManager()
        {
            ((IManager)Instance).Init();
        }

        /// <summary>
        ///     Container to hold all Queues.
        /// </summary>
        private readonly Dictionary<string, UndoQueue> Queues = new Dictionary<string, UndoQueue>();

        /// <summary>
        ///     Registers all hooks.
        /// </summary>
        void IManager.Init()
        {
            Logger.LogInfo("Initializing UndoManager");
            Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void ClearUndoQueuesBefore(ZNetScene __instance)
            {
                foreach (var queuesValue in Instance.Queues.Values)
                {
                    queuesValue.Reset();
                }
            }

            [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Shutdown)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void ClearUndoQueuesAfter(ZNetScene __instance)
            {
                foreach (var queuesValue in Instance.Queues.Values)
                {
                    queuesValue.Reset();
                }
            }
        }
        
        /// <summary>
        ///     Add a message to the console or in the player HUD
        /// </summary>
        /// <param name="message"></param>
        /// <param name="priority"></param>
        private static void AddMessage(string message, bool priority = true)
        {
            if (Console.IsVisible())
            {
                Console.instance.AddString(message);
            }
            var hud = MessageHud.instance;
            var player = Player.m_localPlayer;
            if (!(player || hud))
            {
                return;
            }
            if (priority)
            {
                var items = hud.m_msgQeue.ToArray();
                hud.m_msgQeue.Clear();
                player.Message(MessageHud.MessageType.TopLeft, message);
                foreach (var item in items)
                {
                    hud.m_msgQeue.Enqueue(item);
                }
                hud.m_msgQueueTimer = 10f;
            }
            else
            {
                player.Message(MessageHud.MessageType.TopLeft, message);
            }
        }
        
        /// <summary>
        ///     Manually create a new queue by name and return it. If the queue already exists
        ///     no new queue is created but the existing is returned.
        /// </summary>
        /// <param name="queueName">Global name of the queue</param>
        /// <param name="maxSteps">Optionally define the max history capacity of a newly generated queue</param>
        /// <returns>The <see cref="UndoQueue"/> with the given name</returns>
        public UndoQueue CreateQueue(string queueName, int maxSteps = 50)
        {
            if (!Queues.TryGetValue(queueName, out var queue))
            {
                queue = new UndoQueue(queueName, maxSteps);
                Queues.Add(queueName, queue);
            }
            return queue;
        }

        /// <summary>
        ///     Get a list of all current undo queues.
        /// </summary>
        /// <returns>List of all registered queue names</returns>
        public List<string> GetQueueNames() => Queues.Keys.OrderBy(x => x).ToList();

        /// <summary>
        ///     Get a queue by name. Creates a new queue if it does not exist.
        /// </summary>
        /// <param name="queueName">Global name of the queue</param>
        /// <returns>The <see cref="UndoQueue"/> with the given name</returns>
        public UndoQueue GetQueue(string queueName)
        {
            if (!Queues.TryGetValue(queueName, out var queue))
            {
                queue = new UndoQueue(queueName);
                Queues.Add(queueName, queue);
            }
            return queue;
        }
        
        /// <summary>
        ///     Add a new action to a queue.<br/>
        ///     If a queue with the provided name does not exist it is automatically created.
        /// </summary>
        /// <param name="queueName">Global name of the queue</param>
        /// <param name="action">Mod provided action which can undo and redo whatever was executed</param>
        public void Add(string queueName, IUndoAction action) => GetQueue(queueName).Add(action);

        /// <summary>
        ///     Execute the undo action of the item at the queue's current position and decrease the position pointer.<br/>
        ///     If a queue with the provided name does not exist it is automatically created.
        /// </summary>
        /// <param name="queueName">Global name of the queue</param>
        /// <returns>true if an action was undone, false if no actions exist or the action failed</returns>
        public bool Undo(string queueName) => GetQueue(queueName).Undo();

        /// <summary>
        ///     Execute the redo action of the item after the queue's current position and increase the position pointer.<br/>
        ///     If a queue with the provided name does not exist it is automatically created.
        /// </summary>
        /// <param name="queueName">Global name of the queue</param>
        /// <returns>true if an action was redone, false if no actions exist or the action failed</returns>
        public bool Redo(string queueName) => GetQueue(queueName).Redo();
        
        /// <summary>
        ///     Undo queue implementation.
        /// </summary>
        public class UndoQueue
        {
            private readonly string Name;
            private readonly int MaxSteps;
            private List<IUndoAction> History = new List<IUndoAction>();
            private int Index = -1;
            private bool Executing = false;
            
            internal UndoQueue(string name)
            {
                Name = name;
                MaxSteps = 50;
            }

            internal UndoQueue(string name, int maxSteps)
            {
                if (maxSteps <= 0 || maxSteps >= 100)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxSteps));
                }
                Name = name;
                MaxSteps = maxSteps;
            }
            
            /// <summary>
            ///     Add a new action to this queue.
            /// </summary>
            /// <param name="action">Mod provided action which can undo and redo whatever was executed</param>
            public void Add(IUndoAction action)
            {
                // During undo/redo more steps won't be added.
                if (Executing)
                {
                    return;
                }
                if (History.Count > MaxSteps - 1)
                {
                    History = History.Skip(History.Count - MaxSteps + 1).ToList();
                }
                if (Index < History.Count - 1)
                {
                    History = History.Take(Index + 1).ToList();
                }
                History.Add(action);
                Index = History.Count - 1;
            }

            /// <summary>
            ///     Execute the undo action of the item at the queue's current position and decrease the position pointer.
            /// </summary>
            /// <returns>true if an action was undone, false if no actions exist or the action failed</returns>
            public bool Undo()
            {
                if (Index < 0)
                {
                    AddMessage("Nothing to undo.");
                    return false;
                }
                bool ret = true;
                Executing = true;
                try
                {
                    History[Index].Undo();
                    AddMessage(History[Index].UndoMessage());
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Exception thrown at index {Index} in queue {Name}:\n{e}");
                    ret = false;
                }
                Index--;
                Executing = false;
                return ret;
            }

            /// <summary>
            ///     Execute the redo action of the item after the queue's current position and increase the position pointer.
            /// </summary>
            /// <returns>true if an action was redone, false if no actions exist or the action failed</returns>
            public bool Redo()
            {
                if (Index < History.Count - 1)
                {
                    bool ret = true;
                    Executing = true;
                    Index++;
                    try
                    {
                        History[Index].Redo();
                        AddMessage(History[Index].RedoMessage());
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Exception thrown at index {Index} in queue {Name}:\n{e}");
                        ret = false;
                    }
                    Executing = false;
                    return ret;
                }
                AddMessage("Nothing to redo.");
                return false;
            }

            /// <summary>
            ///     Reset the queue's history and position pointer to its initial state.
            /// </summary>
            public void Reset()
            {
                History.Clear();
                Index = -1;
                Executing = false;
            }

            /// <summary>
            ///     Get this queue's current position index, -1 when empty.
            /// </summary>
            public int GetIndex() => Index;

            /// <summary>
            ///     Get a string array of this queue's current history.
            /// </summary>
            public string[] GetHistory() => History.Select(x => x.Description()).ToArray();
            
            /// <inheritdoc/>
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Queue \"{Name}\"");
                if (Index < 0)
                {
                    sb.AppendLine("Empty!");
                }
                else
                {
                    int idx = 0;
                    foreach (var action in History)
                    {
                        sb.AppendLine($"[{idx:00}{(Index==idx?"*":" ")}] {action.Description()}");
                        ++idx;
                    }
                }
                return sb.ToString();
            }
        }
    }
}
