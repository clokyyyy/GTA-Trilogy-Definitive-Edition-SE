namespace GtaDe.SaveFormat;

/// <summary>Weather types used by San Andreas, matching <c>eWeatherType</c>.</summary>
public enum SanAndreasWeather : short
{
    ExtraSunnyLosSantos = 0,
    SunnyLosSantos = 1,
    ExtraSunnySmogLosSantos = 2,
    SunnySmogLosSantos = 3,
    CloudyLosSantos = 4,
    SunnySanFierro = 5,
    ExtraSunnySanFierro = 6,
    CloudySanFierro = 7,
    RainySanFierro = 8,
    FoggySanFierro = 9,
    SunnyLasVenturas = 10,
    ExtraSunnyLasVenturas = 11,
    CloudyLasVenturas = 12,
    ExtraSunnyCountryside = 13,
    SunnyCountryside = 14,
    CloudyCountryside = 15,
    RainyCountryside = 16,
    ExtraSunnyDesert = 17,
    SunnyDesert = 18,
    SandstormDesert = 19,
    Underwater = 20,
    ExtraColours1 = 21,
    ExtraColours2 = 22,
}

/// <summary>
/// The San Andreas SIMPLE_VARIABLES section: world clock, weather and camera.
/// </summary>
/// <remarks>
/// <para>The section opens with the last mission key (<c>u32 length + text</c>, the same 12 bytes
/// as the header), then the classic SimpleVars fields. Offsets below are relative to the end of
/// that key:</para>
/// <code>
/// +0x00 u32  version id
/// +0x04 u8   mission pack
/// +0x05 i32  current area
/// +0x09 f32  camera X, Y, Z
/// +0x15 u32  milliseconds per in-game minute
/// +0x19 u32  last clock tick
/// +0x1D u8   month, day, hour, minute
/// +0x2B u32  elapsed milliseconds
/// +0x3F i16  previous, current and forced weather (-1 = not forced)
/// +0x45 f32  weather interpolation
/// </code>
/// </remarks>
public sealed class SanAndreasSimpleVars
{
    public const short WeatherNotForced = -1;

    private readonly byte[] _data;

    public SanAndreasSimpleVars(byte[] data, SaveBlock block)
    {
        _data = data;
        if (block.DataLength < 4)
        {
            throw new SaveFormatException("The SIMPLE_VARIABLES section is too short.");
        }

        var keyLength = (int)ByteOps.ReadUInt32(data, block.DataOffset);
        if (keyLength is < 0 or > 64)
        {
            throw new SaveFormatException("The SIMPLE_VARIABLES section does not start with a mission key.");
        }

        // Every field read or written here must stay inside this section, never spill into the next one.
        if (4 + keyLength + LastFieldEnd > block.DataLength)
        {
            throw new SaveFormatException("The SIMPLE_VARIABLES section is too short for its clock and weather fields.");
        }

        Offset = block.DataOffset + 4 + keyLength;
    }

    /// <summary>End of the weather interpolation float, the last field this class touches.</summary>
    private const int LastFieldEnd = 0x45 + 4;

    /// <summary>Absolute offset of the version id, the first classic SimpleVars field.</summary>
    public int Offset { get; }

    public float CameraX => ByteOps.ReadSingle(_data, Offset + 0x09);

    public float CameraY => ByteOps.ReadSingle(_data, Offset + 0x0D);

    public float CameraZ => ByteOps.ReadSingle(_data, Offset + 0x11);

    public uint MillisecondsPerGameMinute => ByteOps.ReadUInt32(_data, Offset + 0x15);

    public int Month => _data[Offset + 0x1D];

    public int Day => _data[Offset + 0x1E];

    public int Hour
    {
        get => _data[Offset + 0x1F];
        set => _data[Offset + 0x1F] = (byte)Math.Clamp(value, 0, 23);
    }

    public int Minute
    {
        get => _data[Offset + 0x20];
        set => _data[Offset + 0x20] = (byte)Math.Clamp(value, 0, 59);
    }

    public uint ElapsedMilliseconds => ByteOps.ReadUInt32(_data, Offset + 0x2B);

    public SanAndreasWeather PreviousWeather
    {
        get => (SanAndreasWeather)ByteOps.ReadInt16(_data, Offset + 0x3F);
        set => ByteOps.WriteInt16(_data, Offset + 0x3F, (short)value);
    }

    public SanAndreasWeather CurrentWeather
    {
        get => (SanAndreasWeather)ByteOps.ReadInt16(_data, Offset + 0x41);
        set => ByteOps.WriteInt16(_data, Offset + 0x41, (short)value);
    }

    public short ForcedWeather
    {
        get => ByteOps.ReadInt16(_data, Offset + 0x43);
        set => ByteOps.WriteInt16(_data, Offset + 0x43, value);
    }

    public float WeatherInterpolation
    {
        get => ByteOps.ReadSingle(_data, Offset + 0x45);
        set => ByteOps.WriteSingle(_data, Offset + 0x45, Math.Clamp(value, 0f, 1f));
    }

    public void SetWeather(SanAndreasWeather weather)
    {
        PreviousWeather = weather;
        CurrentWeather = weather;
        WeatherInterpolation = 0f;
    }
}

/// <summary>
/// The San Andreas PLAYERINFO section: a <c>u32</c> size followed by the saved part of
/// <c>CPlayerInfo</c>.
/// </summary>
/// <remarks>
/// Verified against a 100% save: money equals the visible money, and the maximum health and
/// armour bytes read 220 and 150 (the Paramedic and Vigilante rewards are tied to these).
/// </remarks>
public sealed class SanAndreasPlayerInfoBlock(byte[] data, SaveBlock block) : BlockAccessor(data, block)
{
    private const int P = 4;

    public bool IsAvailable => Block.DataLength >= P + 36 && GetInt(0) >= 36;

    public int Money
    {
        get => GetInt(P + 0);
        set => SetInt(P + 0, value);
    }

    public int VisibleMoney
    {
        get => GetInt(P + 12);
        set => SetInt(P + 12, value);
    }

    /// <summary>Paramedic level 12 reward.</summary>
    public bool InfiniteSprint
    {
        get => GetBool(P + 28);
        set => SetBool(P + 28, value);
    }

    public bool FastReload
    {
        get => GetBool(P + 29);
        set => SetBool(P + 29, value);
    }

    /// <summary>Firefighter level 12 reward.</summary>
    public bool Fireproof
    {
        get => GetBool(P + 30);
        set => SetBool(P + 30, value);
    }

    /// <summary>Raised by Paramedic level 12 (to the 176 cap) and by 100% completion.</summary>
    public int MaxHealth
    {
        get => GetByte(P + 31);
        set => SetByte(P + 31, (byte)Math.Clamp(value, 1, 255));
    }

    /// <summary>Raised to 150 by Vigilante level 12.</summary>
    public int MaxArmour
    {
        get => GetByte(P + 32);
        set => SetByte(P + 32, (byte)Math.Clamp(value, 1, 255));
    }

    public bool GetOutOfJailFree
    {
        get => GetBool(P + 33);
        set => SetBool(P + 33, value);
    }

    public bool FreeHealthCare
    {
        get => GetBool(P + 34);
        set => SetBool(P + 34, value);
    }

    public bool DriveByAllowed
    {
        get => GetBool(P + 35);
        set => SetBool(P + 35, value);
    }

    public void SetMoney(int value)
    {
        Money = value;
        VisibleMoney = value;
    }
}

/// <summary>
/// The San Andreas STATS section: 82 float stats (ids 0–81) followed by 223 integer stats
/// (ids 120–342), then data the editor does not touch.
/// </summary>
public sealed class SanAndreasStatsBlock(byte[] data, SaveBlock block) : BlockAccessor(data, block)
{
    public const int FloatStatCount = 82;
    public const int FirstIntStat = 120;
    public const int LastIntStat = 342;
    private const int IntStatsOffset = FloatStatCount * 4;

    public static bool IsFloatStat(int id) => id is >= 0 and < FloatStatCount;

    public static bool IsIntStat(int id) => id is >= FirstIntStat and <= LastIntStat;

    public static bool IsValid(int id) => IsFloatStat(id) || IsIntStat(id);

    public float GetFloatStat(int id) =>
        IsFloatStat(id) ? GetFloat(id * 4) : throw new ArgumentOutOfRangeException(nameof(id), id, "Not a float stat.");

    public void SetFloatStat(int id, float value)
    {
        if (!IsFloatStat(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Not a float stat.");
        }

        SetFloat(id * 4, value);
    }

    public int GetIntStat(int id) =>
        IsIntStat(id) ? GetInt(IntStatsOffset + ((id - FirstIntStat) * 4)) : throw new ArgumentOutOfRangeException(nameof(id), id, "Not an integer stat.");

    public void SetIntStat(int id, int value)
    {
        if (!IsIntStat(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Not an integer stat.");
        }

        SetInt(IntStatsOffset + ((id - FirstIntStat) * 4), value);
    }

    /// <summary>Reads any stat as a double, whichever storage it uses.</summary>
    public double Get(int id) => IsFloatStat(id) ? GetFloatStat(id) : GetIntStat(id);

    /// <summary>Writes any stat, rounding when it is an integer stat.</summary>
    public void Set(int id, double value)
    {
        if (IsFloatStat(id))
        {
            SetFloatStat(id, (float)value);
        }
        else
        {
            SetIntStat(id, (int)Math.Round(value));
        }
    }
}

/// <summary>
/// The San Andreas TAGS section: a <c>u32</c> count then one alpha byte per gang tag. A tag is
/// sprayed over by the player when its alpha is 255.
/// </summary>
public sealed class SanAndreasTagsBlock(byte[] data, SaveBlock block) : BlockAccessor(data, block)
{
    public const int TotalTags = 100;
    private const byte Sprayed = 0xFF;

    public int Count => IsAvailable ? GetInt(0) : 0;

    public bool IsAvailable => Block.DataLength >= 4 && GetInt(0) is > 0 and <= 1000 && Block.DataLength >= 4 + GetInt(0);

    public int SprayedCount => Enumerable.Range(0, Count).Count(IsSprayed);

    public bool IsSprayed(int index) => GetByte(4 + index) == Sprayed;

    public void SetSprayed(int index, bool value) => SetByte(4 + index, value ? Sprayed : (byte)0);

    /// <summary>Marks the first <paramref name="count"/> tags sprayed and the rest unsprayed.</summary>
    public void SetSprayedCount(int count)
    {
        count = Math.Clamp(count, 0, Count);
        var current = SprayedCount;
        if (current == count)
        {
            return;
        }

        if (count > current)
        {
            for (var i = 0; i < Count && current < count; i++)
            {
                if (!IsSprayed(i))
                {
                    SetSprayed(i, true);
                    current++;
                }
            }
        }
        else
        {
            for (var i = Count - 1; i >= 0 && current > count; i--)
            {
                if (IsSprayed(i))
                {
                    SetSprayed(i, false);
                    current--;
                }
            }
        }
    }
}
