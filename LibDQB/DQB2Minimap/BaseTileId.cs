using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.DQB2Minimap;

public readonly record struct BaseTileId : IComparable<BaseTileId>, IEquatable<BaseTileId>
{
    /// <summary>
    /// The max possible <see cref="Value"/> is 1489 (which is 0x3FFF / 0xB).
    /// </summary>
    public const int MaxValue = 0x3FFF / 11;

    /// <summary>
    /// Guaranteed to be in the inclusive range [0, <see cref="MaxValue"/>].
    /// </summary>
    public readonly int Value;

    public BaseTileId(int value)
    {
        this.Value = Math.Clamp(value & 0x3FFF, 0, MaxValue);
    }

    public BaseTileId(MinimapTile tile)
    {
        this.Value = (tile.TileValue - 1 & 0x3FFF) / 11;
    }

    public static implicit operator int(BaseTileId a) => a.Value;
    public int CompareTo(BaseTileId other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();

    public bool IsLegal => Value <= MinimapTile.MaxLegalBaseTileId;
}
