using GtaDe.Definitions;
using GtaDe.SaveFormat;

namespace GtaDe.Editing;

/// <summary>
/// Applies completion changes the way the game's own script would have, so the result is a save
/// the game treats as genuine.
/// </summary>
/// <remarks>
/// <para>
/// Flipping a single completion flag is almost never enough. Comparing a real save against the
/// same save after one Rampage showed the script touches three separate things, and the HUD and
/// mission unlocks read all three:
/// </para>
/// <list type="number">
///   <item><description>the individual completion flag,</description></item>
///   <item><description>the script's running total and any cash reward it tracks,</description></item>
///   <item><description>the engine's Stats block, which is what the stats menu displays.</description></item>
/// </list>
/// <para>
/// Everything here goes through that rule. Failure counters and per-attempt bookkeeping are left
/// alone on purpose: they are history, not progress, and rewriting them would make the save look
/// tampered with for no benefit.
/// </para>
/// </remarks>
public sealed class ProgressionEditor(Gta3SaveFile save)
{
    /// <summary>
    /// Cash step the script uses for both Rampage and Unique Jump rewards.
    /// </summary>
    /// <remarks>
    /// Derived from two game-written saves: at 15 Rampages <c>RAMPAGE_REWARD</c> held 75000 and at
    /// 19 it held 95000, so the script stores 5000 per Rampage completed. The jump reward is
    /// offset by one step: 13 jumps gave 70000 and 19 gave 100000, which is 5000 per jump
    /// <em>plus one</em>, so that variable holds the value of the next payout rather than the
    /// running total.
    /// </remarks>
    private const int RewardStep = 5000;

    private readonly Gta3SaveFile _save = save;

    private GlobalVariableSpace Globals => _save.Globals;

    public bool IsRampageComplete(RampageEntry rampage) => Globals.GetFlag(rampage.Flag);

    public bool IsUniqueJumpComplete(UniqueJumpEntry jump) => Globals.GetFlag(jump.Flag);

    /// <summary>
    /// Marks a Rampage done or not done, then rebuilds every total that depends on it.
    /// </summary>
    public void SetRampageComplete(RampageEntry rampage, bool complete)
    {
        Globals.SetFlag(rampage.Flag, complete);
        SynchroniseRampageTotals();
    }

    /// <summary>
    /// Marks a Unique Stunt Jump done or not done, then rebuilds every total that depends on it.
    /// </summary>
    public void SetUniqueJumpComplete(UniqueJumpEntry jump, bool complete)
    {
        Globals.SetFlag(jump.Flag, complete);
        SynchroniseUniqueJumpTotals();
    }

    public void SetAllRampages(bool complete)
    {
        foreach (var rampage in Gta3Catalogue.Rampages)
        {
            Globals.SetFlag(rampage.Flag, complete);
        }

        SynchroniseRampageTotals();
    }

    public void SetAllUniqueJumps(bool complete)
    {
        foreach (var jump in Gta3Catalogue.UniqueJumps)
        {
            Globals.SetFlag(jump.Flag, complete);
        }

        SynchroniseUniqueJumpTotals();
    }

    /// <summary>
    /// Recomputes the Rampage totals from the individual flags and mirrors the count into the
    /// Stats block the HUD reads.
    /// </summary>
    public int SynchroniseRampageTotals()
    {
        var passed = Gta3Catalogue.Rampages.Count(r => Globals.GetFlag(r.Flag));

        Globals.SetInt("TOTAL_RAMPAGES_PASSED", passed);

        if (Globals.Has("RAMPAGE_REWARD"))
        {
            Globals.SetInt("RAMPAGE_REWARD", passed * RewardStep);
        }

        _save.Stats.KillFrenziesPassed = passed;
        return passed;
    }

    /// <summary>
    /// Recomputes the Unique Stunt Jump totals from the individual flags and mirrors the count
    /// into the Stats block.
    /// </summary>
    public int SynchroniseUniqueJumpTotals()
    {
        var passed = Gta3Catalogue.UniqueJumps.Count(j => Globals.GetFlag(j.Flag));

        Globals.SetInt("TOTAL_COMPLETED_USJ", passed);

        if (Globals.Has("CASH_REWARD_USJ"))
        {
            Globals.SetInt("CASH_REWARD_USJ", (passed + 1) * RewardStep);
        }

        _save.Stats.UniqueJumpsFound = passed;
        return passed;
    }

    public bool IsMissionComplete(MissionEntry mission) => Globals.GetFlag(mission.Flag);

    /// <summary>
    /// Marks a story mission done or not done and refreshes the strand's "all passed" flag, which
    /// is what actually unlocks later contacts.
    /// </summary>
    public void SetMissionComplete(MissionStrand strand, MissionEntry mission, bool complete)
    {
        Globals.SetFlag(mission.Flag, complete);
        SynchroniseStrand(strand);
        SynchroniseMissionTotals();
    }

    public void SetStrandComplete(MissionStrand strand, bool complete)
    {
        foreach (var mission in strand.Missions)
        {
            Globals.SetFlag(mission.Flag, complete);
        }

        SynchroniseStrand(strand);
        SynchroniseMissionTotals();
    }

    /// <summary>Keeps a strand's aggregate flag consistent with its individual missions.</summary>
    public void SynchroniseStrand(MissionStrand strand)
    {
        if (strand.CompletionFlag is not { } flag || !Globals.Has(flag))
        {
            return;
        }

        Globals.SetFlag(flag, strand.Missions.All(m => Globals.GetFlag(m.Flag)));
    }

    /// <summary>
    /// Rebuilds the mission counters in the Stats block from the story flags, so the stats menu
    /// and the save-game percentage agree with what the script believes.
    /// </summary>
    public int SynchroniseMissionTotals()
    {
        var passed = Gta3Catalogue.Strands
            .SelectMany(s => s.Missions)
            .Count(m => m.CountsTowardTotal && Globals.GetFlag(m.Flag));

        _save.Stats.MissionsPassed = passed;

        if (_save.Stats.MissionsGiven < passed)
        {
            _save.Stats.MissionsGiven = passed;
        }

        return passed;
    }

    /// <summary>
    /// Rebuilds every derived total in one pass. Run this after a bulk edit, or to repair a save
    /// that another tool left inconsistent.
    /// </summary>
    public void SynchroniseEverything()
    {
        foreach (var strand in Gta3Catalogue.Strands)
        {
            SynchroniseStrand(strand);
        }

        SynchroniseMissionTotals();
        SynchroniseRampageTotals();
        SynchroniseUniqueJumpTotals();
    }
}
