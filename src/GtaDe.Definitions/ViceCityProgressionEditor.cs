using GtaDe.Definitions;
using GtaDe.SaveFormat;

namespace GtaDe.Editing;

/// <summary>
/// Vice City counterpart of <see cref="ProgressionEditor"/>: flips script flags and keeps the
/// totals, rewards and Stats figures that depend on them in step.
/// </summary>
/// <remarks>
/// Reward formulas were read off a game-written 100% save: <c>RAMPAGE_REWARD</c> holds 50 per
/// Rampage (35 → 1750) and <c>CASH_REWARD_USJ</c> holds the next payout, 100 × (jumps + 1)
/// (36 → 3700).
/// </remarks>
public sealed class ViceCityProgressionEditor(ViceCitySaveFile save)
{
    private const int RampageRewardStep = 50;
    private const int JumpRewardStep = 100;

    public ViceCitySaveFile Save { get; } = save;

    private GlobalVariableSpace Globals => Save.Globals;

    /// <summary>Whether the save's symbol table knows this global; unknown entries are hidden.</summary>
    public bool Has(FlagEntry entry) => Globals.Has(entry.Flag);

    public bool Has(CounterEntry entry) => Globals.Has(entry.Variable);

    public bool IsSet(FlagEntry entry) => Has(entry) && Globals.GetFlag(entry.Flag);

    /// <summary>Sets a flag and resynchronises whichever totals the flag feeds.</summary>
    public void Set(FlagEntry entry, bool value)
    {
        if (!Has(entry))
        {
            return;
        }

        Globals.SetFlag(entry.Flag, value);
        Synchronise(entry);
    }

    public void SetAll(IEnumerable<FlagEntry> entries, bool value)
    {
        var list = entries.Where(Has).ToList();
        foreach (var entry in list)
        {
            Globals.SetFlag(entry.Flag, value);
        }

        foreach (var entry in list.Take(1))
        {
            Synchronise(entry);
        }
    }

    public int GetCounter(CounterEntry entry) => Has(entry) ? Globals.GetInt(entry.Variable) : 0;

    public void SetCounter(CounterEntry entry, int value)
    {
        if (!Has(entry))
        {
            return;
        }

        Globals.SetInt(entry.Variable, value);
        if (entry.Variable == "NUMBER_OF_PACKAGES_COLLECTED")
        {
            Save.PlayerInfo.HiddenPackagesCollected = value;
        }
    }

    public int Count(IEnumerable<FlagEntry> entries) => entries.Count(IsSet);

    /// <summary>Sets the hidden-package count in both places the game keeps it.</summary>
    public void SetPackages(int collected)
    {
        collected = Math.Clamp(collected, 0, ViceCityCatalogue.PackageCount);
        Save.PlayerInfo.HiddenPackagesCollected = collected;
        if (Globals.Has("NUMBER_OF_PACKAGES_COLLECTED"))
        {
            Globals.SetInt("NUMBER_OF_PACKAGES_COLLECTED", collected);
        }
    }

    private void Synchronise(FlagEntry entry)
    {
        if (ViceCityCatalogue.Rampages.Contains(entry))
        {
            SynchroniseRampages();
        }
        else if (ViceCityCatalogue.UniqueJumps.Contains(entry))
        {
            SynchroniseUniqueJumps();
        }
        else if (ViceCityCatalogue.Robberies.Contains(entry))
        {
            SynchroniseRobberies();
        }
    }

    public void SynchroniseRampages()
    {
        var done = Count(ViceCityCatalogue.Rampages);
        SetIfPresent("TOTAL_RAMPAGES_PASSED", done);
        SetIfPresent("RAMPAGE_REWARD", done * RampageRewardStep);
        Save.Stats.SetInt32(ViceCityStatsBlock.KillFrenziesPassedOffset, done);
    }

    public void SynchroniseUniqueJumps()
    {
        var done = Count(ViceCityCatalogue.UniqueJumps);
        SetIfPresent("TOTAL_COMPLETED_USJ", done);
        SetIfPresent("CASH_REWARD_USJ", (done + 1) * JumpRewardStep);
        Save.Stats.SetInt32(ViceCityStatsBlock.UniqueJumpsFoundOffset, done);
    }

    public void SynchroniseRobberies()
    {
        var done = Count(ViceCityCatalogue.Robberies);
        Save.Stats.SetSingle(ViceCityStatsBlock.StoresKnockedOffOffset, done);
    }

    private void SetIfPresent(string name, int value)
    {
        if (Globals.Has(name))
        {
            Globals.SetInt(name, value);
        }
    }
}
