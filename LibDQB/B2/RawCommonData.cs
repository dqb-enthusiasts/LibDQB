using LibDQB.B2.Records;
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
public sealed class RawCommonData : FileData
{
    protected override uint headerLength { get { return FileFactory.CommonDataHeader; } }
    protected override uint decompressedBodyLength { get { return (uint)_body.Length; } }

    internal RawCommonData(Memory<byte> header, Memory<byte> body) 
		: base(header, body)
    {
    }

	// S> Think we should move some of the next things to composition classes.

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
}
