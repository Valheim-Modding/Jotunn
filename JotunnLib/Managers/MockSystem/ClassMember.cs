using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jotunn.Utils;

namespace Jotunn {
    internal class ClassMember
    {
        public List<MemberBase> Members { get; private set; } = new List<MemberBase>();
        public Type Type { get; private set; }

        private static readonly Dictionary<Type, ClassMember> CachedClassMembers = new Dictionary<Type, ClassMember>();

        private ClassMember(Type type, IEnumerable<FieldInfo> fieldInfos, IEnumerable<PropertyInfo> propertyInfos)
        {
            this.Type = type;

            foreach (var fieldInfo in fieldInfos)
            {
                AddMember(new FieldMember(fieldInfo));
            }

            foreach (var propertyInfo in propertyInfos)
            {
                AddMember(new PropertyMember(propertyInfo));
            }
        }

        private void AddMember(MemberBase member)
        {
            if (!member.IsClass || member.MemberType == typeof(string))
            {
                return;
            }

            if (member.EnumeratedType != null && (!member.IsEnumeratedClass || member.EnumeratedType == typeof(string)))
            {
                return;
            }

            Members.Add(member);
        }

        private static T[] GetMembersFromType<T>(Type type, Func<Type, T[]> getMembers)
        {
            var members = getMembers(type);
            var baseType = type.BaseType;

            while (baseType != null)
            {
                var parentMembers = getMembers(baseType);
                members = members.Union(parentMembers).ToArray();
                baseType = baseType.BaseType;
            }

            return members;
        }

        public static ClassMember GetClassMember(Type type)
        {
            if (CachedClassMembers.TryGetValue(type, out ClassMember classMember))
            {
                return classMember;
            }

            const BindingFlags flags = ReflectionHelper.AllBindingFlags & ~BindingFlags.Static;
            var fields = GetMembersFromType(type, t => t.GetFields(flags));
            var properties = GetMembersFromType(type, t => t.GetProperties(flags));

            classMember = new ClassMember(type, fields, properties);
            CachedClassMembers[type] = classMember;
            return classMember;
        }
    }
}
