using Microsoft.Extensions.Primitives;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace NFlat
{
    public class ObjectFlattener<T, TRawValue> : IEqualityComparer<StringSegment>, IComparer<string>
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
            _splitter = new char[] { separator };
            var result = new T();
            object cur = result;
            var idx = 0;
            var processedMaps = new HashSet<StringSegment>();
            var stackOfSets = new Stack<(object @objectToSetOn, IConstructorMap map, StringSegment propertyPath)>();
            foreach (var p in data.Keys.OrderByDescending(q => q, this))
            {
                var prop = StringSegment.Empty;
                var leftSideOfProp = StringSegment.Empty;
                var prevLeftSideOfProp = StringSegment.Empty;
                var last = 0;
                do
                {
                    idx = p.IndexOf(separator, last);
                    var temp = idx != -1 ? new StringSegment(p, last, idx - last) : new StringSegment(p, last, p.Length - last);
                    prevLeftSideOfProp = leftSideOfProp;
                    leftSideOfProp = new StringSegment(prop.Buffer, 0, prop.Offset + prop.Length);
                    if (_constructorMaps.TryGetValue(leftSideOfProp, out var constructorPropertyMap))
                    {
                        if (processedMaps.Add(leftSideOfProp))
                        {
                            while (stackOfSets.Count > 0 && stackOfSets.Peek().propertyPath != prevLeftSideOfProp)
                            {
                                var (objectToSetOn, map, newLeftSideOfProp) = stackOfSets.Pop();
                                //leftSideOfProp = newLeftSideOfProp;
                                cur = map.Set(objectToSetOn, cur);
                            }
                            stackOfSets.Push((cur, constructorPropertyMap, leftSideOfProp));
                            cur = constructorPropertyMap.Construct();
                        }
                        //var propAsBytes = MemoryMarshal.AsBytes(prop.AsSpan());
                        //if (!Utf8Parser.TryParse(propAsBytes, out int propIndex, out var _))
                        //{
                        //    if (!stackOfSets.Any(q => q.propertyPath == leftSideOfProp))
                        //    {
                        //        stackOfSets.Push((cur, constructorPropertyMap, null, leftSideOfProp));
                        //        cur = constructorPropertyMap.Get(cur, null);
                        //    }
                        //}
                        //else
                        //{
                        //    var tempAsBytes = MemoryMarshal.AsBytes(temp.AsSpan());
                        //    if (!Utf8Parser.TryParse(tempAsBytes, out int _, out var _))
                        //    {
                        //        if (!stackOfSets.Any(q => q.propertyPath == leftSideOfProp))
                        //        {
                        //            stackOfSets.Push((cur, constructorPropertyMap, propIndex, leftSideOfProp));
                        //            cur = constructorPropertyMap.Get(cur, propIndex);
                        //        }

                        //    }
                        //}
                    }
                    prop = temp;
                    last = idx + 1;
                } while (idx >= 0);
                while (stackOfSets.Count > 0 && stackOfSets.Peek().propertyPath != leftSideOfProp)
                {
                    var (objectToSetOn, map, newLeftSideOfProp) = stackOfSets.Pop();
                    //leftSideOfProp = newLeftSideOfProp;
                    cur = map.Set(objectToSetOn, cur);
                }
                if (_propertyMaps.TryGetValue(p, out var propertyMap))
                {
                    cur = propertyMap.Deserialize(data[p], cur);

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

        private char[] _splitter;

        int IComparer<string>.Compare(string x, string y)
        {
            var sx = new StringSegment(x).Split(_splitter);
            var sy = new StringSegment(y).Split(_splitter);
            var itSx = sx.ToArray();
            var itSy = sy.ToArray();
            if (itSx.Length != itSy.Length)
            {
                return itSx.Length.CompareTo(itSy.Length);
            }
            var pointerOf1 = 0;
            var pointerOf2 = 0;
            while (pointerOf1 < itSx.Length && pointerOf2 < itSy.Length)
            {
                var sitSx = itSx[pointerOf1++];
                var sitSy = itSy[pointerOf2++];
                var compResult = 0;
                var sitSxAsBytes = MemoryMarshal.AsBytes(sitSx.AsSpan());
                var sitSyAsBytes = MemoryMarshal.AsBytes(sitSy.AsSpan());
                if (Utf8Parser.TryParse(sitSxAsBytes, out int xIndex, out var _) && Utf8Parser.TryParse(sitSyAsBytes, out int yIndex, out var _))
                {
                    compResult = yIndex.CompareTo(xIndex);
                }
                else
                {
                    compResult = StringSegment.Compare(sitSx, sitSy, StringComparison.Ordinal);
                }
                if (compResult == 0)
                    continue;
                return compResult;
            }
            return 0;
        }

        //int IComparer<string>.Compare(string x, string y)
        //{
        //    var sx = new StringSegment(x).Split(_splitter);
        //    var sy = new StringSegment(y).Split(_splitter);
        //    var itSx = sx.GetEnumerator();
        //    var itSy = sy.GetEnumerator();
        //    var canMoveX = itSx.MoveNext();
        //    var canMoveY = itSy.MoveNext();
        //    while (canMoveX && canMoveY)
        //    {
        //        var sitSx = itSx.Current;
        //        var sitSy = itSy.Current;
        //        var compResult = StringSegment.Compare(sitSx, sitSy, StringComparison.Ordinal);
        //        if (compResult != 0)
        //        {
        //            return compResult;
        //        }
        //        canMoveX = itSx.MoveNext();
        //        canMoveY = itSy.MoveNext();
        //    }
        //    return (!canMoveX).CompareTo((!canMoveY));
        //}
    }
}
