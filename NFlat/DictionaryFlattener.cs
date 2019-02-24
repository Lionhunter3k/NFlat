using Microsoft.Extensions.Primitives;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NFlat
{
    public interface IConstructor
    {

    }

    public interface IPropertyMap<T>
    {
        void Deserialize(string rawValue, T @object);
    }

    public abstract class GenericPropertyMap<T, K> : IPropertyMap<T>
    {
        private readonly Action<T, K> _propertySetter;

        protected GenericPropertyMap(Action<T, K> propertySetter)
        {
            _propertySetter = propertySetter;
        }

        protected abstract K Parse(string rawValue);

        public void Deserialize(string rawValue, T @object)
        {
            _propertySetter(@object, Parse(rawValue));
        }
    }

    public class Int32GenericPropertyMap<T> : GenericPropertyMap<T, Int32>
    {
        protected Int32GenericPropertyMap(Action<T, int> propertySetter) : base(propertySetter)
        {
        }

        protected override int Parse(string rawValue)
        {
            return Int32.Parse(rawValue);
        }
    }

    public class StringGenericPropertyMap<T> : GenericPropertyMap<T, string>
    {
        protected StringGenericPropertyMap(Action<T, string> propertySetter) : base(propertySetter)
        {
        }

        protected override string Parse(string rawValue)
        {
            return rawValue;
        }
    }

    public class DecimalGenericPropertyMap<T> : GenericPropertyMap<T, decimal>
    {
        protected DecimalGenericPropertyMap(Action<T, decimal> propertySetter) : base(propertySetter)
        {
        }

        protected override decimal Parse(string rawValue)
        {
            return decimal.Parse(rawValue);
        }
    }

    public struct PropertySubstring
    {
        public PropertySubstring(string buffer, int offset, int length) : this()
        {
            Offset = offset;
            Length = length;
            Buffer = buffer;
        }

        public PropertySubstring(string buffer, int offset) : this()
        {
            Offset = offset;
            Length = buffer.Length - offset;
            Buffer = buffer;
        }

        public int Offset { get; }

        public int Length { get; }

        public string Buffer { get; }

        public char this[int index]
        {
            get
            {
                return Buffer[Offset + index];
            }
        }

        public ReadOnlySpan<char> AsSpan() => Buffer.AsSpan(Offset, Length);

        public PropertySubstring GetLeftSide()
        {
            return new PropertySubstring(Buffer, 0, Length);
        }
    }

    public class DictionaryFlattener
    {
        public Dictionary<string, object> Unflatten(Dictionary<string, string> data)
        {
            var result = new Dictionary<string, object>();
            (Dictionary<string, object> @object, List<object> list) cur = default;
            var idx = 0;
            string prop = null;
            foreach(var p in data.Keys)
            {
                cur.@object = result;
                prop = string.Empty;
                var last = 0;
                do
                {
                    idx = p.IndexOf("_", last);
                    var temp = idx != -1 ? new StringSegment(p, last, idx - last) : new StringSegment(p, last, p.Length - last);
                    if(cur.@object != null)
                    {
                        if(!cur.@object.ContainsKey(prop))
                        {
                            var tempAsBytes = MemoryMarshal.AsBytes(temp.AsSpan());
                            if (Utf8Parser.TryParse(tempAsBytes, out int listIndex, out var _))
                            {
                                cur.@object.Add(prop, new List<object>());
                            }
                            else
                            {
                                cur.@object.Add(prop, new Dictionary<string, object>());
                            }
                        }
                        if (cur.@object[prop] is Dictionary<string, object>)
                        {
                            cur.@object = cur.@object[prop] as Dictionary<string, object>;
                            cur.list = null;
                        }
                        else
                        {
                            cur.@object = null;
                            cur.list = cur.@object[prop] as List<object>;
                        }
                    }
                    else
                    {
                        var index = int.Parse(prop);
                        var tempAsBytes = MemoryMarshal.AsBytes(temp.AsSpan());
                        if (Utf8Parser.TryParse(tempAsBytes, out int listIndex, out var _))
                        {
                            cur.list.Add(new List<Dictionary<string, object>>());
                        }
                        else
                        {
                            cur.list.Add(new Dictionary<string, object>());
                        }
                        if (cur.list[index] is Dictionary<string, object>)
                        {
                            cur.@object = cur.list[index] as Dictionary<string, object>;
                            cur.list = null;
                        }
                        else
                        {
                            cur.@object = null;
                            cur.list = cur.list[index] as List<object>;
                        }
                    }
                    prop = temp.Value;
                    last = idx + 1;
                } while (idx >= 0);
                cur.@object?.Add(prop, data[p]);
                cur.list?.Add(data[p]);
            }
            return result[string.Empty] as Dictionary<string, object>;
        }
    }
}
