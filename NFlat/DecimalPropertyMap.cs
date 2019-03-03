using System;
using System.Collections.Generic;
using System.Text;

namespace NFlat
{

    public class DecimalPropertyMap<T> : BasePropertyMap<T, decimal>
    {
        public DecimalPropertyMap(Func<T, decimal, T> propertySetter) : base(propertySetter)
        {
        }

        protected override decimal Parse(string rawValue)
        {
            return decimal.Parse(rawValue);
        }
    }
}
