using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB;

/// <summary>
/// Start is inclusive, End is exclusive.
/// (Similar to an array whose first element is at 0 and final element is at Size-1)
/// </summary>
public sealed record Rect(XZ Start, XZ End)
{
    public XZ Size => new XZ(End.X - Start.X, End.Z - Start.Z);

    public bool Contains(XZ xz) => GetIndex(xz).HasValue;

    /// <summary>
    /// If the given <paramref name="xz"/> is within this box, returns a unique index
    /// for that xz in the inclusive range 0 .. (Width*Height - 1).
    /// </summary>
    public int? GetIndex(XZ xz)
    {
        if (xz.X >= End.X || xz.Z >= End.Z)
        {
            return null;
        }

        int zIndex = xz.Z - Start.Z;
        if (zIndex < 0)
        {
            return null;
        }

        int xIndex = xz.X - Start.X;
        if (xIndex < 0)
        {
            return null;
        }

        int width = End.X - Start.X;
        return zIndex * width + xIndex;
    }

    public IEnumerable<XZ> Enumerate()
    {
        for (int z = Start.Z; z < End.Z; z++)
        {
            for (int x = Start.X; x < End.X; x++)
            {
                yield return new XZ(x, z);
            }
        }
    }

    public static Rect GetBounds(IEnumerable<XZ> xzs)
    {
        using var enumerator = xzs.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            throw new ArgumentException("Sequence contains no elements");
        }

        int xMin = enumerator.Current.X;
        int xMax = enumerator.Current.X;

        int zMin = enumerator.Current.Z;
        int zMax = enumerator.Current.Z;

        while (enumerator.MoveNext())
        {
            var xz = enumerator.Current;
            xMin = Math.Min(xMin, xz.X);
            zMin = Math.Min(zMin, xz.Z);
            xMax = Math.Max(xMax, xz.X);
            zMax = Math.Max(zMax, xz.Z);
        }

        return new Rect(new XZ(xMin, zMin), new XZ(xMax + 1, zMax + 1));
    }
}
