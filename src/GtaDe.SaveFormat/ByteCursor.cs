using System.Buffers.Binary;

namespace GtaDe.SaveFormat;

/// <summary>
/// Little-endian cursor over a byte buffer. All GTA DE save data is little-endian.
/// </summary>
public ref struct ByteCursor
{
    private readonly ReadOnlySpan<byte> _data;

    public ByteCursor(ReadOnlySpan<byte> data, int position = 0)
    {
        _data = data;
        Position = position;
    }

    public int Position { get; set; }

    public int Length => _data.Length;

    public int Remaining => _data.Length - Position;

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        EnsureAvailable(count);
        var slice = _data.Slice(Position, count);
        Position += count;
        return slice;
    }

    public byte ReadByte()
    {
        EnsureAvailable(1);
        return _data[Position++];
    }

    public uint ReadUInt32()
    {
        var value = BinaryPrimitives.ReadUInt32LittleEndian(_data.Slice(Position, 4));
        Position += 4;
        return value;
    }

    public int ReadInt32()
    {
        var value = BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(Position, 4));
        Position += 4;
        return value;
    }

    public ulong ReadUInt64()
    {
        var value = BinaryPrimitives.ReadUInt64LittleEndian(_data.Slice(Position, 8));
        Position += 8;
        return value;
    }

    public void Skip(int count)
    {
        EnsureAvailable(count);
        Position += count;
    }

    private void EnsureAvailable(int count)
    {
        if (count < 0 || Position + count > _data.Length)
        {
            throw new SaveFormatException(
                $"Attempted to read {count} byte(s) at offset {Position} but only {Remaining} byte(s) remain.");
        }
    }
}

/// <summary>Static little-endian helpers for reading from and writing into byte arrays.</summary>
public static class ByteOps
{
    public static int ReadInt32(byte[] data, int offset) =>
        BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4));

    public static uint ReadUInt32(byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));

    public static ulong ReadUInt64(byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset, 8));

    public static long ReadInt64(byte[] data, int offset) =>
        BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(offset, 8));

    public static float ReadSingle(byte[] data, int offset) =>
        BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(offset, 4));

    public static short ReadInt16(byte[] data, int offset) =>
        BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset, 2));

    public static ushort ReadUInt16(byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));

    public static void WriteInt32(byte[] data, int offset, int value) =>
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset, 4), value);

    public static void WriteUInt32(byte[] data, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, 4), value);

    public static void WriteUInt64(byte[] data, int offset, ulong value) =>
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(offset, 8), value);

    public static void WriteInt64(byte[] data, int offset, long value) =>
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(offset, 8), value);

    public static void WriteSingle(byte[] data, int offset, float value) =>
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(offset, 4), value);

    public static void WriteInt16(byte[] data, int offset, short value) =>
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset, 2), value);

    public static void WriteUInt16(byte[] data, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset, 2), value);
}
