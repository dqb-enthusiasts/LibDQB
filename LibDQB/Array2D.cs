using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB;

public sealed class Array2D<T> : IGrid<T>
{
    public Rect Bounds { get; }
    private readonly T[] array;

    /// <summary>
    /// WARNING! This constructor is not null-safe, so it must remain private.
    /// (When T a reference type the array will hold null, which might violate
    ///  the nullability of type T.)
    /// </summary>
    private Array2D(Rect bounds)
    {
        this.Bounds = bounds;
        array = new T[bounds.Size.X * bounds.Size.Z];
    }

    public Array2D(Rect bounds, T initialValue) : this(bounds)
    {
        array.AsSpan().Fill(initialValue);
    }

    public static Array2D<T> CopyFrom(IReadOnlyGrid<T> grid)
    {
        // It's okay to use the "unsafe" constructor here since we are copying
        // from a grid which we assume is valid.
        var array = new Array2D<T>(grid.Bounds);
        foreach (var xz in grid.Bounds.Enumerate())
        {
            array.Set(xz, grid.Get(xz));
        }
        return array;
    }

    public void Set(XZ xz, T value)
    {
        int idx = Bounds.GetIndex(xz) ?? throw new ArgumentOutOfRangeException(nameof(xz));
        array[idx] = value;
    }

    public T Get(XZ xz)
    {
        int idx = Bounds.GetIndex(xz) ?? throw new ArgumentOutOfRangeException(nameof(xz));
        return array[idx];
    }
}
