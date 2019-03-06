using System;
using System.Collections.Generic;
using System.Text;

namespace NFlat
{
    public interface IConstructorMap
    {
        Type Type { get; }

        object Construct();

        object Set(object @object, object value);
    }
}
