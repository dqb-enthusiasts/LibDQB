using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB;

/// <summary>
/// An XZ most commonly represents a location in 2D blockspace where
/// larger values of X mean "more East" and larger values of Z mean "more South".
/// (You would need to add a Y coordinate, which specifies elevation,
///  to identify a point in 3D blockspace.)
///
/// However, we can also use XZ in a more general sense for any 2D array (or "grid").
/// This uniformity allows us to reuse operations on an <see cref="IReadOnlyGrid{T}"/>,
/// and to more conveniently map from one domain to another.
/// </summary>
public record struct XZ(int X, int Z) : IComparable<XZ>
{
    public static XZ Zero => new XZ(0, 0);

    public XZ Add(int dx, int dz) => new XZ(X + dx, Z + dz);

    public XZ Add(XZ xz) => Add(xz.X, xz.Z);

    public XZ Subtract(XZ xz) => new XZ(X - xz.X, Z - xz.Z);

    public XZ Scale(int factor) => new XZ(X * factor, Z * factor);

    public XZ Scale(XZ scale) => new XZ(X * scale.X, Z * scale.Z);

    public XZ Unscale(XZ scale) => new XZ(X / scale.X, Z / scale.Z);

    public XZ Unscale(int factor) => Unscale(new XZ(factor, factor));

    public IEnumerable<XZ> CardinalNeighbors()
    {
        yield return Add(1, 0);
        yield return Add(-1, 0);
        yield return Add(0, 1);
        yield return Add(0, -1);
    }

    public IEnumerable<XZ> OrdinalNeighbors()
    {
        yield return Add(-1, -1);
        yield return Add(1, -1);
        yield return Add(-1, 1);
        yield return Add(1, 1);
    }

    public IEnumerable<XZ> AllNeighbors() => CardinalNeighbors().Concat(OrdinalNeighbors());

    public int CompareTo(XZ other)
    {
        int zComp = this.Z.CompareTo(other.Z);
        if (zComp == 0)
        {
            return this.X.CompareTo(other.X);
        }
        return zComp;
    }
}
