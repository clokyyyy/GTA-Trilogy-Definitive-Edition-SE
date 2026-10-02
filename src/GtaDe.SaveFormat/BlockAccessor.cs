namespace GtaDe.SaveFormat;

/// <summary>
/// Base class for accessors that read and write fields directly inside the save buffer.
/// </summary>
public abstract class BlockAccessor(byte[] data, SaveBlock block)
{
    protected byte[] Data { get; } = data;

    public SaveBlock Block { get; } = block;

    protected int Absolute(int offset, int size = 4)
    {
        if (offset < 0 || offset + size > Block.DataLength)
        {
            throw new SaveFormatException(
                $"Offset 0x{offset:X} is outside the {Block.Kind} block ({Block.DataLength} bytes).");
        }

        return Block.DataOffset + offset;
    }

    protected int GetInt(int offset) => ByteOps.ReadInt32(Data, Absolute(offset));

    protected void SetInt(int offset, int value) => ByteOps.WriteInt32(Data, Absolute(offset), value);

    protected float GetFloat(int offset) => ByteOps.ReadSingle(Data, Absolute(offset));

    protected void SetFloat(int offset, float value) => ByteOps.WriteSingle(Data, Absolute(offset), value);

    protected ushort GetUInt16(int offset) => ByteOps.ReadUInt16(Data, Absolute(offset, 2));

    protected void SetUInt16(int offset, ushort value) => ByteOps.WriteUInt16(Data, Absolute(offset, 2), value);

    protected byte GetByte(int offset) => Data[Absolute(offset, 1)];

    protected void SetByte(int offset, byte value) => Data[Absolute(offset, 1)] = value;

    protected bool GetBool(int offset) => GetByte(offset) != 0;

    protected void SetBool(int offset, bool value) => SetByte(offset, value ? (byte)1 : (byte)0);
}
