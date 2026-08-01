using LibDQB.B2.Records;
using LibDQB.DQB2Minimap;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.B2;

/// <summary>
/// Provides direct, low-level access to a CMNDAT file.
/// </summary>
public sealed class RawCommonData
{
    private readonly Memory<byte> header;
    private readonly Memory<byte> body;
    private Span<byte> Header => header.Span;

    internal RawCommonData(Memory<byte> header, Memory<byte> body)
    {
        this.header = header;
        this.body = body;
    }

    /// <summary>
    /// The total length in bytes of the file (header length + compressed body length).
    /// </summary>
    public int CompressedFileSize
    {
        get => BinaryPrimitives.ReadInt32LittleEndian(Header.Slice(0x10));
        private set => BinaryPrimitives.WriteInt32LittleEndian(Header.Slice(0x10), value);
    }

    /// <summary>
    /// See <see cref="LibDQB.B2.Records.SaveFileKey"/>
    /// </summary>
    public SaveFileKey SaveFileKey
    {
        get => new(BinaryPrimitives.ReadUInt32LittleEndian(Header.Slice(0x80)));
        set => BinaryPrimitives.WriteUInt32LittleEndian(Header.Slice(0x80), value.Value);
    }

    /// <summary>
    /// When sailing, indicates the arrival island.
    /// When not sailing, indicates the island the builder is on.
    /// </summary>
    public IslandId ToIslandId
    {
        get => new IslandId(Header[0xC8]);
        set => Header[0xC8] = value.Value;
    }

    /// <summary>
    /// When sailing, indicates the departure island.
    /// When not sailing, indicates the island the builder is on.
    /// </summary>
    public IslandId FromIslandId
    {
        get => new IslandId(Header[0xC9]);
        set => Header[0xC9] = value.Value;
    }

    /// <summary>
    /// The timestamp shown by the game when you load the file.
    /// </summary>
    /// <remarks>
    /// The save file wants UTC.
    /// The game adjusts to the user's time zone when displaying the value.
    /// </remarks>
    public DateTime LastSaveTime
    {
        // Signed because that's what DateTime likes to use.
        // Unconfirmed if the game cares, but it won't matter for any reasonable value.
        get => DateTime.FromFileTimeUtc(BinaryPrimitives.ReadInt64LittleEndian(Header.Slice(0x2A40D)));
        set => BinaryPrimitives.WriteInt64LittleEndian(Header.Slice(0x2A40D), value.ToFileTimeUtc());
    }

    public IMinimap GetMinimap(IslandId islandId)
    {
        const int minimapStart = 2401803; // Island 0 starts at this address (in the body, no header)
        int offset = ReadOnlyMinimap.IslandDataLength * islandId.Value;
        var slice = body.Slice(minimapStart + offset, ReadOnlyMinimap.TileDataLength);
        return new Minimap(slice);
    }

    public void Save(string path)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        Save(stream);
    }

    public void Save(Stream outstream)
    {
        using var compressedBody = new MemoryStream();
        using var zlib = new ZLibStream(compressedBody, CompressionMode.Compress);
        zlib.Write(body.Span);
        zlib.Flush();
        compressedBody.Flush();

        // TODO - it's not clear if we should set this or not...
        // But we do need to write the correct file size either way
        CompressedFileSize = Convert.ToInt32(compressedBody.Length) + header.Length;
        outstream.Write(header.Span);

        compressedBody.Seek(0, SeekOrigin.Begin);
        compressedBody.CopyTo(outstream);

        outstream.Flush();
    }
}
