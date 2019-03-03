using System;
using System.Collections.Generic;
using System.Text;

namespace NFlat
{
    public class StringPropertyMap<T> : BasePropertyMap<T, string>
    {
        public StringPropertyMap(Func<T, string, T> propertySetter) : base(propertySetter)
        {
        }

        protected override string Parse(string rawValue)
        {
            return rawValue;
        }
    }
}
