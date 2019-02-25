using Microsoft.Extensions.Primitives;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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

    public class ObjectFlattener<T, TRawValue> : IEqualityComparer<StringSegment>
        where T : new()
    {
        private readonly Dictionary<StringSegment, IPropertyMap<TRawValue>> _propertyMaps;

        private readonly Dictionary<StringSegment, IConstructorMap> _constructorMaps;

        public ObjectFlattener()
        {
            this._propertyMaps = new Dictionary<StringSegment, IPropertyMap<TRawValue>>(this);
            this._constructorMaps = new Dictionary<StringSegment, IConstructorMap>(this);
        }

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
            foreach (var p in data.Keys.OrderBy(q => q))
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
                        {
                            constructorPropertyMap.Construct(cur);
                        }
                        var propAsBytes = MemoryMarshal.AsBytes(prop.AsSpan());
                        if (!Utf8Parser.TryParse(propAsBytes, out int _, out var _))
                        {
                            cur = constructorPropertyMap.Get(cur);
                        }
                        else
                        {
                            var tempAsBytes = MemoryMarshal.AsBytes(temp.AsSpan());
                            if (!Utf8Parser.TryParse(tempAsBytes, out int _, out var _))
                            {
                                cur = constructorPropertyMap.Get(cur);
                            }
                        }
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

        bool IEqualityComparer<StringSegment>.Equals(StringSegment obj1, StringSegment obj2)
        {
            if (obj1.Length != obj2.Length)
                return false;
            var pointerOf1 = 0;
            var pointerOf2 = 0;
            while (pointerOf1 < obj1.Length && pointerOf2 < obj2.Length)
            {
                var charOfObj1 = obj1[pointerOf1++];
                var charOfObj2 = obj2[pointerOf2++];
                if (char.IsNumber(charOfObj1))
                    charOfObj1 = '*';
                if (char.IsNumber(charOfObj2))
                    charOfObj2 = '*';
                if (!charOfObj1.Equals(charOfObj2))
                    return false;
            }
            return true;
        }

        int IEqualityComparer<StringSegment>.GetHashCode(StringSegment obj)
        {
            if (obj.Length == 0)
                return StringSegment.Empty.GetHashCode();
            var index = 0;
            var charOfObj = obj[index++];
            if (char.IsNumber(charOfObj))
                charOfObj = '*';
            var h = charOfObj.GetHashCode();
            while (index < obj.Length)
            {
                charOfObj = obj[index++];
                if (char.IsNumber(charOfObj))
                    charOfObj = '*';
                h = CombineHashCodes(h, charOfObj.GetHashCode());
            }
            return h;
        }

        //https://github.com/dotnet/corefx/blob/664d98b3dc83a56e1e6454591c585cc6a8e19b78/src/Common/src/CoreLib/System/Tuple.cs
        public virtual int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }
    }
}
