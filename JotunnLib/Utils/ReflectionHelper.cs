using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Jotunn.Utils
{
    /// <summary>
    ///     Various utility methods aiding Reflection tasks.
    /// </summary>
    public static class ReflectionHelper
    {
        public const BindingFlags AllBindingFlags = (BindingFlags)(-1);

        public static bool IsSameOrSubclass(this Type type, Type @base)
        {
            return type.IsSubclassOf(@base) || type == @base;
        }

        public static bool IsEnumerable(this Type self)
        {
            return typeof(IEnumerable).IsAssignableFrom(self) && self != typeof(string);
        }

        // https://stackoverflow.com/a/21995826
        public static Type GetEnumeratedType(this Type type) =>
            type?.GetElementType() ??
            (typeof(IEnumerable).IsAssignableFrom(type) ? type.GetGenericArguments().FirstOrDefault() : null);

        public static Type GetCallingType()
        {
            return new StackTrace().GetFrames()
                .First(x => x.GetMethod().ReflectedType?.Assembly != typeof(Main).Assembly).GetMethod()
                .ReflectedType;
        }

        public static Assembly GetCallingAssembly()
        {
            return new StackTrace().GetFrames()
                    .First(x => x.GetMethod().ReflectedType?.Assembly != typeof(Main).Assembly).GetMethod()
                .ReflectedType?
                .Assembly;
        }

        public static object InvokePrivate(object instance, string name, object[] args = null)
        {
            MethodInfo method = instance.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
            {
                Type[] types = args == null ? Type.EmptyTypes : args.Select(arg => arg.GetType()).ToArray();
                method = instance.GetType().GetMethod(name, types);
            }

            if (method == null)
            {
                Logger.LogError("Method " + name + " does not exist on type: " + instance.GetType());
                return null;
            }

            return method.Invoke(instance, args);
        }

        public static T GetPrivateProperty<T>(object instance, string name)
        {
            PropertyInfo var = instance.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (var == null)
            {
                Logger.LogError("Property " + name + " does not exist on type: " + instance.GetType());
                return default(T);
            }

            return (T)var.GetValue(instance);
        }

        public static T GetPrivateField<T>(object instance, string name)
        {
            FieldInfo var = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (var == null)
            {
                Logger.LogError("Variable " + name + " does not exist on type: " + instance.GetType());
                return default(T);
            }

            return (T)var.GetValue(instance);
        }

        public static T GetPrivateField<T>(Type type, string name)
        {
            FieldInfo var = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);

            if (var == null)
            {
                Logger.LogError("Variable " + name + " does not exist on type: " + type);
                return default(T);
            }

            return (T)var.GetValue(null);
        }

        public static void SetPrivateField(object instance, string name, object value)
        {
            FieldInfo var = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (var == null)
            {
                Logger.LogError("Variable " + name + " does not exist on type: " + instance.GetType());
                return;
            }

            var.SetValue(instance, value);
        }

        /// <summary>
        ///     Cache for Reflection tasks.
        /// </summary>
        public static class Cache
        {
            private static MethodInfo _enumerableToArray;
            public static MethodInfo EnumerableToArray
            {
                get
                {
                    if (_enumerableToArray == null)
                    {
                        _enumerableToArray = typeof(Enumerable).GetMethod("ToArray", AllBindingFlags);
                    }

                    return _enumerableToArray;
                }
            }

            private static MethodInfo _enumerableCast;
            public static MethodInfo EnumerableCast
            {
                get
                {
                    if (_enumerableCast == null)
                    {
                        _enumerableCast = typeof(Enumerable).GetMethod("Cast", AllBindingFlags);
                    }

                    return _enumerableCast;
                }
            }
        }
    }
}
