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

        /// <summary>
        ///     Container to hold all Queues.
        /// </summary>
        private readonly Dictionary<string, UndoQueue> Queues = new Dictionary<string, UndoQueue>();

        /// <summary>
        ///     Registers all hooks.
        /// </summary>
        public void Init()
        {
            Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void ClearUndoQueuesBefore(ZNetScene __instance) => Instance.Queues.Clear();

            [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Shutdown)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void ClearUndoQueuesAfter(ZNetScene __instance) => Instance.Queues.Clear();
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
        ///     Get a list of all current undo queues.
        /// </summary>
        /// <returns>List of all registered queue names</returns>
        public List<string> GetQueueNames() => Queues.Keys.OrderBy(x => x).ToList();
        
        /// <summary>
        ///     Get a string representation of a given queue including all recorded steps and a marker on the current position pointer.
        /// </summary>
        /// <param name="queueName">Global name of the queue</param>
        /// <returns>New line separated string with all the queue's recorded actions</returns>
        public string GetQueueList(string queueName)
        {
            if (!Queues.TryGetValue(queueName, out var queue))
            {
                return $"Queue \"{queueName}\" not found";
            }
            return queue.ToString();
        }

        /// <summary>
        ///     Create a new queue and optionally specify how many steps are recorded into the queue's history.
        /// </summary>
        /// <param name="queueName">Global name of the queue</param>
        /// <param name="maxSteps">The size of the queue, defaults to 50</param>
        public void Create(string queueName, int maxSteps = 50)
        {
            if (!Queues.TryGetValue(queueName, out _))
            {
                Queues.Add(queueName, new UndoQueue(queueName, maxSteps));
            }
        }

        /// <summary>
        ///     Get or create a queue by name
        /// </summary>
        private UndoQueue GetOrAddQueue(string queueName)
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
        public void Add(string queueName, IUndoAction action) => GetOrAddQueue(queueName).Add(action);

        /// <summary>
        ///     Execute the undo action of the item at the queue's current position and decrease the position pointer.<br/>
        ///     If a queue with the provided name does not exist it is automatically created.
        /// </summary>
        /// <param name="queueName">Global name of the queue</param>
        /// <returns>true if an action was undone, false if no actions exist or the action failed</returns>
        public bool Undo(string queueName) => GetOrAddQueue(queueName).Undo();

        /// <summary>
        ///     Execute the redo action of the item after the queue's current position and increase the position pointer.<br/>
        ///     If a queue with the provided name does not exist it is automatically created.
        /// </summary>
        /// <param name="queueName">Global name of the queue</param>
        /// <returns>true if an action was redone, false if no actions exist or the action failed</returns>
        public bool Redo(string queueName) => GetOrAddQueue(queueName).Redo();

        /// <summary>
        ///     Queue implementation
        /// </summary>
        private class UndoQueue
        {
            private readonly string Name;
            private readonly int MaxSteps = 50;
            private List<IUndoAction> History = new List<IUndoAction>();
            private int Index = -1;
            private bool Executing = false;

            public UndoQueue(string name)
            {
                Name = name;
            }

            public UndoQueue(string name, int maxSteps)
            {
                if (maxSteps <= 0 || maxSteps >= 100)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxSteps));
                }
                Name = name;
                MaxSteps = maxSteps;
            }
            
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
