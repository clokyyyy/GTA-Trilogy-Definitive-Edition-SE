namespace GtaDe.SaveFormat;

/// <summary>Weather types used by GTA III.</summary>
public enum WeatherType : ushort
{
    Sunny = 0,
    Cloudy = 1,
    Rainy = 2,
    Foggy = 3,
}

/// <summary>Islands, matching the game's level identifiers.</summary>
public enum IslandLevel
{
    None = 0,
    Portland = 1,
    StauntonIsland = 2,
    ShoresideVale = 3,
}

/// <summary>
/// The 118-byte SimpleVars region that follows the file header: world clock, weather, camera and
/// the engine timers.
/// </summary>
/// <remarks>
/// <para>Verified field map, relative to the start of SimpleVars:</para>
/// <code>
/// +0x00 u32  0x00031401  constant carried over from classic GTA III
/// +0x04 i32  current island
/// +0x08 f32  camera X, Y, Z
/// +0x14 u32  milliseconds per in-game minute
/// +0x18 u32  last clock tick
/// +0x1C u8   game hour
/// +0x1D u8   game minute
/// +0x20 u32  elapsed milliseconds
/// +0x24 f32  time scale
/// +0x28 f32  time step
/// +0x2C f32  time step, unclipped
/// +0x30 u32  frame counter
/// +0x40 u16  previous weather
/// +0x42 u16  current weather
/// +0x44 u16  forced weather, 0xFFFF when the game picks the weather itself
/// +0x46 f32  weather interpolation, 0 to 1
/// </code>
/// <para>
/// The DE build drops the alignment padding the PC version had, so the hour and minute sit in
/// adjacent single bytes rather than padded words.
/// </para>
/// </remarks>
public sealed class SimpleVars(byte[] data, int offset)
{
    /// <summary>Marker value GTA III has always written at the start of SimpleVars.</summary>
    public const uint Signature = 0x00031401;

    /// <summary>Written to <see cref="ForcedWeather"/> to let the game choose the weather.</summary>
    public const ushort WeatherNotForced = 0xFFFF;

    private readonly byte[] _data = data;

    public int Offset { get; } = offset;

    public uint SignatureValue => ByteOps.ReadUInt32(_data, Offset + 0x00);

    public IslandLevel Island
    {
        get => (IslandLevel)ByteOps.ReadInt32(_data, Offset + 0x04);
        set => ByteOps.WriteInt32(_data, Offset + 0x04, (int)value);
    }

    public float CameraX
    {
        get => ByteOps.ReadSingle(_data, Offset + 0x08);
        set => ByteOps.WriteSingle(_data, Offset + 0x08, value);
    }

    public float CameraY
    {
        get => ByteOps.ReadSingle(_data, Offset + 0x0C);
        set => ByteOps.WriteSingle(_data, Offset + 0x0C, value);
    }

    public float CameraZ
    {
        get => ByteOps.ReadSingle(_data, Offset + 0x10);
        set => ByteOps.WriteSingle(_data, Offset + 0x10, value);
    }

    public uint MillisecondsPerGameMinute
    {
        get => ByteOps.ReadUInt32(_data, Offset + 0x14);
        set => ByteOps.WriteUInt32(_data, Offset + 0x14, value);
    }

    /// <summary>In-game hour, 0 to 23.</summary>
    public int Hour
    {
        get => _data[Offset + 0x1C];
        set => _data[Offset + 0x1C] = (byte)Math.Clamp(value, 0, 23);
    }

    /// <summary>In-game minute, 0 to 59.</summary>
    public int Minute
    {
        get => _data[Offset + 0x1D];
        set => _data[Offset + 0x1D] = (byte)Math.Clamp(value, 0, 59);
    }

    public uint FrameCounter
    {
        get => ByteOps.ReadUInt32(_data, Offset + 0x30);
        set => ByteOps.WriteUInt32(_data, Offset + 0x30, value);
    }

    public WeatherType PreviousWeather
    {
        get => (WeatherType)ByteOps.ReadUInt16(_data, Offset + 0x40);
        set => ByteOps.WriteUInt16(_data, Offset + 0x40, (ushort)value);
    }

    public WeatherType CurrentWeather
    {
        get => (WeatherType)ByteOps.ReadUInt16(_data, Offset + 0x42);
        set => ByteOps.WriteUInt16(_data, Offset + 0x42, (ushort)value);
    }

    /// <summary>
    /// Weather the game is forced to use, or <see cref="WeatherNotForced"/> to let it cycle
    /// naturally.
    /// </summary>
    public ushort ForcedWeather
    {
        get => ByteOps.ReadUInt16(_data, Offset + 0x44);
        set => ByteOps.WriteUInt16(_data, Offset + 0x44, value);
    }

    /// <summary>How far the game has blended from the previous weather to the current one.</summary>
    public float WeatherInterpolation
    {
        get => ByteOps.ReadSingle(_data, Offset + 0x46);
        set => ByteOps.WriteSingle(_data, Offset + 0x46, Math.Clamp(value, 0f, 1f));
    }

    /// <summary>Sets the clock and keeps both weather slots consistent with a single choice.</summary>
    public void SetWeather(WeatherType weather)
    {
        PreviousWeather = weather;
        CurrentWeather = weather;
        WeatherInterpolation = 0f;
    }
}
