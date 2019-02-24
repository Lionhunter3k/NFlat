using System;
using System.Collections.Generic;

namespace NFlat
{
    public class DictionaryFlattener
    {
        public Dictionary<string, object> Unflatten(Dictionary<string, string> data)
        {
            var result = new Dictionary<string, object>();
            (Dictionary<string, object> @object, List<object> list) cur = default;
            var idx = 0;
            string prop = null;
            string temp = null;
            foreach(var p in data.Keys)
            {
                cur.@object = result;
                prop = string.Empty;
                var last = 0;
                do
                {
                    idx = p.IndexOf("_", last);
                    temp = idx != -1 ? p.Substring(last, idx - last) : p.Substring(last);
                    if(cur.@object != null)
                    {
                        if(!cur.@object.ContainsKey(prop))
                        {
                            if (int.TryParse(temp, out var _))
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
                        if (int.TryParse(temp, out var _))
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
                    prop = temp;
                    last = idx + 1;
                } while (idx >= 0);
                cur.@object?.Add(prop, data[p]);
                cur.list?.Add(data[p]);
            }
            return result[string.Empty] as Dictionary<string, object>;
        }
    }
}
