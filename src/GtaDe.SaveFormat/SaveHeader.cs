namespace GtaDe.SaveFormat;

/// <summary>
/// The fixed-position fields at the very start of a Definitive Edition save slot.
/// </summary>
/// <remarks>
/// <code>
/// +0x00 u32      magic 0x00FF00FF (stored as 00 FF 00 FF)
/// +0x04 u32      save format version (16 for GTA III DE)
/// +0x08 byte[16] integrity seal, see <see cref="SaveSeal"/>
/// +0x18 u32      save format version, repeated
/// +0x1C i64      Unreal FDateTime, 100ns ticks since 0001-01-01 UTC
/// +0x24 u32      byte length of the following GXT key, including its NUL
/// +0x28 ascii    GXT key of the last mission passed, e.g. "LM3\0"
/// </code>
/// SimpleVars follows immediately after the GXT key.
/// </remarks>
public sealed class SaveHeader
{
    public const int MagicOffset = 0x00;
    public const int VersionOffset = 0x04;
    public const int VersionRepeatOffset = 0x18;
    public const int TimestampOffset = 0x1C;
    public const int LastMissionLengthOffset = 0x24;
    public const int LastMissionOffset = 0x28;

    /// <summary>The magic value as it reads back as a little-endian uint32.</summary>
    public const uint Magic = 0xFF00FF00;

    private readonly byte[] _data;

    private SaveHeader(byte[] data, int length)
    {
        _data = data;
        Length = length;
    }

    /// <summary>Total byte length of the header, i.e. the offset at which SimpleVars begins.</summary>
    public int Length { get; }

    public uint Version => ByteOps.ReadUInt32(_data, VersionOffset);

    public uint VersionRepeat => ByteOps.ReadUInt32(_data, VersionRepeatOffset);

    /// <summary>Raw Unreal <c>FDateTime</c> tick count.</summary>
    public long TimestampTicks
    {
        get => ByteOps.ReadInt64(_data, TimestampOffset);
        set => ByteOps.WriteInt64(_data, TimestampOffset, value);
    }

    /// <summary>The save timestamp as a UTC <see cref="DateTime"/>.</summary>
    public DateTime Timestamp
    {
        get => new(TimestampTicks, DateTimeKind.Utc);
        set => TimestampTicks = value.ToUniversalTime().Ticks;
    }

    /// <summary>Byte length of the GXT key field, including the terminating NUL.</summary>
    public int LastMissionKeyLength => (int)ByteOps.ReadUInt32(_data, LastMissionLengthOffset);

    /// <summary>
    /// GXT key of the last mission passed, e.g. <c>LM3</c>. This is what the slot shows in the
    /// load menu; it is not a format tag.
    /// </summary>
    public string LastMissionKey
    {
        get
        {
            var span = _data.AsSpan(LastMissionOffset, LastMissionKeyLength);
            var nul = span.IndexOf((byte)0);
            if (nul >= 0)
            {
                span = span[..nul];
            }

            return System.Text.Encoding.ASCII.GetString(span);
        }
    }

    public static SaveHeader Read(byte[] data)
    {
        if (data.Length < LastMissionOffset + 4)
        {
            throw new SaveFormatException("The file is too short to be a GTA Definitive Edition save.");
        }

        if (ByteOps.ReadUInt32(data, MagicOffset) != Magic)
        {
            throw new SaveFormatException(
                "This is not a GTA Definitive Edition save file (the 00 FF 00 FF magic is missing).");
        }

        var keyLength = (int)ByteOps.ReadUInt32(data, LastMissionLengthOffset);
        if (keyLength is < 1 or > 64 || LastMissionOffset + keyLength > data.Length)
        {
            throw new SaveFormatException($"Implausible last-mission key length {keyLength}.");
        }

        return new SaveHeader(data, LastMissionOffset + keyLength);
    }
}
