using System;
using System.Collections.Generic;
using System.Text;

namespace NFlat
{
    public interface IPropertyMap<TRawValue>
    {
        Type Type { get; }

        object Deserialize(TRawValue rawValue, object @object);
    }
}
