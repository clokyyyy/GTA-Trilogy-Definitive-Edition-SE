namespace GtaDe.SaveFormat;

/// <summary>How a stat is stored.</summary>
public enum StatKind
{
    Int,
    Float,
}

/// <summary>A named field in a stats block.</summary>
public sealed record StatField(string Group, string Name, int Offset, StatKind Kind, string? Hint = null);

/// <summary>
/// The Vice City Stats block, in <c>CStats::SaveStats</c> order.
/// </summary>
/// <remarks>
/// The first 595 bytes follow reVC exactly and were verified field-by-field against a retail save
/// (154 of 154 progress points, 36 unique jumps, 35 rampages, 15 properties). The DE appends 40
/// further bytes that are not mapped and are never touched.
/// </remarks>
public sealed class ViceCityStatsBlock(byte[] data, SaveBlock block) : BlockAccessor(data, block)
{
    public const int PedTypeCount = 23;
    public const int PropertyCount = 15;
    public const int FastestTimeCount = 23;
    public const int HighestScoreCount = 5;

    public const int PeopleKilledByPlayerOffset = 0;
    public const int PedsKilledOfThisTypeOffset = 24;
    public const int ProgressMadeOffset = 120;
    public const int TotalProgressInGameOffset = 124;
    public const int UniqueJumpsFoundOffset = 188;
    public const int TotalUniqueJumpsOffset = 192;
    public const int MissionsGivenOffset = 196;
    public const int LivesSavedOffset = 256;
    public const int CriminalsCaughtOffset = 260;
    public const int FiresExtinguishedOffset = 264;
    public const int HighestVigilanteOffset = 268;
    public const int HighestAmbulanceOffset = 272;
    public const int HighestFireOffset = 276;
    public const int KillFrenziesPassedOffset = 284;
    public const int TotalKillFrenziesOffset = 288;
    public const int TotalMissionsOffset = 292;
    public const int StoresKnockedOffOffset = 320;
    public const int AssassinationsOffset = 328;
    public const int PizzasDeliveredOffset = 332;
    public const int IceCreamSoldOffset = 340;
    public const int NumPropertyOwnedOffset = 388;
    public const int PropertyOwnedOffset = 400;
    public const int FastestTimesOffset = 419;
    public const int HighestScoresOffset = 511;
    public const int MappedLength = 595;

    public static IReadOnlyList<StatField> Fields { get; } =
    [
        new("Crime", "People wasted by you", 0, StatKind.Int),
        new("Crime", "People wasted by others", 4, StatKind.Int),
        new("Crime", "Cars destroyed", 8, StatKind.Int),
        new("Crime", "Boats destroyed", 12, StatKind.Int),
        new("Crime", "Tyres popped", 16, StatKind.Int),
        new("Crime", "Rounds fired", 20, StatKind.Int),
        new("Crime", "Helicopters destroyed", 116, StatKind.Int),
        new("Crime", "Explosives used (kg)", 128, StatKind.Int),
        new("Crime", "Bullets that hit", 132, StatKind.Int),
        new("Crime", "Headshots", 136, StatKind.Int),
        new("Crime", "Wanted stars attained", 140, StatKind.Int),
        new("Crime", "Wanted stars evaded", 144, StatKind.Int),
        new("Crime", "Times busted", 148, StatKind.Int),
        new("Crime", "Hospital visits", 152, StatKind.Int),
        new("Crime", "Property destroyed ($)", 384, StatKind.Int),
        new("Crime", "Highest media attention", 415, StatKind.Float),
        new("Crime", "Total legitimate kills", 539, StatKind.Int),
        new("Crime", "Times cheated", 551, StatKind.Int),
        new("Crime", "Seagulls sniped", 304, StatKind.Int),
        new("Life", "Days passed", 156, StatKind.Int),
        new("Life", "Safehouse visits", 160, StatKind.Int),
        new("Life", "Sprayings", 164, StatKind.Int),
        new("Life", "Times drowned", 300, StatKind.Int),
        new("Life", "Photos taken", 280, StatKind.Int),
        new("Life", "Flight time (ms)", 296, StatKind.Int),
        new("Money", "Spent on weapons", 308, StatKind.Float),
        new("Money", "Spent on clothes", 312, StatKind.Float),
        new("Money", "Spent on property", 376, StatKind.Float),
        new("Money", "Spent on auto repairs and paint", 380, StatKind.Float),
        new("Money", "Taxi earnings", 204, StatKind.Int),
        new("Driving", "Longest insane jump (m)", 168, StatKind.Float),
        new("Driving", "Highest insane jump (m)", 172, StatKind.Float),
        new("Driving", "Most flips", 176, StatKind.Int),
        new("Driving", "Most rotation (degrees)", 180, StatKind.Int),
        new("Driving", "Best insane stunt", 184, StatKind.Int, "0 = none ... 8 = Insane"),
        new("Driving", "Longest wheelie (ms)", 352, StatKind.Int),
        new("Driving", "Longest stoppie (ms)", 356, StatKind.Int),
        new("Driving", "Longest two-wheel (ms)", 360, StatKind.Int),
        new("Driving", "Longest wheelie (m)", 364, StatKind.Float),
        new("Driving", "Longest stoppie (m)", 368, StatKind.Float),
        new("Driving", "Longest two-wheel (m)", 372, StatKind.Float),
        new("Travel", "On foot (m)", 228, StatKind.Float),
        new("Travel", "By car (m)", 232, StatKind.Float),
        new("Travel", "By bike (m)", 236, StatKind.Float),
        new("Travel", "By boat (m)", 240, StatKind.Float),
        new("Travel", "By golf cart (m)", 244, StatKind.Float),
        new("Travel", "By helicopter (m)", 248, StatKind.Float),
        new("Travel", "By plane (m)", 252, StatKind.Float),
        new("Jobs", "Taxi fares", 200, StatKind.Int),
        new("Jobs", "Lives saved (Paramedic)", LivesSavedOffset, StatKind.Int),
        new("Jobs", "Criminals killed (Vigilante)", CriminalsCaughtOffset, StatKind.Int),
        new("Jobs", "Fires extinguished", FiresExtinguishedOffset, StatKind.Int),
        new("Jobs", "Highest Vigilante level", HighestVigilanteOffset, StatKind.Int),
        new("Jobs", "Highest Paramedic level", HighestAmbulanceOffset, StatKind.Int),
        new("Jobs", "Highest Firefighter level", HighestFireOffset, StatKind.Int),
        new("Jobs", "Pizzas delivered", PizzasDeliveredOffset, StatKind.Float),
        new("Jobs", "Ice cream sold", IceCreamSoldOffset, StatKind.Float),
        new("Jobs", "Stores knocked off", StoresKnockedOffOffset, StatKind.Float),
        new("Jobs", "Assassinations", AssassinationsOffset, StatKind.Float),
        new("Jobs", "Loan shark visits", 316, StatKind.Float),
        new("Jobs", "Movie stunts", 324, StatKind.Float),
        new("Jobs", "Garbage pickups", 336, StatKind.Float),
        new("Jobs", "Top shooting range score", 344, StatKind.Float),
        new("Jobs", "Shooting range rank", 348, StatKind.Float),
        new("Jobs", "Blood ring kills", 392, StatKind.Int),
        new("Jobs", "Blood ring longest time (s)", 396, StatKind.Int),
        new("Progress", "Progress made", ProgressMadeOffset, StatKind.Float),
        new("Progress", "Total progress in game", TotalProgressInGameOffset, StatKind.Float),
        new("Progress", "Missions attempted", MissionsGivenOffset, StatKind.Int),
        new("Progress", "Missions in game", TotalMissionsOffset, StatKind.Int),
        new("Progress", "Unique jumps found", UniqueJumpsFoundOffset, StatKind.Int),
        new("Progress", "Unique jumps in game", TotalUniqueJumpsOffset, StatKind.Int),
        new("Progress", "Rampages passed", KillFrenziesPassedOffset, StatKind.Int),
        new("Progress", "Rampages in game", TotalKillFrenziesOffset, StatKind.Int),
        new("Progress", "Properties owned", NumPropertyOwnedOffset, StatKind.Int),
    ];

    public static IReadOnlyList<string> PedTypeNames { get; } =
    [
        "Player 1", "Player 2", "Player 3", "Player 4", "Civilian (male)", "Civilian (female)", "Cops",
        "Cubans", "Haitians", "Streetwannabes", "Diaz gang", "Security guards", "Biker gang",
        "Vercetti gang", "Golfers", "Gang 9", "Emergency", "Firemen", "Criminals", "Unused",
        "Prostitutes", "Special", "Unused 2",
    ];

    public float Read(StatField field) => field.Kind == StatKind.Float ? GetFloat(field.Offset) : GetInt(field.Offset);

    public void Write(StatField field, double value)
    {
        if (field.Kind == StatKind.Float)
        {
            SetFloat(field.Offset, (float)value);
        }
        else
        {
            SetInt(field.Offset, (int)Math.Clamp(Math.Round(value), int.MinValue, int.MaxValue));
        }
    }

    public int GetInt32(int offset) => GetInt(offset);

    public void SetInt32(int offset, int value) => SetInt(offset, value);

    public float GetSingle(int offset) => GetFloat(offset);

    public void SetSingle(int offset, float value) => SetFloat(offset, value);

    public int GetPedsKilled(int pedType) => GetInt(PedsKilledOfThisTypeOffset + (pedType * 4));

    public void SetPedsKilled(int pedType, int value) => SetInt(PedsKilledOfThisTypeOffset + (pedType * 4), value);

    public float ProgressMade
    {
        get => GetFloat(ProgressMadeOffset);
        set => SetFloat(ProgressMadeOffset, value);
    }

    public float TotalProgressInGame => GetFloat(TotalProgressInGameOffset);

    public bool IsPropertyOwned(int index) => GetBool(PropertyOwnedOffset + index);

    public void SetPropertyOwned(int index, bool owned) => SetBool(PropertyOwnedOffset + index, owned);

    public string LastMissionPassedName
    {
        get
        {
            var span = Data.AsSpan(Absolute(543, 8), 8);
            var nul = span.IndexOf((byte)0);
            return System.Text.Encoding.ASCII.GetString(nul >= 0 ? span[..nul] : span);
        }
    }
}
