using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.DQB2Minimap;

/// <remarks>
/// The original research comes from https://github.com/Sapphire645/DQB2MinimapExporter
///
/// A CMNDAT file holds minimap data for all islands.
/// Each minimap is 256x256 tiles, and each tile is represented
/// by a 2-byte <see cref="TileValue"/>.
/// The ranges for tile values are:
/// * [0x0000..0x3FFF] for hidden tiles, normal
/// * [0x4000..0x7FFF] for hidden tiles, quirky
/// * [0x8000..0xBFFF] for visible tiles, normal
/// * [0xC000..0xFFFF] for visible tiles, quirky
/// These "quirky" ranges are not used by DQB2 in normal operation.
/// More info at <see cref="IsQuirky"/>.
///
/// The <see cref="IsVisible"/> property is simply whether the value
/// has the 0x8000 bit set or not.
///
/// The other two important properties are the <see cref="BaseTileId"/>
/// and the <see cref="OverlayId"/> which are computed the same regardless
/// of visibility and quirkiness.
/// To ignore visibility and quirkiness, define MaskedValue to be (value & 0x3FFF).
/// When MaskedValue is 0, it indicates "no data".
/// Otherwise we can compute
/// * BaseTileId = (MaskedValue - 1) / 11
/// * OverlayId = (MaskedValue - 1) % 11
/// </remarks>
public readonly record struct MinimapTile
{
    const int VisibleBit = 0x8000;
    const int QuirkyBit = 0x4000;

    public required int TileValue { get; init; }

    public BaseTileId BaseTileId => new(this);

    public OverlayId OverlayId => new(this);

    /// <summary>
    /// Indicates whether the tile has been revealed.
    /// In normal play, tiles automatically reveal themselves when
    /// the Builder comes near enough.
    /// </summary>
    public bool IsVisible => (TileValue & VisibleBit) != 0;

    /// <summary>
    /// DQB2 does not use quirky tiles in normal operation, but it does handle
    /// quirky tiles somewhat gracefully. For the most part, quirky tiles behave
    /// the same as normal tiles with these exceptions:
    /// * Deep Sea is rendered as Shallow Sea or Clear Water
    /// * Quirky overlays get rendered, see <see cref="QuirkyOverlay"/>.
    /// </summary>
    public bool IsQuirky => (TileValue & QuirkyBit) != 0;

    public SeaTypeIndex SeaTypeIndex
    {
        get
        {
            if (BaseTileId < seaTypeLookup.Count && BaseTileId >= 0)
            {
                return seaTypeLookup[BaseTileId];
            }
            return SeaTypeIndex.None;
        }
    }

    public bool CanHaveShoreline() => SeaTypeIndex != SeaTypeIndex.None;

    public MinimapTile FixupShoreline(MinimapShorelineKey key)
    {
        switch (SeaTypeIndex)
        {
            case SeaTypeIndex.DeepSea: return ReplaceWithDeepSea(key);
            case SeaTypeIndex.ShallowSea: return ReplaceWithShallowSea(key);
            case SeaTypeIndex.ClearWater: return ReplaceWithClearWater(key);
            default: return this;
        }
    }

    public MinimapTile ReplaceWithDeepSea(MinimapShorelineKey key)
    {
        return ReplaceBaseTile(key.DeepSeaBaseTileId);
    }

    public MinimapTile ReplaceWithShallowSea(MinimapShorelineKey key)
    {
        return ReplaceBaseTile(key.ShallowSeaBaseTileId);
    }

    public MinimapTile ReplaceWithClearWater(MinimapShorelineKey key)
    {
        return ReplaceBaseTile(key.ClearWaterBaseTileId);
    }

    public MinimapTile ReplaceBaseTile(BaseTileId baseTileId)
    {
        int val = this.TileValue & ~0x7FFF;
        val += baseTileId * 11;
        val += OverlayId;
        val += 1;
        return new MinimapTile { TileValue = val };
    }

    public MinimapTile ReplaceOverlay(OverlayId overlayId)
    {
        if (!BaseTileId.IsLegal)
        {
            return this;
        }
        int val = this.TileValue & ~0x7FFF;
        val += this.BaseTileId * 11;
        val += overlayId % 11;
        val += 1;
        return new MinimapTile { TileValue = val };
    }

    public MinimapTile ReplaceVisibility(bool isVisible)
    {
        int val = this.TileValue;

        // The other "Replace***" methods guarantee the result is not quirky.
        // For visibility, we don't really have to clear the quirky bit but
        // doing so is more consistent with those other methods.
        val &= ~QuirkyBit;

        if (isVisible)
        {
            val |= VisibleBit;
        }
        else
        {
            val &= ~VisibleBit;
        }
        return new MinimapTile { TileValue = val };
    }

    /// <summary>
    /// DQB2 draws unexplained overlays in some rare situations.
    /// These situations should never occur during normal operation, but if your goal
    /// is to render every possible minimap exactly as DQB2 would render then this
    /// property is relevant. When the normal overlay is not present, this property
    /// may return the quirky overlay that DQB2 will render instead.
    /// For most tiles, it returns null indicating there is no quirky overlay.
    /// </summary>
    /// <remarks>
    /// See also <see cref="IsQuirky"/>.
    /// </remarks>
    public int? QuirkyOverlay
    {
        get
        {
            // Add 1 to skip the "unTile"
            const int Base = QuirkyBit + 1;

            return (TileValue & 0x7FFF) switch
            {
                // Base + 11*BaseTileId => OverlayId
                Base + 11 * 8 => 8,
                Base + 11 * 9 => 10,
                Base + 11 * 10 => 9,
                Base + 11 * 11 => 7,
                Base + 11 * 18 => 8,
                _ => null,
            };
        }
    }

    private static readonly IReadOnlyList<SeaTypeIndex> seaTypeLookup = BuildSeaTypeLookup();

    private static IReadOnlyList<SeaTypeIndex> BuildSeaTypeLookup()
    {
        var array = new SeaTypeIndex[BaseTileId.MaxLegalValue + 1];
        var span = array.AsSpan();
        span.Fill(SeaTypeIndex.None);

        // primary IDs - no edges and no corners
        array[0] = SeaTypeIndex.DeepSea;
        array[1] = SeaTypeIndex.ShallowSea;
        array[2] = SeaTypeIndex.ClearWater;

        int i = 0x1A;

        // bank 1 - the 15 edge possiblities (no corners)
        span.Slice(i, 15).Fill(SeaTypeIndex.DeepSea);
        i += 15;
        span.Slice(i, 15).Fill(SeaTypeIndex.ShallowSea);
        i += 15;
        span.Slice(i, 15).Fill(SeaTypeIndex.ClearWater);
        i += 15;

        // bank 2 - the 15 corner possibilities (no edges)
        span.Slice(i, 15).Fill(SeaTypeIndex.DeepSea);
        i += 15;
        span.Slice(i, 15).Fill(SeaTypeIndex.ShallowSea);
        i += 15;
        span.Slice(i, 15).Fill(SeaTypeIndex.ClearWater);
        i += 15;

        // bank 3 - the 225 edge+corner possibilities (the cartesian product of bank 1 and bank 2)
        span.Slice(i, 225).Fill(SeaTypeIndex.DeepSea);
        i += 225;
        span.Slice(i, 225).Fill(SeaTypeIndex.ShallowSea);
        i += 225;
        span.Slice(i, 225).Fill(SeaTypeIndex.ClearWater);
        i += 225;

        return array;
    }
}
