using System;
using System.Collections.Generic;
using On.Steamworks;

namespace Jotunn
{
    /// <summary>
    ///     Helper class for C# Events.
    /// </summary>
    public static class EventExtensions
    {
        /// <summary>
        ///     Try catch the delegate chain so that it doesnt break on the first failing Delegate.
        /// </summary>
        /// <param name="events"></param>
        public static void SafeInvoke(this Action events)
        {
            if (events == null)
            {
                return;
            }

            foreach (Action @event in events.GetInvocationList())
            {
                try
                {
                    @event();
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }

        /// <summary>
        ///     Try catch the delegate chain so that it doesnt break on the first failing Delegate.
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <param name="events"></param>
        /// <param name="arg1"></param>
        public static void SafeInvoke<TArg1>(this Action<TArg1> events, TArg1 arg1)
        {
            if (events == null)
            {
                return;
            }

            foreach (Action<TArg1> @event in events.GetInvocationList())
            {
                try
                {
                    @event(arg1);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }

        public static void ClearEvent(this Action events, List<Action> items)
        {
            foreach (var @event in items)
            {
                events -= @event;
            }

            items.Clear();
        }
    }
}
