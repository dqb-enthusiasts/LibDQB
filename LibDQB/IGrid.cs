using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB;

public interface IGrid<T> : IReadOnlyGrid<T>
{
    void Set(XZ xz, T value);

    new T this[XZ xz]
    {
        get => Get(xz);
        set => Set(xz, value);
    }
}
