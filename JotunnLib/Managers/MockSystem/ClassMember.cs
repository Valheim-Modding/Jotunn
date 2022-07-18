using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jotunn.Utils;

namespace Jotunn {
    internal class ClassMember
    {
        public readonly List<MemberBase> members = new List<MemberBase>();

        private static readonly Dictionary<Type, ClassMember> CachedClassMembers = new Dictionary<Type, ClassMember>();

        private ClassMember(IEnumerable<FieldInfo> fieldInfos, IEnumerable<PropertyInfo> propertyInfos)
        {
            foreach (var fieldInfo in fieldInfos)
            {
                members.Add(new FieldMember(fieldInfo));
            }

            foreach (var propertyInfo in propertyInfos)
            {
                members.Add(new PropertyMember(propertyInfo));
            }
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

            classMember = new ClassMember(fields, properties);
            CachedClassMembers[type] = classMember;
            return classMember;
        }
    }
}
