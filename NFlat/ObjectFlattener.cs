﻿using Microsoft.Extensions.Primitives;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace NFlat
{
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
            var stackOfSets = new Stack<(object @objectToSetOn, IConstructorMap map, int? index)>();
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
                            cur = constructorPropertyMap.Construct(cur);
                        }
                        var propAsBytes = MemoryMarshal.AsBytes(prop.AsSpan());
                        if (!Utf8Parser.TryParse(propAsBytes, out int propIndex, out var _))
                        {
                            stackOfSets.Push((cur, constructorPropertyMap, null));
                            cur = constructorPropertyMap.Get(cur, null);
                        }
                        else
                        {
                            var tempAsBytes = MemoryMarshal.AsBytes(temp.AsSpan());
                            if (!Utf8Parser.TryParse(tempAsBytes, out int _, out var _))
                            {
                                stackOfSets.Push((cur, constructorPropertyMap, propIndex));
                                cur = constructorPropertyMap.Get(cur, propIndex);
                            }
                        }
                    }
                    prop = temp;
                    last = idx + 1;
                } while (idx >= 0);
                if (_propertyMaps.TryGetValue(p, out var propertyMap))
                {
                    cur = propertyMap.Deserialize(data[p], cur);
                }
                while(stackOfSets.Count > 0)
                {
                    var (objectToSetOn, map, index) = stackOfSets.Pop();
                    cur = map.Set(objectToSetOn, cur, index);
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
