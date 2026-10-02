using GtaDe.Definitions;

namespace GtaDe.SaveFormat.Tests;

/// <summary>
/// Guards the content catalogue against typos and against names that simply do not exist in the
/// game. Every variable the catalogue names has to be present in the symbol table the game itself
/// embedded in the save, otherwise the editor would silently do nothing when the user toggles it.
/// </summary>
public class CatalogueTests
{
    private static readonly ScriptSymbolTable Symbols = SampleSaves.Load("GTA3sf1.sav").Symbols;

    private static void AssertGlobalExists(string name)
    {
        Assert.True(
            Symbols.FindGlobal(name) is not null,
            $"The catalogue refers to '{name}', which is not in the save's symbol table.");
    }

    [Fact]
    public void EveryStoryMissionFlagExists()
    {
        foreach (var strand in Gta3Catalogue.Strands)
        {
            foreach (var mission in strand.Missions)
            {
                AssertGlobalExists(mission.Flag);
            }

            if (strand.CompletionFlag is { } completion)
            {
                AssertGlobalExists(completion);
            }
        }
    }

    [Fact]
    public void EveryRampageAndJumpFlagExists()
    {
        foreach (var rampage in Gta3Catalogue.Rampages)
        {
            AssertGlobalExists(rampage.Flag);
        }

        foreach (var jump in Gta3Catalogue.UniqueJumps)
        {
            AssertGlobalExists(jump.Flag);
        }
    }

    [Fact]
    public void EveryChallengeFlagExists()
    {
        foreach (var entry in Gta3Catalogue.OffRoadChallenges
            .Concat(Gta3Catalogue.RemoteControlChallenges)
            .Concat(Gta3Catalogue.UnconfirmedScriptMissions))
        {
            AssertGlobalExists(entry.Flag);
        }

        foreach (var (_, flag) in Gta3Catalogue.IslandCompletionFlags)
        {
            AssertGlobalExists(flag);
        }
    }

    [Fact]
    public void EveryImportExportSlotExists()
    {
        foreach (var garage in Gta3Catalogue.ImportExportGarages)
        {
            AssertGlobalExists(garage.FilledCountVariable);

            foreach (var vehicle in garage.Vehicles)
            {
                AssertGlobalExists($"{garage.SlotPrefix}{vehicle.Slot}");
            }
        }
    }

    [Fact]
    public void TheCatalogueCoversTheWholeGame()
    {
        Assert.Equal(Gta3Catalogue.TotalRampages, Gta3Catalogue.Rampages.Count);
        Assert.Equal(Gta3Catalogue.TotalUniqueJumps, Gta3Catalogue.UniqueJumps.Count);

        var numbers = Gta3Catalogue.Rampages.Select(r => r.Number).ToArray();
        Assert.Equal(Enumerable.Range(1, Gta3Catalogue.TotalRampages), numbers);

        var jumps = Gta3Catalogue.UniqueJumps.Select(j => j.Number).ToArray();
        Assert.Equal(Enumerable.Range(1, Gta3Catalogue.TotalUniqueJumps), jumps);
    }

    [Fact]
    public void IdentifiersAreUnique()
    {
        var strandIds = Gta3Catalogue.Strands.Select(s => s.Id).ToArray();
        Assert.Equal(strandIds.Length, strandIds.Distinct().Count());

        var flags = Gta3Catalogue.Strands.SelectMany(s => s.Missions).Select(m => m.Flag).ToArray();
        Assert.Equal(flags.Length, flags.Distinct().Count());
    }

    /// <summary>
    /// Counts the catalogue derives from the individual flags have to equal the running totals the
    /// game maintains independently. This is what exposed <c>RAMPAGE_nn</c> as a set of script
    /// object handles rather than completion flags: it reported twenty completed Rampages in a
    /// save where the game's own total said fifteen.
    /// </summary>
    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void DerivedCountsAgreeWithTheGamesOwnTotals(string name)
    {
        var save = SampleSaves.Load(name);

        var rampages = Gta3Catalogue.Rampages.Count(r => save.Globals.GetFlag(r.Flag));
        Assert.Equal(save.Globals.GetInt("TOTAL_RAMPAGES_PASSED"), rampages);
        Assert.Equal(save.Stats.KillFrenziesPassed, rampages);

        var jumps = Gta3Catalogue.UniqueJumps.Count(j => save.Globals.GetFlag(j.Flag));
        Assert.Equal(save.Globals.GetInt("TOTAL_COMPLETED_USJ"), jumps);
        Assert.Equal(save.Stats.UniqueJumpsFound, jumps);

        var missions = Gta3Catalogue.Strands
            .SelectMany(s => s.Missions)
            .Count(m => m.CountsTowardTotal && save.Globals.GetFlag(m.Flag));
        Assert.Equal(save.Stats.MissionsPassed, missions);
    }
}
