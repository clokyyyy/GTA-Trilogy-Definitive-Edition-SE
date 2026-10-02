using GtaDe.Definitions;
using GtaDe.Editing;

namespace GtaDe.SaveFormat.Tests;

public class ProgressionEditorTests
{
    private static (Gta3SaveFile Save, ProgressionEditor Editor) Open()
    {
        var save = SampleSaves.Load("GTA3sf1.sav");
        return (save, new ProgressionEditor(save));
    }

    [Fact]
    public void CompletingARampageUpdatesTheFlagTheTotalAndTheReward()
    {
        var (save, editor) = Open();
        var rampage = Gta3Catalogue.Rampages.First(r => !new ProgressionEditor(save).IsRampageComplete(r));

        editor.SetRampageComplete(rampage, true);

        Assert.True(editor.IsRampageComplete(rampage));

        var expected = Gta3Catalogue.Rampages.Count(r => save.Globals.GetFlag(r.Flag));
        Assert.Equal(expected, save.Globals.GetInt("TOTAL_RAMPAGES_PASSED"));
        Assert.Equal(expected * 5000, save.Globals.GetInt("RAMPAGE_REWARD"));
        Assert.Equal(expected, save.Stats.KillFrenziesPassed);
    }

    [Fact]
    public void CompletingEverySideActivityProducesTheGamesMaximums()
    {
        var (save, editor) = Open();

        editor.SetAllRampages(true);
        editor.SetAllUniqueJumps(true);

        Assert.Equal(Gta3Catalogue.TotalRampages, save.Globals.GetInt("TOTAL_RAMPAGES_PASSED"));
        Assert.Equal(Gta3Catalogue.TotalRampages, save.Stats.KillFrenziesPassed);
        Assert.Equal(save.Stats.KillFrenziesTotal, save.Stats.KillFrenziesPassed);

        Assert.Equal(Gta3Catalogue.TotalUniqueJumps, save.Globals.GetInt("TOTAL_COMPLETED_USJ"));
        Assert.Equal(Gta3Catalogue.TotalUniqueJumps, save.Stats.UniqueJumpsFound);
        Assert.Equal(save.Stats.UniqueJumpsTotal, save.Stats.UniqueJumpsFound);
    }

    [Fact]
    public void ClearingRampagesRollsTheTotalsBack()
    {
        var (save, editor) = Open();

        editor.SetAllRampages(true);
        editor.SetAllRampages(false);

        Assert.Equal(0, save.Globals.GetInt("TOTAL_RAMPAGES_PASSED"));
        Assert.Equal(0, save.Globals.GetInt("RAMPAGE_REWARD"));
        Assert.Equal(0, save.Stats.KillFrenziesPassed);
        Assert.All(Gta3Catalogue.Rampages, r => Assert.False(editor.IsRampageComplete(r)));
    }

    [Fact]
    public void CompletingAStrandSetsItsAllPassedFlag()
    {
        var (save, editor) = Open();
        var strand = Gta3Catalogue.Strands.First(s => s.Id == "luigi");

        editor.SetStrandComplete(strand, true);

        Assert.True(save.Globals.GetFlag(strand.CompletionFlag!));
        Assert.All(strand.Missions, m => Assert.True(editor.IsMissionComplete(m)));
    }

    [Fact]
    public void UncompletingOneMissionClearsTheStrandFlag()
    {
        var (save, editor) = Open();
        var strand = Gta3Catalogue.Strands.First(s => s.Id == "luigi");

        editor.SetStrandComplete(strand, true);
        editor.SetMissionComplete(strand, strand.Missions[2], false);

        Assert.False(save.Globals.GetFlag(strand.CompletionFlag!));
    }

    [Fact]
    public void MissionsPassedCountsEveryMissionExceptTheIntro()
    {
        var (save, editor) = Open();

        foreach (var strand in Gta3Catalogue.Strands)
        {
            editor.SetStrandComplete(strand, true);
        }

        Assert.True(save.Stats.MissionsPassed <= save.Stats.MissionsGiven);
        Assert.Equal(
            Gta3Catalogue.Strands.SelectMany(s => s.Missions).Count(m => m.CountsTowardTotal),
            save.Stats.MissionsPassed);
    }

    /// <summary>
    /// The strongest check in the suite. Every sample was written by the game itself, so the game
    /// already considers it consistent. If rebuilding the totals from the individual flags changes
    /// a single byte, the editor's model of how progress is recorded is wrong.
    /// </summary>
    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void SynchronisingAGameWrittenSaveChangesNothing(string name)
    {
        var save = SampleSaves.Load(name);
        var before = (byte[])save.Data.Clone();

        new ProgressionEditor(save).SynchroniseEverything();

        Assert.Equal(before, save.Data);
    }
}
