using System.Reflection;

namespace ValheimLokiLoader
{
    public class Util
    {
        public static T GetPrivateField<T>(object instance, string name)
        {
            FieldInfo var = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)var.GetValue(instance);
        }

        public static void SetPrivateField(object instance, string name, object value)
        {
            FieldInfo var = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            var.SetValue(instance, value);
        }
    }
}
