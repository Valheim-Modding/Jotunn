using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Various utility methods aiding Reflection tasks.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        ///     All possible binding flags
        /// </summary>
        public const BindingFlags AllBindingFlags = (BindingFlags)(-1);

        /// <summary>
        ///     Determines whether this type is the same or a subclass of another type
        /// </summary>
        /// <param name="type">this type</param>
        /// <param name="base">Type against the type is checked</param>
        /// <returns>true if this type is the same or a subtype</returns>
        public static bool IsSameOrSubclass(this Type type, Type @base)
        {
            return type.IsSubclassOf(@base) || type == @base;
        }

        /// <summary>
        ///     Determines if this type inherits from <see cref="IEnumerable"/>
        /// </summary>
        /// <param name="type">this type</param>
        /// <returns></returns>
        public static bool IsEnumerable(this Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
        }

        /// <summary>
        ///     Get the generic <see cref="IEnumerable"/> type of this type.<br />
        ///     https://stackoverflow.com/a/21995826
        /// </summary>
        /// <param name="type">this type</param>
        /// <returns></returns>
        public static Type GetEnumeratedType(this Type type) =>
            type?.GetElementType() ??
            (typeof(IEnumerable).IsAssignableFrom(type) ? type.GetGenericArguments().FirstOrDefault() : null);

        /// <summary>
        ///     Get the <see cref="Type.ReflectedType"/> of the first caller outside of this assembly
        /// </summary>
        /// <returns>The reflected type of the first caller outside of this assembly</returns>
        public static Type GetCallingType()
        {
            return (new StackTrace().GetFrames() ?? Array.Empty<StackFrame>())
                .First(x => x.GetMethod().ReflectedType?.Assembly != typeof(Main).Assembly)
                .GetMethod()
                .ReflectedType;
        }

        /// <summary>
        ///     Get the <see cref="Assembly"/> of the first caller outside of this assembly
        /// </summary>
        /// <returns>The assembly of the first caller outside of this assembly</returns>
        public static Assembly GetCallingAssembly()
        {
            return (new StackTrace().GetFrames() ?? Array.Empty<StackFrame>())
                .First(x => x.GetMethod().ReflectedType?.Assembly != typeof(Main).Assembly)
                .GetMethod()
                .ReflectedType?
                .Assembly;
        }

        /// <summary>
        ///     Invoke a private method of any class instance
        /// </summary>
        /// <param name="instance">Instance of the class</param>
        /// <param name="name">Name of the method</param>
        /// <param name="args">Argument values (if any) of the method</param>
        /// <returns>The return of the method as an <see cref="object"/></returns>
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

        /// <summary>
        ///     Get the value of a private property of any class instance
        /// </summary>
        /// <typeparam name="T">Generic property type</typeparam>
        /// <param name="instance">Instance of the class</param>
        /// <param name="name">Name of the property</param>
        /// <returns>The value of the property</returns>
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

        /// <summary>
        ///     Get the value of a private field of any class instance
        /// </summary>
        /// <typeparam name="T">Generic field type</typeparam>
        /// <param name="instance">Instance of the class</param>
        /// <param name="name">Name of the field</param>
        /// <returns>The value of the field</returns>
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
        
        /// <summary>
        ///     Get the value of a private static field of any class
        /// </summary>
        /// <typeparam name="T">Generic field type</typeparam>
        /// <param name="type">Type of the class</param>
        /// <param name="name">Name of the field</param>
        /// <returns>The value of the field</returns>
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
        
        /// <summary>
        ///     Set a value of a private field of any class instance
        /// </summary>
        /// <param name="instance">Instance of the class</param>
        /// <param name="name">Name of the field</param>
        /// <param name="value">New value of the field</param>
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
            /// <summary>
            ///     <see cref="MethodInfo"/> of <see cref="Enumerable.ToArray{TSource}"/>
            /// </summary>
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
            
            private static MethodInfo _enumerableToList;
            /// <summary>
            ///     <see cref="MethodInfo"/> of <see cref="Enumerable.ToList{TSource}"/>
            /// </summary>
            public static MethodInfo EnumerableToList
            {
                get
                {
                    if (_enumerableToList == null)
                    {
                        _enumerableToList = typeof(Enumerable).GetMethod("ToList", AllBindingFlags);
                    }

                    return _enumerableToList;
                }
            }

            private static MethodInfo _enumerableCast;
            /// <summary>
            ///     <see cref="MethodInfo"/> of <see cref="Enumerable.Cast{TResult}"/>
            /// </summary>
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
