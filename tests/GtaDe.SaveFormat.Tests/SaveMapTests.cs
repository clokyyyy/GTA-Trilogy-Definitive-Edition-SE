namespace GtaDe.SaveFormat.Tests;

using Xunit.Abstractions;

public class SaveMapTests(ITestOutputHelper output)
{
    [Fact]
    public void ComparingTwoGameWrittenSavesNamesTheRampageVariables()
    {
        // sf2 has 15 rampages and 13 jumps; sf1 has 19 and 19. The diff between them is the
        // ground truth that proved RAMPAGE_nn_FLAG is the real completion flag, so the annotator
        // has to be able to reproduce that reading.
        var left = Gta3SaveFile.Parse(SampleSaves.Bytes("GTA3sf2.sav"));
        var right = Gta3SaveFile.Parse(SampleSaves.Bytes("GTA3sf1.sav"));

        var differences = SaveMap.Compare(left, right);

        Assert.NotEmpty(differences);

        var named = differences
            .Where(d => d.Detail is not null)
            .Select(d => d.Detail!)
            .ToList();

        foreach (var difference in differences.Take(40))
        {
            output.WriteLine($"0x{difference.Offset:X6} +{difference.Length,-4} {difference.Region,-12} {difference.Detail}");
        }

        Assert.Contains(named, d => d.Contains("RAMPAGE_", StringComparison.Ordinal) && d.EndsWith("_FLAG", StringComparison.Ordinal));
        Assert.Contains(named, d => d.Contains("TOTAL_RAMPAGES_PASSED", StringComparison.Ordinal));
        Assert.Contains(named, d => d.Contains("FLAG_USJ", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void AnIdenticalPairHasNoDifferences(string name)
    {
        var left = Gta3SaveFile.Parse(SampleSaves.Bytes(name));
        var right = Gta3SaveFile.Parse(SampleSaves.Bytes(name));

        Assert.Empty(SaveMap.Compare(left, right));
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void ASingleEditIsReportedAgainstItsOwnVariable(string name)
    {
        var left = Gta3SaveFile.Parse(SampleSaves.Bytes(name));
        var right = Gta3SaveFile.Parse(SampleSaves.Bytes(name));

        right.Globals.SetInt("TOTAL_RAMPAGES_PASSED", 7);

        var differences = SaveMap.Compare(left, right);
        var difference = Assert.Single(differences);

        Assert.Equal("Scripts", difference.Region);
        Assert.Contains("TOTAL_RAMPAGES_PASSED", difference.Detail);
        Assert.Equal(7, difference.AsInt32!.Value.After);
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void EveryOffsetInTheFileResolvesToARegion(string name)
    {
        var save = Gta3SaveFile.Parse(SampleSaves.Bytes(name));

        for (var offset = 0; offset < save.Data.Length; offset += 97)
        {
            var (region, _) = SaveMap.Describe(save, offset);
            Assert.False(string.IsNullOrWhiteSpace(region), $"No region for offset 0x{offset:X}");
        }
    }
}
