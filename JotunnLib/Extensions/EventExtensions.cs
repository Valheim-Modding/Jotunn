using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Jotunn
{
    /// <summary>
    ///     Helper class for C# Events.
    /// </summary>
    internal static class EventExtensions
    {
        /// <summary>
        ///     try/catch the delegate chain so that it doesnt break on the first failing Delegate.
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
                    Logger.LogWarning($"Exception thrown at event {(new StackFrame(1).GetMethod().Name)} in {@event.Method.DeclaringType.Name}.{@event.Method.Name}:\n{e}");
                }
            }
        }

        /// <summary>
        ///     try/catch the delegate chain so that it doesnt break on the first failing Delegate.
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
                    Logger.LogWarning($"Exception thrown at event {(new StackFrame(1).GetMethod().Name)} in {@event.Method.DeclaringType.Name}.{@event.Method.Name}:\n{e}");
                }
            }
        }
        
        /// <summary>
        ///     try/catch the delegate chain so that it doesnt break on the first failing Delegate.
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <param name="events"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public static void SafeInvoke<TArg1, TArg2>(this Action<TArg1, TArg2> events, TArg1 arg1, TArg2 arg2)
        {
            if (events == null)
            {
                return;
            }

            foreach (Action<TArg1, TArg2> @event in events.GetInvocationList())
            {
                try
                {
                    @event(arg1, arg2);
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Exception thrown at event {(new StackFrame(1).GetMethod().Name)} in {@event.Method.DeclaringType.Name}.{@event.Method.Name}:\n{e}");
                }
            }
        }

        /// <summary>
        ///     try/catch the delegate chain so that it doesnt break on the first failing Delegate.
        /// </summary>
        /// <typeparam name="TEventArg"></typeparam>
        /// <param name="events"></param>
        /// <param name="sender"></param>
        /// <param name="arg1"></param>
        public static void SafeInvoke<TEventArg>(this EventHandler<TEventArg> events, object sender, TEventArg arg1)
        {
            if (events == null)
            {
                return;
            }

            foreach (EventHandler<TEventArg> @event in events.GetInvocationList())
            {
                try
                {
                    @event(sender, arg1);
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Exception thrown at event {(new StackFrame(1).GetMethod().Name)} in {@event.Method.DeclaringType.Name}.{@event.Method.Name}:\n{e}");
                }
            }
        }
    }
}
