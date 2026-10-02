using System.Text;

namespace GtaDe.SaveFormat;

/// <summary>
/// The Stats block: everything the in-game stats menu shows.
/// </summary>
/// <remarks>
/// <para>
/// GTA III DE keeps the classic 420-byte <c>CStats</c> layout and appends 48 bytes of its own.
/// The mapping below was confirmed against three real saves:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <see cref="PeopleWastedByPlayer"/> equals the sum of <see cref="GetPedTypeKills"/> across all
/// 23 ped types, in every sample.
/// </description></item>
/// <item><description>
/// <c>LivesSavedWithAmbulance</c> is 15 while <c>HighestLevelAmbulanceMission</c> is 5, and
/// 1+2+3+4+5 = 15, exactly as the Paramedic mission awards lives.
/// </description></item>
/// <item><description>
/// <see cref="LastMissionPassed"/> at 0x19C reads <c>LM5</c>, matching the GXT key in the file
/// header, which pins the end of the classic struct.
/// </description></item>
/// </list>
/// </remarks>
public sealed class StatsBlock(byte[] data, SaveBlock block) : BlockAccessor(data, block)
{
    /// <summary>Number of ped types tracked by the kill breakdown.</summary>
    public const int PedTypeCount = 23;

    /// <summary>Number of saved fastest times and highest scores.</summary>
    public const int RecordSlots = 16;

    /// <summary>Byte length of the classic GTA III stats struct.</summary>
    public const int ClassicLength = 0x1A4;

    private const int PedTypeKillsOffset = 0x010;
    private const int FastestTimesOffset = 0x114;
    private const int HighestScoresOffset = 0x154;
    private const int LastMissionOffset = 0x19C;
    private const int LastMissionLength = 8;

    public int PeopleWastedByPlayer { get => GetInt(0x000); set => SetInt(0x000, value); }

    public int PeopleWastedByOthers { get => GetInt(0x004); set => SetInt(0x004, value); }

    public int CarsExploded { get => GetInt(0x008); set => SetInt(0x008, value); }

    public int RoundsFiredByPlayer { get => GetInt(0x00C); set => SetInt(0x00C, value); }

    public int HelicoptersDestroyed { get => GetInt(0x06C); set => SetInt(0x06C, value); }

    /// <summary>Completion points earned, out of <see cref="TotalProgressInGame"/>.</summary>
    public int ProgressMade { get => GetInt(0x070); set => SetInt(0x070, value); }

    /// <summary>Completion points available in the whole game; 154 in GTA III.</summary>
    public int TotalProgressInGame { get => GetInt(0x074); set => SetInt(0x074, value); }

    public int KgsOfExplosivesUsed { get => GetInt(0x078); set => SetInt(0x078, value); }

    public int InstantHitsFiredByPlayer { get => GetInt(0x07C); set => SetInt(0x07C, value); }

    public int InstantHitsHitByPlayer { get => GetInt(0x080); set => SetInt(0x080, value); }

    public int CarsCrushed { get => GetInt(0x084); set => SetInt(0x084, value); }

    public int HeadsPopped { get => GetInt(0x088); set => SetInt(0x088, value); }

    public int TimesArrested { get => GetInt(0x08C); set => SetInt(0x08C, value); }

    public int TimesDied { get => GetInt(0x090); set => SetInt(0x090, value); }

    public int DaysPassed { get => GetInt(0x094); set => SetInt(0x094, value); }

    public int MillimetresOfRain { get => GetInt(0x098); set => SetInt(0x098, value); }

    public float MaximumJumpDistance { get => GetFloat(0x09C); set => SetFloat(0x09C, value); }

    public float MaximumJumpHeight { get => GetFloat(0x0A0); set => SetFloat(0x0A0, value); }

    public int MaximumJumpFlips { get => GetInt(0x0A4); set => SetInt(0x0A4, value); }

    public int MaximumJumpSpins { get => GetInt(0x0A8); set => SetInt(0x0A8, value); }

    public int BestStuntJump { get => GetInt(0x0AC); set => SetInt(0x0AC, value); }

    public int UniqueJumpsFound { get => GetInt(0x0B0); set => SetInt(0x0B0, value); }

    public int UniqueJumpsTotal { get => GetInt(0x0B4); set => SetInt(0x0B4, value); }

    public int MissionsGiven { get => GetInt(0x0B8); set => SetInt(0x0B8, value); }

    public int MissionsPassed { get => GetInt(0x0BC); set => SetInt(0x0BC, value); }

    public int TaxiPassengersDroppedOff { get => GetInt(0x0C0); set => SetInt(0x0C0, value); }

    public int MoneyMadeWithTaxi { get => GetInt(0x0C4); set => SetInt(0x0C4, value); }

    public bool IndustrialPassed { get => GetInt(0x0C8) != 0; set => SetInt(0x0C8, value ? 1 : 0); }

    public bool CommercialPassed { get => GetInt(0x0CC) != 0; set => SetInt(0x0CC, value ? 1 : 0); }

    public bool SuburbanPassed { get => GetInt(0x0D0) != 0; set => SetInt(0x0D0, value ? 1 : 0); }

    public bool PamphletMissionPassed { get => GetInt(0x0D4) != 0; set => SetInt(0x0D4, value ? 1 : 0); }

    public float DistanceTravelledOnFoot { get => GetFloat(0x0D8); set => SetFloat(0x0D8, value); }

    public float DistanceTravelledByCar { get => GetFloat(0x0DC); set => SetFloat(0x0DC, value); }

    /// <summary>Best time on the first off-road time trial, in milliseconds; 0 when unset.</summary>
    public int Record4x4One { get => GetInt(0x0E0); set => SetInt(0x0E0, value); }

    public int Record4x4Two { get => GetInt(0x0E4); set => SetInt(0x0E4, value); }

    public int Record4x4Three { get => GetInt(0x0E8); set => SetInt(0x0E8, value); }

    public int Record4x4Mayhem { get => GetInt(0x0EC); set => SetInt(0x0EC, value); }

    public int LivesSavedWithAmbulance { get => GetInt(0x0F0); set => SetInt(0x0F0, value); }

    public int CriminalsCaught { get => GetInt(0x0F4); set => SetInt(0x0F4, value); }

    public int HighestLevelAmbulanceMission { get => GetInt(0x0F8); set => SetInt(0x0F8, value); }

    public int FiresExtinguished { get => GetInt(0x0FC); set => SetInt(0x0FC, value); }

    public int LongestFlightInDodo { get => GetInt(0x100); set => SetInt(0x100, value); }

    public int TimeTakenDefuseMission { get => GetInt(0x104); set => SetInt(0x104, value); }

    public int KillFrenziesPassed { get => GetInt(0x108); set => SetInt(0x108, value); }

    public int KillFrenziesTotal { get => GetInt(0x10C); set => SetInt(0x10C, value); }

    public int TotalMissions { get => GetInt(0x110); set => SetInt(0x110, value); }

    public int KillsSinceLastCheckpoint { get => GetInt(0x194); set => SetInt(0x194, value); }

    public int TotalLegitimateKills { get => GetInt(0x198); set => SetInt(0x198, value); }

    /// <summary>GXT key of the last mission passed, mirrored from the file header.</summary>
    public string LastMissionPassed
    {
        get
        {
            var span = Data.AsSpan(Absolute(LastMissionOffset, LastMissionLength), LastMissionLength);
            var nul = span.IndexOf((byte)0);
            return Encoding.ASCII.GetString(nul >= 0 ? span[..nul] : span);
        }

        set
        {
            var span = Data.AsSpan(Absolute(LastMissionOffset, LastMissionLength), LastMissionLength);
            span.Clear();
            var text = Encoding.ASCII.GetBytes(value);
            text.AsSpan(0, Math.Min(text.Length, LastMissionLength - 1)).CopyTo(span);
        }
    }

    public int GetPedTypeKills(int pedType) => GetInt(PedTypeKillsOffset + (Index(pedType, PedTypeCount) * 4));

    public void SetPedTypeKills(int pedType, int value) =>
        SetInt(PedTypeKillsOffset + (Index(pedType, PedTypeCount) * 4), value);

    public int GetFastestTime(int slot) => GetInt(FastestTimesOffset + (Index(slot, RecordSlots) * 4));

    public void SetFastestTime(int slot, int value) =>
        SetInt(FastestTimesOffset + (Index(slot, RecordSlots) * 4), value);

    public int GetHighestScore(int slot) => GetInt(HighestScoresOffset + (Index(slot, RecordSlots) * 4));

    public void SetHighestScore(int slot, int value) =>
        SetInt(HighestScoresOffset + (Index(slot, RecordSlots) * 4), value);

    /// <summary>Sum of the per-ped-type kill counters, which should equal <see cref="PeopleWastedByPlayer"/>.</summary>
    public int SumPedTypeKills()
    {
        var total = 0;
        for (var i = 0; i < PedTypeCount; i++)
        {
            total += GetPedTypeKills(i);
        }

        return total;
    }

    private static int Index(int value, int count) => value >= 0 && value < count
        ? value
        : throw new ArgumentOutOfRangeException(nameof(value), value, $"Expected 0 to {count - 1}.");
}
