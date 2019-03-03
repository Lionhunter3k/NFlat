using System;
using System.Collections.Generic;
using System.Text;

namespace NFlat
{
    public class Int32PropertyMap<T> : BasePropertyMap<T, Int32>
    {
        public Int32PropertyMap(Func<T, int, T> propertySetter) : base(propertySetter)
        {
        }

        protected override int Parse(string rawValue)
        {
            return Int32.Parse(rawValue);
        }
    }
}
