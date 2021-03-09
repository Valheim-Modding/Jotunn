using System.Reflection;
using UnityEngine;

namespace JotunnLib.Utils
{
    public class ReflectionUtils
    {
        public static object InvokePrivate(object instance, string name, object[] args = null)
        {
            MethodInfo method = instance.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
            {
                Debug.LogError("Method " + name + " does not exist on type: " + instance.GetType());
                return null;
            }

            return method.Invoke(instance, args);
        }

        public static T GetPrivateField<T>(object instance, string name)
        {
            FieldInfo var = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (var == null)
            {
                Debug.LogError("Variable " + name + " does not exist on type: " + instance.GetType());
                return default(T);
            }

            return (T)var.GetValue(instance);
        }

        public static void SetPrivateField(object instance, string name, object value)
        {
            FieldInfo var = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (var == null)
            {
                Debug.LogError("Variable " + name + " does not exist on type: " + instance.GetType());
                return;
            }

            var.SetValue(instance, value);
        }
    }
}
