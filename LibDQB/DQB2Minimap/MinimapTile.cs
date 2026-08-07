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
/// and the <see cref="FormulaicOverlayId"/> which are computed the same regardless
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

    public int TileValue { get; }

    private MinimapTile(int value) { this.TileValue = value; }

    public static MinimapTile FromRawValue(int value) => new(value);

    /// <summary>
    /// Defines which base tile will be rendered.
    /// For example: Earth, Grassy Earth, Sand, and Light Dolomite are all
    /// mapped to different base tiles.
    /// See also <see cref="ApparentOverlayId"/>.
    /// </summary>
    public BaseTileId BaseTileId => new(this);

    /// <summary>
    /// Defines which overlay will be rendered.
    /// There are exactly 11 possible values:
    /// <list type="bullet">
    /// <item><term>0</term><description>No overlay</description></item>
    /// <item><term>1</term><description>Normal trees</description></item>
    /// <item><term>2</term><description>Tower</description></item>
    /// <item><term>3</term><description>Tropical Trees</description></item>
    /// <item><term>4</term><description>unused and unsupported</description></item>
    /// <item><term>5</term><description>unused and unsupported</description></item>
    /// <item><term>6</term><description>Door (used for rooms)</description></item>
    /// <item><term>7</term><description>Brown Foothills</description></item>
    /// <item><term>8</term><description>Gray Mountains</description></item>
    /// <item><term>9</term><description>Red-Brown Mountains</description></item>
    /// <item><term>10</term><description>Gray Foothills</description></item>
    /// </list>
    /// See also <see cref="BaseTileId"/>.
    /// </summary>
    public OverlayId ApparentOverlayId => QuirkyOverlay ?? FormulaicOverlayId;

    public OverlayId FormulaicOverlayId => new(this);

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

    public SeaType SeaType
    {
        get
        {
            if (BaseTileId < seaTypeLookup.Count && BaseTileId >= 0)
            {
                return seaTypeLookup[BaseTileId];
            }
            return SeaType.IllegalTile;
        }
    }

    public bool CanHaveShoreline()
    {
        switch (SeaType)
        {
            case SeaType.DeepSea:
            case SeaType.ShallowSea:
            case SeaType.ClearWater:
                return true;
            default:
                return false;
        }
    }

    public MinimapTile FixupShoreline(MinimapShorelineKey key)
    {
        switch (SeaType)
        {
            case SeaType.DeepSea: return ReplaceWithDeepSea(key);
            case SeaType.ShallowSea: return ReplaceWithShallowSea(key);
            case SeaType.ClearWater: return ReplaceWithClearWater(key);
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
        val += ApparentOverlayId;
        val += 1;
        return new(val);
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
        return new(val);
    }

    public MinimapTile RemoveQuirkiness()
    {
        // Removing quirkiness is easier than adding it.
        // (And in fact, adding quirkiness is a "dangerous" request because it
        //  could change your Apparent Overlay from nothing to something)
        int val = this.TileValue & ~0x7FFF;
        val += BaseTileId * 11;
        val += ApparentOverlayId;
        val += 1;
        return new(val);
    }

    public MinimapTile ReplaceVisibility(bool isVisible)
    {
        int val = this.TileValue;
        if (isVisible)
        {
            val |= VisibleBit;
        }
        else
        {
            val &= ~VisibleBit;
        }
        return new MinimapTile(val);
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
    private OverlayId? QuirkyOverlay
    {
        get
        {
            // Add 1 to skip the "unTile"
            const int Offset = QuirkyBit + 1;

            return (TileValue & 0x7FFF) switch
            {
                // Offset + 11*BaseTileId => OverlayId
                Offset + 11 * 8 => new(8),
                Offset + 11 * 9 => new(10),
                Offset + 11 * 10 => new(9),
                Offset + 11 * 11 => new(7),
                Offset + 11 * 18 => new(8),
                _ => null,
            };
        }
    }

    private static readonly IReadOnlyList<SeaType> seaTypeLookup = BuildSeaTypeLookup();

    private static IReadOnlyList<SeaType> BuildSeaTypeLookup()
    {
        var array = new SeaType[BaseTileId.MaxLegalValue + 1];
        var span = array.AsSpan();
        span.Fill(SeaType.Land);

        // primary IDs - no edges and no corners
        array[0] = SeaType.DeepSea;
        array[1] = SeaType.ShallowSea;
        array[2] = SeaType.ClearWater;

        int i = 0x1A;

        // bank 1 - the 15 edge possiblities (no corners)
        span.Slice(i, 15).Fill(SeaType.DeepSea);
        i += 15;
        span.Slice(i, 15).Fill(SeaType.ShallowSea);
        i += 15;
        span.Slice(i, 15).Fill(SeaType.ClearWater);
        i += 15;

        // bank 2 - the 15 corner possibilities (no edges)
        span.Slice(i, 15).Fill(SeaType.DeepSea);
        i += 15;
        span.Slice(i, 15).Fill(SeaType.ShallowSea);
        i += 15;
        span.Slice(i, 15).Fill(SeaType.ClearWater);
        i += 15;

        // bank 3 - the 225 edge+corner possibilities (the cartesian product of bank 1 and bank 2)
        span.Slice(i, 225).Fill(SeaType.DeepSea);
        i += 225;
        span.Slice(i, 225).Fill(SeaType.ShallowSea);
        i += 225;
        span.Slice(i, 225).Fill(SeaType.ClearWater);
        i += 225;

        return array;
    }
}
