using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;

namespace NFlat
{
    public interface IPropertyMap<TRawValue>
    {
        void Deserialize(TRawValue rawValue, object @object);
    }

    public interface IConstructorMap
    {
        void Construct(object @object);

        object Get(object @object);
    }

    public class GenericConstructorMap<T> : IConstructorMap
    {
        private readonly Action<T> _constructor;
        private readonly Func<T, object> _getter;

        public GenericConstructorMap(Action<T> constructor, Func<T, object> getter)
        {
            _constructor = constructor;
            _getter = getter;
        }

        public void Construct(object @object)
        {
            _constructor((T)@object);
        }

        public object Get(object @object)
        {
            return _getter((T)@object);
        }
    }

    public abstract class BasePropertyMap<T, K> : IPropertyMap<string>
    {
        private readonly Action<T, K> _propertySetter;

        protected BasePropertyMap(Action<T, K> propertySetter)
        {
            _propertySetter = propertySetter;
        }

        protected abstract K Parse(string rawValue);

        public void Deserialize(string rawValue, object @object)
        {
            _propertySetter((T)@object, Parse(rawValue));
        }
    }

    public class Int32PropertyMap<T> : BasePropertyMap<T, Int32>
    {
        public Int32PropertyMap(Action<T, int> propertySetter) : base(propertySetter)
        {
        }

        protected override int Parse(string rawValue)
        {
            return Int32.Parse(rawValue);
        }
    }

    public class StringPropertyMap<T> : BasePropertyMap<T, string>
    {
        public StringPropertyMap(Action<T, string> propertySetter) : base(propertySetter)
        {
        }

        protected override string Parse(string rawValue)
        {
            return rawValue;
        }
    }

    public class DecimalPropertyMap<T> : BasePropertyMap<T, decimal>
    {
        public DecimalPropertyMap(Action<T, decimal> propertySetter) : base(propertySetter)
        {
        }

        protected override decimal Parse(string rawValue)
        {
            return decimal.Parse(rawValue);
        }
    }

    public class ObjectFlattener<T, TRawValue> where T : new()
    {
        private readonly Dictionary<StringSegment, IPropertyMap<TRawValue>> _propertyMaps = new Dictionary<StringSegment, IPropertyMap<TRawValue>>();

        private readonly Dictionary<StringSegment, IConstructorMap> _constructorMaps = new Dictionary<StringSegment, IConstructorMap>();

        public ObjectFlattener<T, TRawValue> MapProperty(string path, IPropertyMap<TRawValue> propertyMap)
        {
            _propertyMaps.Add(path, propertyMap);
            return this;
        }

        public ObjectFlattener<T, TRawValue> MapNested(string path, IConstructorMap constructorMap)
        {
            _constructorMaps.Add(path, constructorMap);
            return this;
        }

        public T Unflatten(IDictionary<string, TRawValue> data, char separator = '_')
        {
            var result = new T();
            object cur = default;
            var idx = 0;
            var processedMaps = new HashSet<StringSegment>();
            foreach (var p in data.Keys)
            {
                cur = result;
                var prop = StringSegment.Empty;
                var last = 0;
                do
                {
                    idx = p.IndexOf(separator, last);
                    var temp = idx != -1 ? new StringSegment(p, last, idx - last) : new StringSegment(p, last, p.Length - last);
                    var leftSideOfProp = new StringSegment(prop.Buffer, 0, prop.Offset + prop.Length);
                    if (_constructorMaps.TryGetValue(leftSideOfProp, out var constructorPropertyMap))
                    {
                        if (processedMaps.Add(leftSideOfProp))
                            constructorPropertyMap.Construct(cur);
                        cur = constructorPropertyMap.Get(cur);
                    }
                    prop = temp;
                    last = idx + 1;
                } while (idx >= 0);
                if (_propertyMaps.TryGetValue(p, out var propertyMap) && processedMaps.Add(p))
                {
                    propertyMap.Deserialize(data[p], cur);
                }
            }
            return result;
        }
    }
}
