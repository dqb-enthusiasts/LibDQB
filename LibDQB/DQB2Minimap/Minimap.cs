using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.DQB2Minimap;

sealed class Minimap : ReadOnlyMinimap, IMinimap
{
    private readonly Memory<byte> data;

    public Minimap(Memory<byte> data) : base(data)
    {
        this.data = data;
    }

    public void Set(XZ xz, MinimapTile value)
    {
        int index = GetIndex(xz);
        byte byte1 = (byte)(value.TileValue & 0xFF);
        byte byte2 = (byte)((value.TileValue >> 8) & 0xFF);
        data.Span[index] = byte1;
        data.Span[index + 1] = byte2;
    }
}
