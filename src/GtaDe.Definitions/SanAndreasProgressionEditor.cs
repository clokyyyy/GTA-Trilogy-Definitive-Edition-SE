using GtaDe.Definitions;
using GtaDe.SaveFormat;

namespace GtaDe.Editing;

/// <summary>
/// Reads and writes San Andreas progression. Story missions are counter thresholds, side
/// activities are script flags (some stored as floats, some arrays), and collectibles live in the
/// stats block with matching <c>RETURNED_*</c> globals that drive the rewards.
/// </summary>
public sealed class SanAndreasProgressionEditor(SanAndreasSaveFile save)
{
    public SanAndreasSaveFile Save { get; } = save;

    private GlobalVariableSpace Globals => Save.Globals;

    private ScriptGlobal? Find(string name) => Globals.Symbols.FindGlobal(name);

    private double Read(string name, int element = 0)
    {
        if (Find(name) is not { } global || element >= global.ArrayCount)
        {
            return 0;
        }

        return global.Type == ScriptVariableType.Float ? Globals.GetFloat(global, element) : Globals.GetInt(global, element);
    }

    private void Write(string name, double value, int element = 0)
    {
        if (Find(name) is not { } global || element >= global.ArrayCount)
        {
            return;
        }

        if (global.Type == ScriptVariableType.Float)
        {
            Globals.SetFloat(global, (float)value, element);
        }
        else
        {
            Globals.SetInt(global, (int)Math.Round(value), element);
        }
    }

    // ---- flags -------------------------------------------------------------------------------

    public bool Has(SanAndreasFlag flag) => Find(flag.Variable) is { } g && flag.Element < g.ArrayCount;

    public bool IsSet(SanAndreasFlag flag) => Has(flag) && Read(flag.Variable, flag.Element) >= flag.OnValue;

    public void Set(SanAndreasFlag flag, bool value)
    {
        if (!Has(flag))
        {
            return;
        }

        var current = Read(flag.Variable, flag.Element);
        if (value)
        {
            // Keep any larger value the game wrote (job progress flags reach 2).
            Write(flag.Variable, Math.Max(current, flag.OnValue), flag.Element);
        }
        else
        {
            Write(flag.Variable, flag.OnValue > 1 ? Math.Min(current, flag.OnValue - 1) : 0, flag.Element);
        }

        if (flag == SanAndreasCatalogue.KingInExile || SanAndreasCatalogue.CatalinaRobberies.Contains(flag))
        {
            SynchroniseCatalina();
        }
    }

    public void SetAll(IEnumerable<SanAndreasFlag> flags, bool value)
    {
        foreach (var flag in flags)
        {
            Set(flag, value);
        }
    }

    public int Count(IEnumerable<SanAndreasFlag> flags) => flags.Count(IsSet);

    /// <summary>Keeps <c>CAT_COUNTER</c> equal to what Catalina's scripts would have counted.</summary>
    public void SynchroniseCatalina()
    {
        var robberies = Count(SanAndreasCatalogue.CatalinaRobberies);
        var counter = robberies;
        if (robberies == SanAndreasCatalogue.CatalinaRobberies.Count && IsSet(SanAndreasCatalogue.KingInExile))
        {
            counter++;
        }

        Write(SanAndreasCatalogue.CatalinaCounter, counter);
    }

    // ---- story strands -----------------------------------------------------------------------

    public bool Has(StoryStrand strand) => Find(strand.Counter) is not null;

    public int GetCounter(StoryStrand strand) => (int)Read(strand.Counter);

    public bool IsComplete(StoryStrand strand, StrandMission mission) =>
        Has(strand) && GetCounter(strand) > mission.Launch;

    /// <summary>
    /// Passing a mission moves the strand at least past it; un-passing rewinds the strand to it,
    /// which also un-passes everything later in the same strand, exactly as the game would see it.
    /// </summary>
    public void SetComplete(StoryStrand strand, StrandMission mission, bool value)
    {
        if (!Has(strand))
        {
            return;
        }

        var current = GetCounter(strand);
        Write(strand.Counter, value ? Math.Max(current, mission.Launch + 1) : Math.Min(current, mission.Launch));
    }

    public void SetStrandComplete(StoryStrand strand, bool value) =>
        Write(strand.Counter, value ? Math.Max(GetCounter(strand), strand.CompleteValue) : 0);

    public int CountComplete(StoryStrand strand) => strand.Missions.Count(m => IsComplete(strand, m));

    // ---- numbers -----------------------------------------------------------------------------

    public bool HasVariable(string name) => Find(name) is not null;

    public double GetVariable(string name, int element = 0) => Read(name, element);

    public void SetVariable(string name, double value, int element = 0) => Write(name, value, element);

    // ---- collectibles ------------------------------------------------------------------------

    public int TagsSprayed => Save.Tags.IsAvailable ? Save.Tags.SprayedCount : Save.Stats.GetIntStat(SanAndreasCatalogue.TagStat);

    /// <summary>Sprays the first <paramref name="count"/> tags and keeps the stat and reward global in step.</summary>
    public void SetTags(int count)
    {
        count = Math.Clamp(count, 0, SanAndreasTagsBlock.TotalTags);
        if (Save.Tags.IsAvailable)
        {
            Save.Tags.SetSprayedCount(count);
        }

        Save.Stats.SetIntStat(SanAndreasCatalogue.TagStat, count);
        if (count >= SanAndreasTagsBlock.TotalTags)
        {
            Write("RETURNED_TAGS", SanAndreasTagsBlock.TotalTags);
        }
    }

    public int GetCollected(int stat) => Save.Stats.GetIntStat(stat);

    public void SetCollected((string Name, int Stat, int TotalStat, int Total, string Note) item, int value)
    {
        value = Math.Clamp(value, 0, item.Total);
        Save.Stats.SetIntStat(item.Stat, value);
        if (Save.Stats.GetIntStat(item.TotalStat) < item.Total)
        {
            Save.Stats.SetIntStat(item.TotalStat, item.Total);
        }

        var returned = item.Stat switch
        {
            231 => "RETURNED_SNAPSHOTS",
            241 => "RETURNED_SHOEHORSES",
            243 => "RETURNED_OYSTERS",
            _ => null,
        };
        if (returned is not null && value >= item.Total)
        {
            Write(returned, item.Total);
        }
    }

    // ---- summary -----------------------------------------------------------------------------

    public (int Done, int Total) StoryProgress()
    {
        var done = 0;
        var total = 0;
        foreach (var strand in SanAndreasCatalogue.Strands.Where(Has))
        {
            total += strand.Missions.Count;
            done += CountComplete(strand);
        }

        var catalina = SanAndreasCatalogue.CatalinaRobberies.Append(SanAndreasCatalogue.KingInExile).Where(Has).ToList();
        total += catalina.Count;
        done += Count(catalina);
        return (done, total);
    }
}
