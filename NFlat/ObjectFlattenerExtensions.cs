using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NFlat
{
    public static class ObjectFlattenerExtensions
    {
        public struct PropertyContainer
        {
            public bool IsCollection { get; set; }

            public PropertyInfo Property { get; set; }

            public Type Type { get; set; }

            public Func<object, object> ObjGetter { get; set; }

            public Action<object, object> ObjSetter { get; set; }

            public Func<object> Constructor { get; set; }
        }

        private static bool IsAssignableToGenericType(Type givenType, Type genericType, out Type genericArgumentType)
        {
            genericArgumentType = null;
            var interfaceTypes = givenType.GetInterfaces().ToList();
            if (givenType.IsInterface)
                interfaceTypes.Add(givenType);
            foreach (var it in interfaceTypes)
                if (it.IsGenericType)
                    if (it.GetGenericTypeDefinition() == genericType)
                    {
                        var genericArguments = it.GetGenericArguments();
                        if (genericArguments.Length == 1)
                        {
                            genericArgumentType = genericArguments[0];
                            return true;
                        }
                    };

            Type baseType = givenType.BaseType;
            if (baseType == null)
            {
                return false;
            }
            var isAssignable = (baseType.IsGenericType &&
                baseType.GetGenericTypeDefinition() == genericType ||
                    IsAssignableToGenericType(baseType, genericType, out genericArgumentType));
            return isAssignable;
        }

        public static ObjectFlattener<TObject, string> ConfigureAllProperties<TObject>(this ObjectFlattener<TObject, string> objectFlattener)
            where TObject: class, new()
        {
            var mappedTypes = new Dictionary<string, List<PropertyContainer>>();
            var typeStack = new Stack<(Type currentType, string currentPath)>();
            typeStack.Push((typeof(TObject), string.Empty));
            while (typeStack.Count > 0)
            {
                (Type currentType, string currentPath) = typeStack.Pop();
                if (!mappedTypes.ContainsKey(currentPath))
                {
                    var properties = GetProperties(currentType);
                    foreach (var childType in properties)
                    {
                        var childPath = currentPath + "_" + childType.Property.Name;
                        typeStack.Push((childType.Type, childPath));
                    }
                    mappedTypes.Add(currentPath, properties);
                }
            }
            return objectFlattener;
        }

        private static List<PropertyContainer> GetProperties(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(q => !q.PropertyType.IsAbstract);
            return properties.Select(q =>
            {
                Type propertyType = q.PropertyType;
                if (IsAssignableToGenericType(q.PropertyType, typeof(IList<>), out propertyType))
                {
                    return new PropertyContainer { IsCollection = true, Type = propertyType, Property = q };
                }
                return new PropertyContainer { Type = q.PropertyType, ObjGetter = ReflectionExtensions.GetValueGetter<object, object>(q), ObjSetter = ReflectionExtensions.GetValueSetter<object, object>(q), Constructor = () => Activator.CreateInstance(propertyType), Property = q };
            })
            .ToList();
        }
    }
}
