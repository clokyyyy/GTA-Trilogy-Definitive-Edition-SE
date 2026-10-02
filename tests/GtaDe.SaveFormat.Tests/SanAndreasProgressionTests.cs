using GtaDe.Definitions;
using GtaDe.Editing;

namespace GtaDe.SaveFormat.Tests;

public class SanAndreasProgressionTests
{
    private static SanAndreasProgressionEditor Editor() => new(SanAndreasFormatTests.Load());

    private static IEnumerable<SanAndreasFlag> AllFlags =>
        SanAndreasCatalogue.ActivityTabs.SelectMany(t => t.Entries)
            .Concat(SanAndreasCatalogue.Safehouses)
            .Concat(SanAndreasCatalogue.CatalinaRobberies)
            .Append(SanAndreasCatalogue.KingInExile);

    [Fact]
    public void Every_catalogued_global_exists_in_the_save()
    {
        var editor = Editor();
        var missing = AllFlags.Where(f => !editor.Has(f)).Select(f => f.Variable)
            .Concat(SanAndreasCatalogue.Strands.Where(s => !editor.Has(s)).Select(s => s.Counter))
            .ToList();
        Assert.Empty(missing);
        Assert.True(editor.HasVariable(SanAndreasCatalogue.CatalinaCounter));
    }

    [Fact]
    public void Hundred_percent_save_reports_the_story_complete()
    {
        var editor = Editor();
        var (done, total) = editor.StoryProgress();
        Assert.Equal(total, done);
        Assert.True(total > 90);
        foreach (var strand in SanAndreasCatalogue.Strands)
        {
            Assert.Equal(strand.CompleteValue, editor.GetCounter(strand));
        }

        Assert.Equal(5, (int)editor.GetVariable(SanAndreasCatalogue.CatalinaCounter));
    }

    [Fact]
    public void Hundred_percent_save_has_side_activities_and_houses()
    {
        var editor = Editor();
        foreach (var tab in SanAndreasCatalogue.ActivityTabs.Where(t => t.Id is "jobs" or "schools"))
        {
            Assert.All(tab.Entries.Where(e => e.Variable != "DONE_BURGLARY_PROGRESS" && !e.Name.EndsWith("gold") && !e.Name.EndsWith("gold reward")), e => Assert.True(editor.IsSet(e), e.Name));
        }

        Assert.All(SanAndreasCatalogue.Safehouses, h => Assert.True(editor.IsSet(h), h.Name));
    }

    [Fact]
    public void Unpassing_a_mission_rewinds_its_strand_and_survives_a_reparse()
    {
        var editor = Editor();
        var sweet = SanAndreasCatalogue.Strands.Single(s => s.Id == "sweet");
        var driveThru = sweet.Missions[2];
        editor.SetComplete(sweet, driveThru, false);
        Assert.Equal(2, editor.GetCounter(sweet));
        Assert.False(editor.IsComplete(sweet, sweet.Missions[^1]));
        Assert.True(editor.IsComplete(sweet, sweet.Missions[1]));

        var reparsed = new SanAndreasProgressionEditor(SanAndreasSaveFile.Parse(editor.Save.ToBytes()));
        Assert.Equal(2, reparsed.GetCounter(sweet));
        reparsed.SetComplete(sweet, driveThru, true);
        Assert.Equal(3, reparsed.GetCounter(sweet));
    }

    [Fact]
    public void Catalina_counter_follows_the_robberies()
    {
        var editor = Editor();
        editor.Set(SanAndreasCatalogue.CatalinaRobberies[0], false);
        Assert.Equal(3, (int)editor.GetVariable(SanAndreasCatalogue.CatalinaCounter));
        Assert.True(editor.IsSet(SanAndreasCatalogue.KingInExile));

        editor.Set(SanAndreasCatalogue.KingInExile, false);
        Assert.Equal(1, (int)editor.GetVariable(SanAndreasCatalogue.KingInExile.Variable));

        editor.SetAll(SanAndreasCatalogue.CatalinaRobberies.Append(SanAndreasCatalogue.KingInExile), true);
        Assert.Equal(5, (int)editor.GetVariable(SanAndreasCatalogue.CatalinaCounter));
    }

    [Fact]
    public void Float_flags_and_array_elements_round_trip()
    {
        var editor = Editor();
        var grav = new SanAndreasFlag("Grav", "FLAG_GRAV_PASSED_1STIME");
        editor.Set(grav, false);
        Assert.False(editor.IsSet(grav));
        editor.Set(grav, true);
        Assert.Equal(1.0, editor.GetVariable(grav.Variable));

        var house = SanAndreasCatalogue.Safehouses[0];
        editor.Set(house, false);
        Assert.False(editor.IsSet(house));
        Assert.True(editor.IsSet(SanAndreasCatalogue.Safehouses[1]));
    }

    [Fact]
    public void Collectible_setters_keep_stats_in_step()
    {
        var editor = Editor();
        editor.SetTags(10);
        Assert.Equal(10, editor.TagsSprayed);
        Assert.Equal(10, editor.Save.Stats.GetIntStat(SanAndreasCatalogue.TagStat));

        var oysters = SanAndreasCatalogue.Collectibles.Single(c => c.Name == "Oysters");
        editor.SetCollected(oysters, 12);
        Assert.Equal(12, editor.GetCollected(oysters.Stat));
    }
}
