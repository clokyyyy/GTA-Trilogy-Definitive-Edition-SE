namespace GtaDe.SaveFormat;

/// <summary>What a garage does, matching the game's <c>eGarageType</c>.</summary>
public enum GarageType : byte
{
    None = 0,
    Mission = 1,
    BombShop1 = 2,
    BombShop2 = 3,
    BombShop3 = 4,
    Respray = 5,
    CollectorsItems = 6,
    CollectSpecificCars = 7,
    CollectCars1 = 8,
    CollectCars2 = 9,
    CollectCars3 = 10,
    ForCarToComeOutOf = 11,
    SixtySeconds = 12,
    Crusher = 13,
    MissionKeepCar = 14,
    ForScriptToOpen = 15,
    HideoutOne = 16,
    HideoutTwo = 17,
    HideoutThree = 18,
    ForScriptToOpenAndClose = 19,
    KeepsOpeningAndClosing = 20,
    MissionKeepCarRemainClosed = 21,
}

/// <summary>Readable names for <see cref="GarageType"/>.</summary>
public static class GarageTypeNames
{
    public static string ToDisplayName(this GarageType type) => type switch
    {
        GarageType.None => "Unused",
        GarageType.Mission => "Mission garage",
        GarageType.BombShop1 => "8-Ball's bomb shop",
        GarageType.BombShop2 => "Bomb shop",
        GarageType.BombShop3 => "Bomb shop",
        GarageType.Respray => "Pay 'n' Spray",
        GarageType.CollectorsItems => "Collector's items",
        GarageType.CollectSpecificCars => "Specific cars wanted",
        GarageType.CollectCars1 => "Import / export",
        GarageType.CollectCars2 => "Import / export (crane)",
        GarageType.CollectCars3 => "Import / export",
        GarageType.ForCarToComeOutOf => "Car spawn garage",
        GarageType.SixtySeconds => "60 second garage",
        GarageType.Crusher => "Crusher",
        GarageType.MissionKeepCar => "Mission garage (keeps car)",
        GarageType.ForScriptToOpen => "Script-opened garage",
        GarageType.HideoutOne => "Safehouse",
        GarageType.HideoutTwo => "Safehouse",
        GarageType.HideoutThree => "Safehouse",
        GarageType.ForScriptToOpenAndClose => "Script-controlled garage",
        GarageType.KeepsOpeningAndClosing => "Opens and closes",
        GarageType.MissionKeepCarRemainClosed => "Mission garage (stays closed)",
        _ => $"Type {(byte)type}",
    };
}

/// <summary>A car parked in a safehouse garage.</summary>
/// <param name="Slot">Index into the stored-car array.</param>
/// <param name="Offset">File offset of the record.</param>
public readonly record struct StoredCar(int Slot, int Offset, int ModelIndex, float X, float Y, float Z)
{
    public bool IsEmpty => ModelIndex == 0;
}

/// <summary>One garage as the save records it.</summary>
public readonly record struct GarageSlot(
    int Index,
    int Offset,
    GarageType Type,
    byte State,
    float X1,
    float X2,
    float Y1,
    float Y2)
{
    public bool IsEmpty => Type == GarageType.None;

    public float CentreX => (X1 + X2) / 2f;

    public float CentreY => (Y1 + Y2) / 2f;

    /// <summary>Which island the garage sits on, from its world position.</summary>
    public string Island => CentreX switch
    {
        < -500f => "Shoreside Vale",
        < 700f => "Staunton Island",
        _ => "Portland",
    };
}

/// <summary>
/// Reads the Garages block.
/// </summary>
/// <remarks>
/// <para>
/// The layout was recovered by inspecting retail saves rather than from documentation, and is
/// confirmed by the fact that the recovered garage types and world positions land exactly on
/// the real ones: the Harwood crusher, three Pay 'n' Sprays, three bomb shops, three safehouses
/// and both import/export garages.
/// </para>
/// <code>
/// +0    i32   NumGarages (27 in a retail save)
/// +4    u32   BombsAreFree
/// +8    u32   RespraysAreFree
/// +12   i32   CarsCollected
/// +16   i32   BankVansCollected
/// +20   i32   PoliceCarsCollected
/// +24   i32   CarTypesCollected[3]
/// +36   u32   LastTimeHelpMessage
/// +40         16 stored cars, 44 bytes each
/// +744        43 garages, 140 bytes each (the game's sizeof(CGarage))
/// </code>
/// <para>
/// Within a garage record the first 16 bytes are the two door entity pointers, which the game
/// always writes as zero, so the type and state bytes sit at +16 and +17.
/// </para>
/// </remarks>
public sealed class GarageBlock
{
    internal const int HeaderLength = 40;
    internal const int StoredCarLength = 44;

    /// <summary>Number of safehouse parking spaces the save reserves.</summary>
    public const int StoredCarCount = 16;

    internal const int GarageArrayOffset = 744;
    internal const int GarageLength = 140;

    /// <summary>Number of garage slots the save reserves, used or not.</summary>
    public const int GarageCount = 43;

    private const int TypeWithinGarage = 16;
    private const int StateWithinGarage = 17;
    private const int BoundsWithinGarage = 44;

    private readonly byte[] _buffer;
    private readonly int _base;

    internal GarageBlock(byte[] buffer, SaveBlock block)
    {
        _buffer = buffer;
        _base = block.DataOffset;

        var expected = GarageArrayOffset + (GarageCount * GarageLength);
        IsRecognised = block.DataLength == expected;
    }

    /// <summary>
    /// False when the block is not the size this reader understands, in which case every
    /// accessor returns empty and nothing may be written.
    /// </summary>
    public bool IsRecognised { get; }

    /// <summary>How many of the garage slots the game actually uses.</summary>
    public int ActiveGarageCount => IsRecognised ? ReadInt(0) : 0;

    /// <summary>Car bombs are fitted free of charge.</summary>
    public bool BombsAreFree
    {
        get => IsRecognised && ReadInt(4) != 0;
        set => WriteInt(4, value ? 1 : 0);
    }

    /// <summary>Respraying costs nothing.</summary>
    public bool RespraysAreFree
    {
        get => IsRecognised && ReadInt(8) != 0;
        set => WriteInt(8, value ? 1 : 0);
    }

    /// <summary>Cars delivered to the Portland import/export garage.</summary>
    public int CarsCollected => IsRecognised ? ReadInt(12) : 0;

    public int BankVansCollected => IsRecognised ? ReadInt(16) : 0;

    public int PoliceCarsCollected => IsRecognised ? ReadInt(20) : 0;

    /// <summary>Every garage slot, including the unused tail.</summary>
    public IReadOnlyList<GarageSlot> Garages
    {
        get
        {
            if (!IsRecognised)
            {
                return [];
            }

            var slots = new List<GarageSlot>(GarageCount);
            for (var index = 0; index < GarageCount; index++)
            {
                var start = GarageArrayOffset + (index * GarageLength);
                slots.Add(new GarageSlot(
                    index,
                    _base + start,
                    (GarageType)_buffer[_base + start + TypeWithinGarage],
                    _buffer[_base + start + StateWithinGarage],
                    ReadFloat(start + BoundsWithinGarage),
                    ReadFloat(start + BoundsWithinGarage + 4),
                    ReadFloat(start + BoundsWithinGarage + 8),
                    ReadFloat(start + BoundsWithinGarage + 12)));
            }

            return slots;
        }
    }

    /// <summary>Cars parked in the safehouse garages.</summary>
    public IReadOnlyList<StoredCar> StoredCars
    {
        get
        {
            if (!IsRecognised)
            {
                return [];
            }

            var cars = new List<StoredCar>(StoredCarCount);
            for (var slot = 0; slot < StoredCarCount; slot++)
            {
                var start = HeaderLength + (slot * StoredCarLength);
                cars.Add(new StoredCar(
                    slot,
                    _base + start,
                    ReadInt(start),
                    ReadFloat(start + 4),
                    ReadFloat(start + 8),
                    ReadFloat(start + 12)));
            }

            return cars;
        }
    }

    /// <summary>
    /// The garage a parked car sits in, found by position rather than by assuming a fixed number
    /// of spaces per safehouse, since the save reserves more spaces than there are safehouses.
    /// </summary>
    public GarageSlot? GarageFor(StoredCar car)
    {
        if (car.IsEmpty)
        {
            return null;
        }

        foreach (var garage in Garages)
        {
            if (!garage.IsEmpty &&
                car.X >= garage.X1 - 2f && car.X <= garage.X2 + 2f &&
                car.Y >= garage.Y1 - 2f && car.Y <= garage.Y2 + 2f)
            {
                return garage;
            }
        }

        return null;
    }

    /// <summary>
    /// Empties a safehouse parking space, which is exactly what the game itself writes once the
    /// car has been driven out, so it cannot leave the save in a state the game has not seen.
    /// </summary>
    public void ClearStoredCar(int slot)
    {
        if (!IsRecognised || slot < 0 || slot >= StoredCarCount)
        {
            return;
        }

        var start = _base + HeaderLength + (slot * StoredCarLength);
        Array.Clear(_buffer, start, StoredCarLength);
    }

    private int ReadInt(int offset) => BitConverter.ToInt32(_buffer, _base + offset);

    private float ReadFloat(int offset) => BitConverter.ToSingle(_buffer, _base + offset);

    private void WriteInt(int offset, int value)
    {
        if (IsRecognised)
        {
            BitConverter.TryWriteBytes(_buffer.AsSpan(_base + offset, 4), value);
        }
    }
}
