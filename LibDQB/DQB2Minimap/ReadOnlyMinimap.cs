using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.DQB2Minimap;

class ReadOnlyMinimap : IReadOnlyMinimap
{
    /// <summary>
    /// Should be sliced to contain only the <see cref="TileDataLength"/> range of bytes
    /// for the desired map.
    /// </summary>
    private readonly ReadOnlyMemory<byte> data;

    public ReadOnlyMinimap(ReadOnlyMemory<byte> data)
    {
        if (data.Length != TileDataLength)
        {
            throw new ArgumentException($"Wrong size, got {data.Length}, expected {TileDataLength}");
        }
        this.data = data;
    }

    const int BytesPerTile = 2;
    const int MapDimension = 256;
    internal const int TileDataLength = MapDimension * MapDimension * BytesPerTile;
    const int OutroLength = 4;
    internal const int IslandDataLength = TileDataLength + OutroLength;

    public Rect Bounds => new Rect(XZ.Zero, new XZ(MapDimension, MapDimension));

    protected int GetIndex(XZ xz)
    {
        if (!Bounds.Contains(xz))
        {
            throw new ArgumentOutOfRangeException(nameof(xz));
        }
        int index = xz.Z * MapDimension + xz.X;
        index *= BytesPerTile;
        return index;
    }

    public MinimapTile Get(XZ xz)
    {
        int index = GetIndex(xz);
        byte byte1 = data.Span[index];
        byte byte2 = data.Span[index + 1];
        var tile = new MinimapTile
        {
            TileValue = byte1 | (byte2 << 8)
        };
        return tile;
    }
}
