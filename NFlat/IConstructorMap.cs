using System;
using System.Collections.Generic;
using System.Text;

namespace NFlat
{
    public interface IConstructorMap
    {
        Type Type { get; }

        object Construct(object @object);

        object Get(object @object, int? index);

        object Set(object @object, object value, int? index);
    }
}
