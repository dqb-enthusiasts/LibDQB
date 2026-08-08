using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.DQB2Minimap;

/// <summary>
/// See <see cref="MinimapTile.ApparentOverlayId"/>
/// and <see cref="MinimapTile.FormulaicOverlayId"/>.
/// </summary>
public readonly record struct OverlayId : IComparable<OverlayId>
{
    /// <summary>
    /// The max possible <see cref="Value"/> is 10.
    /// </summary>
    public const int MaxValue = 10;

    /// <summary>
    /// Guaranteed to be in the inclusive range [0, <see cref="MaxValue"/>].
    /// </summary>
    public readonly int Value;

    public OverlayId(int value) { this.Value = value % 11; }

    public OverlayId(MinimapTile tile)
    {
        if (tile.BaseTileId.IsLegal)
        {
            Value = (tile.TileValue - 1 & 0x3FFF) % 11;
        }
        else
        {
            // Confirmed that DQB2 never shows overlays if the base tile is illegal.
            Value = 0;
        }
    }

    public static implicit operator int(OverlayId a) => a.Value;
    public int CompareTo(OverlayId other) => this.Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
}
