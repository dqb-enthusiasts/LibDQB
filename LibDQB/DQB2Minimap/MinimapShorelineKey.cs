using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.DQB2Minimap;

/// <summary>
/// The minimap uses special sea tiles wherever the sea meets the land.
/// More precisely, the correct sea tile to use depends on
/// whether each of its 8 neighboring tiles is or is not land.
/// This struct encodes the status of a tile's 8 neighbors into an 8-bit number
/// which can be used in a lookup table to get the desired tile.
/// </summary>
/// <remarks>
/// This is called "autotiling" and when diagonals are conditionally ignored as per
/// <see cref="MakeCanonical"/>, it is well-known that there are 47 unique tiles.
/// For example see https://github.com/Game-Development-Resources/Autotile-47
/// </remarks>
public readonly record struct MinimapShorelineKey
{
    const byte N = 1;
    const byte E = 2;
    const byte S = 4;
    const byte W = 8;
    const byte NW = 16;
    const byte NE = 32;
    const byte SE = 64;
    const byte SW = 128;

    public byte Value { get; private init; }

    public MinimapShorelineKey(byte value)
    {
        this.Value = value;
    }

    public static MinimapShorelineKey NoShoreline => new(0);

    public static MinimapShorelineKey Compute(XZ xz, IReadOnlyGrid<MinimapTile> grid)
    {
        bool IsLand(int dx, int dz)
        {
            var neighbor = xz.Add(dx, dz);
            return grid.Bounds.Contains(neighbor)
                && grid.Get(neighbor).SeaType == SeaType.Land;
        }

        byte key = 0;

        if (IsLand(0, -1)) key |= N;
        if (IsLand(1, 0)) key |= E;
        if (IsLand(0, 1)) key |= S;
        if (IsLand(-1, 0)) key |= W;

        if (IsLand(-1, -1)) key |= NW;
        if (IsLand(1, -1)) key |= NE;
        if (IsLand(1, 1)) key |= SE;
        if (IsLand(-1, 1)) key |= SW;

        return new MinimapShorelineKey(key);
    }

    /// <summary>
    /// The ordinal neighbors (NW,NE,SE,SW) are irrelevant if either of their component
    /// cardinal neighbors (N,E,S,W) are set.
    /// For example, if either N or E (or both) is land then it doesn't matter
    /// whether NE is land, so the canonical form clears the NE bit.
    /// </summary>
    public MinimapShorelineKey MakeCanonical()
    {
        // The ordinal bits are irrelevant if either of their neighboring cardinal bits are set.
        // For example, NE is irrelevant if either N or E is set.
        static void MaybeUnsetOrdinal(ref byte val, byte cardinal1, byte cardinal2, byte ordinalMask)
        {
            if ((val & cardinal1) != 0 || (val & cardinal2) != 0)
            {
                val &= ordinalMask;
            }
        }

        byte val = this.Value;
        MaybeUnsetOrdinal(ref val, N, W, unchecked((byte)~NW));
        MaybeUnsetOrdinal(ref val, N, E, unchecked((byte)~NE));
        MaybeUnsetOrdinal(ref val, S, E, unchecked((byte)~SE));
        MaybeUnsetOrdinal(ref val, S, W, unchecked((byte)~SW));
        return new MinimapShorelineKey(val);
    }

    /// <summary>
    /// This is used for empty areas (no bedrock, no chunk) and for areas covered with
    /// the special seawater block (BlockId=420). This special block seems to be the
    /// only block that causes the game to render deep sea naturally.
    /// </summary>
    public BaseTileId DeepSeaBaseTileId => new(_DeepSeaBaseTileId);

    private int _DeepSeaBaseTileId
    {
        get
        {
            // This lookup table was extracted from a minimap which contained all 47 tiles.
            // Unfortunately there is no alternate bit assignment that will make this any cleaner,
            // nor allow us to implement it as a calculation rather than a lookup table.
            switch (this.Value)
            {
                case 0: return 0;
                case 1: return 26;
                case 2: return 27;
                case 3: return 30;
                case 4: return 28;
                case 5: return 34;
                case 6: return 31;
                case 7: return 36;
                case 8: return 29;
                case 9: return 33;
                case 10: return 35;
                case 11: return 39;
                case 12: return 32;
                case 13: return 38;
                case 14: return 37;
                case 15: return 40;
                case 16: return 71;
                case 17: return 26;
                case 18: return 131;
                case 19: return 30;
                case 20: return 146;
                case 21: return 34;
                case 22: return 191;
                case 23: return 36;
                case 24: return 29;
                case 25: return 33;
                case 26: return 35;
                case 27: return 39;
                case 28: return 32;
                case 29: return 38;
                case 30: return 37;
                case 31: return 40;
                case 32: return 72;
                case 33: return 26;
                case 34: return 27;
                case 35: return 30;
                case 36: return 147;
                case 37: return 34;
                case 38: return 31;
                case 39: return 36;
                case 40: return 162;
                case 41: return 33;
                case 42: return 35;
                case 43: return 39;
                case 44: return 207;
                case 45: return 38;
                case 46: return 37;
                case 47: return 40;
                case 48: return 75;
                case 49: return 26;
                case 50: return 131;
                case 51: return 30;
                case 52: return 150;
                case 53: return 34;
                case 54: return 191;
                case 55: return 36;
                case 56: return 162;
                case 57: return 33;
                case 58: return 35;
                case 59: return 39;
                case 60: return 207;
                case 61: return 38;
                case 62: return 37;
                case 63: return 40;
                case 64: return 73;
                case 65: return 118;
                case 66: return 27;
                case 67: return 30;
                case 68: return 28;
                case 69: return 34;
                case 70: return 31;
                case 71: return 36;
                case 72: return 163;
                case 73: return 223;
                case 74: return 35;
                case 75: return 39;
                case 76: return 32;
                case 77: return 38;
                case 78: return 37;
                case 79: return 40;
                case 80: return 79;
                case 81: return 118;
                case 82: return 131;
                case 83: return 30;
                case 84: return 146;
                case 85: return 34;
                case 86: return 191;
                case 87: return 36;
                case 88: return 163;
                case 89: return 223;
                case 90: return 35;
                case 91: return 39;
                case 92: return 32;
                case 93: return 38;
                case 94: return 37;
                case 95: return 40;
                case 96: return 76;
                case 97: return 118;
                case 98: return 27;
                case 99: return 30;
                case 100: return 147;
                case 101: return 34;
                case 102: return 31;
                case 103: return 36;
                case 104: return 166;
                case 105: return 223;
                case 106: return 35;
                case 107: return 39;
                case 108: return 207;
                case 109: return 38;
                case 110: return 37;
                case 111: return 40;
                case 112: return 81;
                case 113: return 118;
                case 114: return 131;
                case 115: return 30;
                case 116: return 150;
                case 117: return 34;
                case 118: return 191;
                case 119: return 36;
                case 120: return 166;
                case 121: return 223;
                case 122: return 35;
                case 123: return 39;
                case 124: return 207;
                case 125: return 38;
                case 126: return 37;
                case 127: return 40;
                case 128: return 74;
                case 129: return 119;
                case 130: return 134;
                case 131: return 179;
                case 132: return 28;
                case 133: return 34;
                case 134: return 31;
                case 135: return 36;
                case 136: return 29;
                case 137: return 33;
                case 138: return 35;
                case 139: return 39;
                case 140: return 32;
                case 141: return 38;
                case 142: return 37;
                case 143: return 40;
                case 144: return 78;
                case 145: return 119;
                case 146: return 138;
                case 147: return 179;
                case 148: return 146;
                case 149: return 34;
                case 150: return 191;
                case 151: return 36;
                case 152: return 29;
                case 153: return 33;
                case 154: return 35;
                case 155: return 39;
                case 156: return 32;
                case 157: return 38;
                case 158: return 37;
                case 159: return 40;
                case 160: return 80;
                case 161: return 119;
                case 162: return 134;
                case 163: return 179;
                case 164: return 147;
                case 165: return 34;
                case 166: return 31;
                case 167: return 36;
                case 168: return 162;
                case 169: return 33;
                case 170: return 35;
                case 171: return 39;
                case 172: return 207;
                case 173: return 38;
                case 174: return 37;
                case 175: return 40;
                case 176: return 84;
                case 177: return 119;
                case 178: return 138;
                case 179: return 179;
                case 180: return 150;
                case 181: return 34;
                case 182: return 191;
                case 183: return 36;
                case 184: return 162;
                case 185: return 33;
                case 186: return 35;
                case 187: return 39;
                case 188: return 207;
                case 189: return 38;
                case 190: return 37;
                case 191: return 40;
                case 192: return 77;
                case 193: return 122;
                case 194: return 134;
                case 195: return 179;
                case 196: return 28;
                case 197: return 34;
                case 198: return 31;
                case 199: return 36;
                case 200: return 163;
                case 201: return 223;
                case 202: return 35;
                case 203: return 39;
                case 204: return 32;
                case 205: return 38;
                case 206: return 37;
                case 207: return 40;
                case 208: return 83;
                case 209: return 122;
                case 210: return 138;
                case 211: return 179;
                case 212: return 146;
                case 213: return 34;
                case 214: return 191;
                case 215: return 36;
                case 216: return 163;
                case 217: return 223;
                case 218: return 35;
                case 219: return 39;
                case 220: return 32;
                case 221: return 38;
                case 222: return 37;
                case 223: return 40;
                case 224: return 82;
                case 225: return 122;
                case 226: return 134;
                case 227: return 179;
                case 228: return 147;
                case 229: return 34;
                case 230: return 31;
                case 231: return 36;
                case 232: return 166;
                case 233: return 223;
                case 234: return 35;
                case 235: return 39;
                case 236: return 207;
                case 237: return 38;
                case 238: return 37;
                case 239: return 40;
                case 240: return 85;
                case 241: return 122;
                case 242: return 138;
                case 243: return 179;
                case 244: return 150;
                case 245: return 34;
                case 246: return 191;
                case 247: return 36;
                case 248: return 166;
                case 249: return 223;
                case 250: return 35;
                case 251: return 39;
                case 252: return 207;
                case 253: return 38;
                case 254: return 37;
                case 255: return 40;
            }
        }
    }

    /// <summary>
    /// This is used for areas covered with normal seawater, most commonly BlockId=349.
    /// </summary>
    public BaseTileId ShallowSeaBaseTileId => new(_ShallowSeaBaseTileId);

    private int _ShallowSeaBaseTileId
    {
        get
        {
            // Generated the same way as the Deep lookup was:
            switch (this.Value)
            {
                case 0: return 1;
                case 1: return 41;
                case 2: return 42;
                case 3: return 45;
                case 4: return 43;
                case 5: return 49;
                case 6: return 46;
                case 7: return 51;
                case 8: return 44;
                case 9: return 48;
                case 10: return 50;
                case 11: return 54;
                case 12: return 47;
                case 13: return 53;
                case 14: return 52;
                case 15: return 55;
                case 16: return 86;
                case 17: return 41;
                case 18: return 356;
                case 19: return 45;
                case 20: return 371;
                case 21: return 49;
                case 22: return 416;
                case 23: return 51;
                case 24: return 44;
                case 25: return 48;
                case 26: return 50;
                case 27: return 54;
                case 28: return 47;
                case 29: return 53;
                case 30: return 52;
                case 31: return 55;
                case 32: return 87;
                case 33: return 41;
                case 34: return 42;
                case 35: return 45;
                case 36: return 372;
                case 37: return 49;
                case 38: return 46;
                case 39: return 51;
                case 40: return 387;
                case 41: return 48;
                case 42: return 50;
                case 43: return 54;
                case 44: return 432;
                case 45: return 53;
                case 46: return 52;
                case 47: return 55;
                case 48: return 90;
                case 49: return 41;
                case 50: return 356;
                case 51: return 45;
                case 52: return 375;
                case 53: return 49;
                case 54: return 416;
                case 55: return 51;
                case 56: return 387;
                case 57: return 48;
                case 58: return 50;
                case 59: return 54;
                case 60: return 432;
                case 61: return 53;
                case 62: return 52;
                case 63: return 55;
                case 64: return 88;
                case 65: return 343;
                case 66: return 42;
                case 67: return 45;
                case 68: return 43;
                case 69: return 49;
                case 70: return 46;
                case 71: return 51;
                case 72: return 388;
                case 73: return 448;
                case 74: return 50;
                case 75: return 54;
                case 76: return 47;
                case 77: return 53;
                case 78: return 52;
                case 79: return 55;
                case 80: return 94;
                case 81: return 343;
                case 82: return 356;
                case 83: return 45;
                case 84: return 371;
                case 85: return 49;
                case 86: return 416;
                case 87: return 51;
                case 88: return 388;
                case 89: return 448;
                case 90: return 50;
                case 91: return 54;
                case 92: return 47;
                case 93: return 53;
                case 94: return 52;
                case 95: return 55;
                case 96: return 91;
                case 97: return 343;
                case 98: return 42;
                case 99: return 45;
                case 100: return 372;
                case 101: return 49;
                case 102: return 46;
                case 103: return 51;
                case 104: return 391;
                case 105: return 448;
                case 106: return 50;
                case 107: return 54;
                case 108: return 432;
                case 109: return 53;
                case 110: return 52;
                case 111: return 55;
                case 112: return 96;
                case 113: return 343;
                case 114: return 356;
                case 115: return 45;
                case 116: return 375;
                case 117: return 49;
                case 118: return 416;
                case 119: return 51;
                case 120: return 391;
                case 121: return 448;
                case 122: return 50;
                case 123: return 54;
                case 124: return 432;
                case 125: return 53;
                case 126: return 52;
                case 127: return 55;
                case 128: return 89;
                case 129: return 344;
                case 130: return 359;
                case 131: return 404;
                case 132: return 43;
                case 133: return 49;
                case 134: return 46;
                case 135: return 51;
                case 136: return 44;
                case 137: return 48;
                case 138: return 50;
                case 139: return 54;
                case 140: return 47;
                case 141: return 53;
                case 142: return 52;
                case 143: return 55;
                case 144: return 93;
                case 145: return 344;
                case 146: return 363;
                case 147: return 404;
                case 148: return 371;
                case 149: return 49;
                case 150: return 416;
                case 151: return 51;
                case 152: return 44;
                case 153: return 48;
                case 154: return 50;
                case 155: return 54;
                case 156: return 47;
                case 157: return 53;
                case 158: return 52;
                case 159: return 55;
                case 160: return 95;
                case 161: return 344;
                case 162: return 359;
                case 163: return 404;
                case 164: return 372;
                case 165: return 49;
                case 166: return 46;
                case 167: return 51;
                case 168: return 387;
                case 169: return 48;
                case 170: return 50;
                case 171: return 54;
                case 172: return 432;
                case 173: return 53;
                case 174: return 52;
                case 175: return 55;
                case 176: return 99;
                case 177: return 344;
                case 178: return 363;
                case 179: return 404;
                case 180: return 375;
                case 181: return 49;
                case 182: return 416;
                case 183: return 51;
                case 184: return 387;
                case 185: return 48;
                case 186: return 50;
                case 187: return 54;
                case 188: return 432;
                case 189: return 53;
                case 190: return 52;
                case 191: return 55;
                case 192: return 92;
                case 193: return 347;
                case 194: return 359;
                case 195: return 404;
                case 196: return 43;
                case 197: return 49;
                case 198: return 46;
                case 199: return 51;
                case 200: return 388;
                case 201: return 448;
                case 202: return 50;
                case 203: return 54;
                case 204: return 47;
                case 205: return 53;
                case 206: return 52;
                case 207: return 55;
                case 208: return 98;
                case 209: return 347;
                case 210: return 363;
                case 211: return 404;
                case 212: return 371;
                case 213: return 49;
                case 214: return 416;
                case 215: return 51;
                case 216: return 388;
                case 217: return 448;
                case 218: return 50;
                case 219: return 54;
                case 220: return 47;
                case 221: return 53;
                case 222: return 52;
                case 223: return 55;
                case 224: return 97;
                case 225: return 347;
                case 226: return 359;
                case 227: return 404;
                case 228: return 372;
                case 229: return 49;
                case 230: return 46;
                case 231: return 51;
                case 232: return 391;
                case 233: return 448;
                case 234: return 50;
                case 235: return 54;
                case 236: return 432;
                case 237: return 53;
                case 238: return 52;
                case 239: return 55;
                case 240: return 100;
                case 241: return 347;
                case 242: return 363;
                case 243: return 404;
                case 244: return 375;
                case 245: return 49;
                case 246: return 416;
                case 247: return 51;
                case 248: return 391;
                case 249: return 448;
                case 250: return 50;
                case 251: return 54;
                case 252: return 432;
                case 253: return 53;
                case 254: return 52;
                case 255: return 55;
            }
        }
    }

    /// <summary>
    /// Clear water uses different base tile IDs than <see cref="ShallowSeaBaseTileId"/>,
    /// but DQB2 reuses the same graphics for both.
    /// </summary>
    /// <remarks>
    /// This could allow us to use a tilesheet that displays clear water differently than
    /// the shallow sea (whether in a tool or in a modded DQB2).
    /// </remarks>
    public BaseTileId ClearWaterBaseTileId => new(_ClearWaterBaseTileId);

    private int _ClearWaterBaseTileId
    {
        get
        {
            // Generated the same way as the Deep lookup was:
            switch (this.Value)
            {
                case 0: return 2;
                case 1: return 56;
                case 2: return 57;
                case 3: return 60;
                case 4: return 58;
                case 5: return 64;
                case 6: return 61;
                case 7: return 66;
                case 8: return 59;
                case 9: return 63;
                case 10: return 65;
                case 11: return 69;
                case 12: return 62;
                case 13: return 68;
                case 14: return 67;
                case 15: return 70;
                case 16: return 101;
                case 17: return 56;
                case 18: return 581;
                case 19: return 60;
                case 20: return 596;
                case 21: return 64;
                case 22: return 641;
                case 23: return 66;
                case 24: return 59;
                case 25: return 63;
                case 26: return 65;
                case 27: return 69;
                case 28: return 62;
                case 29: return 68;
                case 30: return 67;
                case 31: return 70;
                case 32: return 102;
                case 33: return 56;
                case 34: return 57;
                case 35: return 60;
                case 36: return 597;
                case 37: return 64;
                case 38: return 61;
                case 39: return 66;
                case 40: return 612;
                case 41: return 63;
                case 42: return 65;
                case 43: return 69;
                case 44: return 657;
                case 45: return 68;
                case 46: return 67;
                case 47: return 70;
                case 48: return 105;
                case 49: return 56;
                case 50: return 581;
                case 51: return 60;
                case 52: return 600;
                case 53: return 64;
                case 54: return 641;
                case 55: return 66;
                case 56: return 612;
                case 57: return 63;
                case 58: return 65;
                case 59: return 69;
                case 60: return 657;
                case 61: return 68;
                case 62: return 67;
                case 63: return 70;
                case 64: return 103;
                case 65: return 568;
                case 66: return 57;
                case 67: return 60;
                case 68: return 58;
                case 69: return 64;
                case 70: return 61;
                case 71: return 66;
                case 72: return 613;
                case 73: return 673;
                case 74: return 65;
                case 75: return 69;
                case 76: return 62;
                case 77: return 68;
                case 78: return 67;
                case 79: return 70;
                case 80: return 109;
                case 81: return 568;
                case 82: return 581;
                case 83: return 60;
                case 84: return 596;
                case 85: return 64;
                case 86: return 641;
                case 87: return 66;
                case 88: return 613;
                case 89: return 673;
                case 90: return 65;
                case 91: return 69;
                case 92: return 62;
                case 93: return 68;
                case 94: return 67;
                case 95: return 70;
                case 96: return 106;
                case 97: return 568;
                case 98: return 57;
                case 99: return 60;
                case 100: return 597;
                case 101: return 64;
                case 102: return 61;
                case 103: return 66;
                case 104: return 616;
                case 105: return 673;
                case 106: return 65;
                case 107: return 69;
                case 108: return 657;
                case 109: return 68;
                case 110: return 67;
                case 111: return 70;
                case 112: return 111;
                case 113: return 568;
                case 114: return 581;
                case 115: return 60;
                case 116: return 600;
                case 117: return 64;
                case 118: return 641;
                case 119: return 66;
                case 120: return 616;
                case 121: return 673;
                case 122: return 65;
                case 123: return 69;
                case 124: return 657;
                case 125: return 68;
                case 126: return 67;
                case 127: return 70;
                case 128: return 104;
                case 129: return 569;
                case 130: return 584;
                case 131: return 629;
                case 132: return 58;
                case 133: return 64;
                case 134: return 61;
                case 135: return 66;
                case 136: return 59;
                case 137: return 63;
                case 138: return 65;
                case 139: return 69;
                case 140: return 62;
                case 141: return 68;
                case 142: return 67;
                case 143: return 70;
                case 144: return 108;
                case 145: return 569;
                case 146: return 588;
                case 147: return 629;
                case 148: return 596;
                case 149: return 64;
                case 150: return 641;
                case 151: return 66;
                case 152: return 59;
                case 153: return 63;
                case 154: return 65;
                case 155: return 69;
                case 156: return 62;
                case 157: return 68;
                case 158: return 67;
                case 159: return 70;
                case 160: return 110;
                case 161: return 569;
                case 162: return 584;
                case 163: return 629;
                case 164: return 597;
                case 165: return 64;
                case 166: return 61;
                case 167: return 66;
                case 168: return 612;
                case 169: return 63;
                case 170: return 65;
                case 171: return 69;
                case 172: return 657;
                case 173: return 68;
                case 174: return 67;
                case 175: return 70;
                case 176: return 114;
                case 177: return 569;
                case 178: return 588;
                case 179: return 629;
                case 180: return 600;
                case 181: return 64;
                case 182: return 641;
                case 183: return 66;
                case 184: return 612;
                case 185: return 63;
                case 186: return 65;
                case 187: return 69;
                case 188: return 657;
                case 189: return 68;
                case 190: return 67;
                case 191: return 70;
                case 192: return 107;
                case 193: return 572;
                case 194: return 584;
                case 195: return 629;
                case 196: return 58;
                case 197: return 64;
                case 198: return 61;
                case 199: return 66;
                case 200: return 613;
                case 201: return 673;
                case 202: return 65;
                case 203: return 69;
                case 204: return 62;
                case 205: return 68;
                case 206: return 67;
                case 207: return 70;
                case 208: return 113;
                case 209: return 572;
                case 210: return 588;
                case 211: return 629;
                case 212: return 596;
                case 213: return 64;
                case 214: return 641;
                case 215: return 66;
                case 216: return 613;
                case 217: return 673;
                case 218: return 65;
                case 219: return 69;
                case 220: return 62;
                case 221: return 68;
                case 222: return 67;
                case 223: return 70;
                case 224: return 112;
                case 225: return 572;
                case 226: return 584;
                case 227: return 629;
                case 228: return 597;
                case 229: return 64;
                case 230: return 61;
                case 231: return 66;
                case 232: return 616;
                case 233: return 673;
                case 234: return 65;
                case 235: return 69;
                case 236: return 657;
                case 237: return 68;
                case 238: return 67;
                case 239: return 70;
                case 240: return 115;
                case 241: return 572;
                case 242: return 588;
                case 243: return 629;
                case 244: return 600;
                case 245: return 64;
                case 246: return 641;
                case 247: return 66;
                case 248: return 616;
                case 249: return 673;
                case 250: return 65;
                case 251: return 69;
                case 252: return 657;
                case 253: return 68;
                case 254: return 67;
                case 255: return 70;
            }
        }
    }
}
