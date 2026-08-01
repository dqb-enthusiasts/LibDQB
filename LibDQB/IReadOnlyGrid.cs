using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB;

public interface IReadOnlyGrid<out T>
{
    Rect Bounds { get; }
    T Get(XZ xz);

    T this[XZ xz] => Get(xz);
}
