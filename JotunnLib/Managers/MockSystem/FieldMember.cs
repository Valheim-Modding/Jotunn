using System.Reflection;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn {
    internal class FieldMember : MemberBase
    {
        private readonly FieldInfo fieldInfo;

        public FieldMember(FieldInfo fieldInfo)
        {
            this.fieldInfo = fieldInfo;
            MemberType = fieldInfo.FieldType;
            IsUnityObject = MemberType.IsSameOrSubclass(typeof(Object));
            IsClass = MemberType.IsClass;
            HasGetMethod = true;
            EnumeratedType = MemberType.GetEnumeratedType();
            IsEnumerableOfUnityObjects = EnumeratedType?.IsSameOrSubclass(typeof(Object)) == true;
            IsEnumeratedClass = EnumeratedType?.IsClass == true;
        }

        public override object GetValue(object obj)
        {
            return fieldInfo.GetValue(obj);
        }

        public override void SetValue(object obj, object value)
        {
            fieldInfo.SetValue(obj, value);
        }

        public override bool HasCustomAttribute<T>()
        {
            return fieldInfo.GetCustomAttribute<T>() != null;
        }
    }
}
