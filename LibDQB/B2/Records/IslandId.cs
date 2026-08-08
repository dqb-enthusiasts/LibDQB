using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.B2.Records;

public readonly record struct IslandId
{
    public byte Value { get; }

    public IslandId(byte value)
    {
        Value = value;
    }

    public static IslandId IoA => new(0);
    public static IslandId Furrowfield => new(1);
    public static IslandId KhrumbulDun => new(2);
    public static IslandId Moonbrooke => new(3);
    public static IslandId Malhalla => new(4);
    public static IslandId AnglersIsle => new(7);
    public static IslandId Skelkatraz => new(8);
    public static IslandId Buildertopia1 => new(10);
    public static IslandId Buildertopia2 => new(11);
    public static IslandId Buildertopia3 => new(13);
}
