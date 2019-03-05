using System;
using System.Collections.Generic;
using System.Text;

namespace NFlat
{
    public class GenericConstructorMap<T, K> : IConstructorMap
    {
        private readonly Func<T, T> _constructor;
        private readonly Func<T, int?, K> _getter;
        private readonly Func<T, K, int?, T> _setter;

        public GenericConstructorMap(Func<T, T> constructor, Func<T, int?, K> getter, Func<T, K, int?, T> setter)
        {
            _constructor = constructor;
            _getter = getter;
            _setter = setter;
        }

        public GenericConstructorMap(Func<T, T> constructor, Func<T, K> getter, Func<T, K, T> setter)
        {
            _constructor = constructor;
            _getter = (u, i) => getter(u);
            _setter = (u, k, i) => setter(u, k);
        }

        public Type Type => typeof(T);

        public object Construct(object @object)
        {
            return _constructor((T)@object);
        }

        public object Get(object @object, int? index)
        {
            return _getter((T)@object, index);
        }

        public object Set(object @object, object value, int? index)
        {
            return _setter((T)@object, (K)value, index);
        }
    }
}
