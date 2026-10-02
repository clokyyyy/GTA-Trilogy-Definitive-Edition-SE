namespace GtaDe.SaveFormat;

/// <summary>Weather types used by Vice City.</summary>
public enum ViceCityWeather : ushort
{
    Sunny = 0,
    Cloudy = 1,
    Rainy = 2,
    Foggy = 3,
    ExtraSunny = 4,
    Hurricane = 5,
}

/// <summary>Vice City's two halves, matching the game's level identifiers.</summary>
public enum ViceCityLevel
{
    Generic = 0,
    Beach = 1,
    Mainland = 2,
}

/// <summary>
/// The 156-byte Vice City SimpleVars region: world clock, weather and camera.
/// </summary>
/// <remarks>
/// <para>Verified field map, relative to the start of SimpleVars:</para>
/// <code>
/// +0x00 u32  0x00031401  constant carried over from the PC version
/// +0x04 i32  current level
/// +0x08 f32  camera X, Y, Z
/// +0x14 u32  milliseconds per in-game minute
/// +0x18 u32  last clock tick
/// +0x1C u8   game hour
/// +0x1D u8   game minute
/// +0x20 u32  elapsed milliseconds
/// +0x24 f32  time scale
/// +0x40 u16  previous weather
/// +0x42 u16  current weather
/// +0x44 u16  forced weather, 0xFFFF when the game picks the weather itself
/// +0x46 f32  weather interpolation, 0 to 1
/// +0x74 u32[10] radio station positions
/// </code>
/// The fields between the weather and the radio positions are not mapped and are never touched.
/// </remarks>
public sealed class ViceCitySimpleVars(byte[] data, int offset)
{
    public const ushort WeatherNotForced = 0xFFFF;

    private readonly byte[] _data = data;

    public int Offset { get; } = offset;

    public uint SignatureValue => ByteOps.ReadUInt32(_data, Offset);

    public ViceCityLevel Level => (ViceCityLevel)ByteOps.ReadInt32(_data, Offset + 0x04);

    public float CameraX => ByteOps.ReadSingle(_data, Offset + 0x08);

    public float CameraY => ByteOps.ReadSingle(_data, Offset + 0x0C);

    public float CameraZ => ByteOps.ReadSingle(_data, Offset + 0x10);

    public uint MillisecondsPerGameMinute
    {
        get => ByteOps.ReadUInt32(_data, Offset + 0x14);
        set => ByteOps.WriteUInt32(_data, Offset + 0x14, value);
    }

    public int Hour
    {
        get => _data[Offset + 0x1C];
        set => _data[Offset + 0x1C] = (byte)Math.Clamp(value, 0, 23);
    }

    public int Minute
    {
        get => _data[Offset + 0x1D];
        set => _data[Offset + 0x1D] = (byte)Math.Clamp(value, 0, 59);
    }

    public ViceCityWeather PreviousWeather
    {
        get => (ViceCityWeather)ByteOps.ReadUInt16(_data, Offset + 0x40);
        set => ByteOps.WriteUInt16(_data, Offset + 0x40, (ushort)value);
    }

    public ViceCityWeather CurrentWeather
    {
        get => (ViceCityWeather)ByteOps.ReadUInt16(_data, Offset + 0x42);
        set => ByteOps.WriteUInt16(_data, Offset + 0x42, (ushort)value);
    }

    public ushort ForcedWeather
    {
        get => ByteOps.ReadUInt16(_data, Offset + 0x44);
        set => ByteOps.WriteUInt16(_data, Offset + 0x44, value);
    }

    public float WeatherInterpolation
    {
        get => ByteOps.ReadSingle(_data, Offset + 0x46);
        set => ByteOps.WriteSingle(_data, Offset + 0x46, Math.Clamp(value, 0f, 1f));
    }

    public void SetWeather(ViceCityWeather weather)
    {
        PreviousWeather = weather;
        CurrentWeather = weather;
        WeatherInterpolation = 0f;
    }
}

/// <summary>
/// The Vice City PlayerInfo block, in <c>CPlayerInfo::SavePlayerInfo</c> order and densely packed.
/// </summary>
/// <remarks>
/// Verified against a retail save: money equals the visible money, the package total reads 100,
/// and the maximum health and armour bytes read 200 after all the Vice City bonuses.
/// </remarks>
public sealed class ViceCityPlayerInfoBlock(byte[] data, SaveBlock block) : BlockAccessor(data, block)
{
    public const int HiddenPackageTotal = 100;

    public int Money
    {
        get => GetInt(0x00);
        set => SetInt(0x00, value);
    }

    public ushort TrafficMultiplier
    {
        get => GetUInt16(0x09);
        set => SetUInt16(0x09, value);
    }

    public float RoadDensity
    {
        get => GetFloat(0x0B);
        set => SetFloat(0x0B, value);
    }

    public int VisibleMoney
    {
        get => GetInt(0x0F);
        set => SetInt(0x0F, value);
    }

    public int HiddenPackagesCollected
    {
        get => GetInt(0x13);
        set => SetInt(0x13, value);
    }

    public int TotalHiddenPackages
    {
        get => GetInt(0x17);
        set => SetInt(0x17, value);
    }

    /// <summary>Phil Cassidy's reward: unlimited sprint.</summary>
    public bool InfiniteSprint
    {
        get => GetBool(0x1B);
        set => SetBool(0x1B, value);
    }

    /// <summary>Shooting range reward: fast reload.</summary>
    public bool FastReload
    {
        get => GetBool(0x1C);
        set => SetBool(0x1C, value);
    }

    /// <summary>Firefighter level 12 reward.</summary>
    public bool Fireproof
    {
        get => GetBool(0x1D);
        set => SetBool(0x1D, value);
    }

    /// <summary>Paramedic level 12 reward raises this to 150; 100% completion raises it to 200.</summary>
    public int MaxHealth
    {
        get => GetByte(0x1E);
        set => SetByte(0x1E, (byte)Math.Clamp(value, 1, 255));
    }

    /// <summary>Pizza boy level 10 reward raises this to 150; 100% completion raises it to 200.</summary>
    public int MaxArmour
    {
        get => GetByte(0x1F);
        set => SetByte(0x1F, (byte)Math.Clamp(value, 1, 255));
    }

    /// <summary>Vigilante level 12 reward: bribes are free when busted.</summary>
    public bool GetOutOfJailFree
    {
        get => GetBool(0x20);
        set => SetBool(0x20, value);
    }

    /// <summary>Paramedic reward: hospital visits are free.</summary>
    public bool FreeHealthCare
    {
        get => GetBool(0x21);
        set => SetBool(0x21, value);
    }

    /// <summary>Whether the player can shoot out of vehicle windows.</summary>
    public bool DriveByAllowed
    {
        get => GetBool(0x22);
        set => SetBool(0x22, value);
    }

    public void SetMoney(int value)
    {
        Money = value;
        VisibleMoney = value;
    }
}
