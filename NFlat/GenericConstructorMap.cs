using System;
using System.Collections.Generic;
using System.Text;

namespace NFlat
{
    public class GenericConstructorMap<T, K> : IConstructorMap
    {
        private readonly Func<K> _constructor;
        private readonly Func<T, K, T> _setter;

        public GenericConstructorMap(Func<K> constructor, Func<T, K, T> setter)
        {
            _constructor = constructor;
            _setter = setter;
        }

        public Type Type => typeof(T);

        public object Construct()
        {
            return _constructor();
        }

        public object Set(object @object, object value)
        {
            return _setter((T)@object, (K)value);
        }
    }
}
