using Microsoft.Extensions.Primitives;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NFlat
{
    public class DictionaryFlattener
    {
        public Dictionary<StringSegment, object> Unflatten(Dictionary<string, string> data, char separator = '_')
        {
            var result = new Dictionary<StringSegment, object>();
            (Dictionary<StringSegment, object> @object, List<object> list) cur = default;
            var idx = 0;
            StringSegment prop = StringSegment.Empty;
            foreach(var p in data.Keys)
            {
                cur.@object = result;
                prop = string.Empty;
                var last = 0;
                do
                {
                    idx = p.IndexOf(separator, last);
                    var temp = idx != -1 ? new StringSegment(p, last, idx - last) : new StringSegment(p, last, p.Length - last);
                    if(cur.@object != null)
                    {
                        var leftSideOfProp = new StringSegment(prop.Buffer, 0, prop.Offset + prop.Length);
                        if(!cur.@object.ContainsKey(prop))
                        {
                            var tempAsBytes = MemoryMarshal.AsBytes(temp.AsSpan());
                            if (Utf8Parser.TryParse(tempAsBytes, out int listIndex, out var _))
                            {
                                cur.@object.Add(prop, new List<object>());
                            }
                            else
                            {
                                cur.@object.Add(prop, new Dictionary<StringSegment, object>());
                            }
                        }
                        if (cur.@object[prop] is Dictionary<StringSegment, object>)
                        {
                            cur.@object = cur.@object[prop] as Dictionary<StringSegment, object>;
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
                        var propAsBytes = MemoryMarshal.AsBytes(prop.AsSpan());
                        var tempAsBytes = MemoryMarshal.AsBytes(temp.AsSpan());
                        Utf8Parser.TryParse(propAsBytes, out int index, out var _);
                        if (Utf8Parser.TryParse(tempAsBytes, out int listIndex, out var _))
                        {
                            cur.list.Add(new List<Dictionary<StringSegment, object>>());
                        }
                        else
                        {
                            cur.list.Add(new Dictionary<StringSegment, object>());
                        }
                        if (cur.list[index] is Dictionary<StringSegment, object>)
                        {
                            cur.@object = cur.list[index] as Dictionary<StringSegment, object>;
                            cur.list = null;
                        }
                        else
                        {
                            cur.@object = null;
                            cur.list = cur.list[index] as List<object>;
                        }
                    }
                    prop = temp;
                    last = idx + 1;
                } while (idx >= 0);
                cur.@object?.Add(prop, data[p]);
                cur.list?.Add(data[p]);
            }
            return result[StringSegment.Empty] as Dictionary<StringSegment, object>;
        }
    }
}
