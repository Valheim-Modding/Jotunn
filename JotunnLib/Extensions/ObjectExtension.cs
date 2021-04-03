using System.Linq;

namespace JotunnLib
{

    static class ObjectExtensions
    {
        public static string GetObjectString(this object obj)
        {
            if (obj == null)
            {
                return "null";
            }
            else
            {
                var output = $"{obj}";
                var type = obj.GetType();
                var publicFields = type.GetFields().Where(f => f.IsPublic);
                foreach (var f in publicFields)
                {
                    var value = f.GetValue(obj);
                    var valueString = value == null ? "null" : value.ToString();
                    output += $"\n {f.Name}: {valueString}";
                }
                var publicProps = type.GetProperties();
                foreach (var f in publicProps)
                {
                    var value = f.GetValue(obj, null);
                    var valueString = value == null ? "null" : value.ToString();
                    output += $"\n {f.Name}: {valueString}";
                }

                return output;
            }
        }
    }
}