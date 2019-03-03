using System;
using System.Collections.Generic;
using System.Text;

namespace NFlat
{
    public abstract class BasePropertyMap<T, K> : IPropertyMap<string>
    {
        private readonly Func<T, K, T> _propertySetter;

        protected BasePropertyMap(Func<T, K, T> propertySetter)
        {
            _propertySetter = propertySetter;
        }

        protected abstract K Parse(string rawValue);

        public object Deserialize(string rawValue, object @object)
        {
            return _propertySetter((T)@object, Parse(rawValue));
        }
    }
}
