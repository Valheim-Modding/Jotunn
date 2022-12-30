using System;

namespace Jotunn {
    internal abstract class MemberBase
    {
        public abstract object GetValue(object obj);
        public abstract void SetValue(object obj, object value);
        public abstract bool HasCustomAttribute<T>() where T : Attribute;
        public bool HasGetMethod { get; protected set; }
        public Type MemberType { get; protected set; }
        public Type EnumeratedType { get; protected set; }
        public bool IsUnityObject { get; protected set; }
        public bool IsClass { get; protected set; }
        public bool IsEnumerableOfUnityObjects { get; protected set; }
        public bool IsEnumeratedClass { get; protected set; }
    }
}
