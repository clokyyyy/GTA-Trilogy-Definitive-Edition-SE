namespace GtaDe.SaveFormat.Tests;

public class SanAndreasFormatTests
{
    internal const string Sample = "GTASAsf1.sav";

    internal static SanAndreasSaveFile Load() => SanAndreasSaveFile.Load(SampleSaves.Path(Sample));

    [Fact]
    public void DetectsSanAndreasAndParses()
    {
        var generic = DeSaveFile.Load(SampleSaves.Path(Sample));

        var save = Assert.IsType<SanAndreasSaveFile>(generic);
        Assert.Equal(GameKind.SanAndreas, save.Game);
        Assert.True(save.SealWasValid);
        Assert.True(save.Symbols.Globals.Count > 1000);
    }

    [Fact]
    public void RoundTripsByteIdentically()
    {
        var bytes = SampleSaves.Bytes(Sample);
        var save = SanAndreasSaveFile.Parse(bytes);

        Assert.Equal(bytes, save.ToBytes());
    }

    [Fact]
    public void LocatesEveryNamedSection()
    {
        var layout = Load().Layout;

        Assert.Equal(SaveLayoutSpec.SanAndreasBlocks.Count, layout.Blocks.Count);
        Assert.Equal(0x38, layout[SaveBlockKind.SimpleVariables].Offset);
        Assert.Equal(0x3271A, layout[SaveBlockKind.Scripts].DataOffset);
        Assert.Equal(0x5414D, layout[SaveBlockKind.Stats].DataOffset);
        Assert.Equal(0x5FDA6, layout[SaveBlockKind.Briefs].Offset);

        for (var i = 1; i < layout.Blocks.Count; i++)
        {
            Assert.Equal(layout.Blocks[i - 1].EndOffset, layout.Blocks[i].Offset);
        }
    }

    [Fact]
    public void ReadsKnownValuesFromTheHundredPercentSave()
    {
        var save = Load();

        Assert.Equal(5_015_918, save.PlayerInfo.Money);
        Assert.Equal(save.PlayerInfo.Money, save.PlayerInfo.VisibleMoney);
        Assert.Equal(220, save.PlayerInfo.MaxHealth);
        Assert.Equal(150, save.PlayerInfo.MaxArmour);

        Assert.True(save.PlayerPed.IsAvailable);
        Assert.Equal(220f, save.PlayerPed.Health);
        Assert.Equal(SanAndreasWeapon.Knife, save.PlayerPed.Weapons[1].Type);
        Assert.Equal(SanAndreasWeapon.SilencedPistol, save.PlayerPed.Weapons[2].Type);
        Assert.Equal(SanAndreasWeapon.Minigun, save.PlayerPed.Weapons[7].Type);
        Assert.Equal(SanAndreasWeapon.Detonator, save.PlayerPed.Weapons[12].Type);

        Assert.Equal(143, save.Stats.GetIntStat(147));
        Assert.Equal(100, save.Tags.Count);
        Assert.Equal(100, save.Tags.SprayedCount);

        Assert.Equal(12, save.SimpleVars.Hour);
        Assert.Equal(22, save.SimpleVars.Minute);
        Assert.Equal(SanAndreasWeather.SunnyLosSantos, save.SimpleVars.CurrentWeather);
        Assert.Equal(SanAndreasSimpleVars.WeatherNotForced, save.SimpleVars.ForcedWeather);
    }

    [Fact]
    public void EditsSurviveAReparse()
    {
        var save = Load();

        save.PlayerInfo.SetMoney(1234);
        save.PlayerPed.Armour = 150;
        save.PlayerPed.Weapons[5].Set(SanAndreasWeapon.Ak47, 400);
        save.Stats.SetFloatStat(23, 1000f);
        save.Stats.SetIntStat(160, 999);
        save.Tags.SetSprayedCount(40);
        save.SimpleVars.Hour = 3;
        save.SimpleVars.SetWeather(SanAndreasWeather.FoggySanFierro);

        var reloaded = SanAndreasSaveFile.Parse(save.ToBytes());

        Assert.True(reloaded.SealWasValid);
        Assert.Equal(1234, reloaded.PlayerInfo.Money);
        Assert.Equal(1234, reloaded.PlayerInfo.VisibleMoney);
        Assert.Equal(150f, reloaded.PlayerPed.Armour);
        Assert.Equal(SanAndreasWeapon.Ak47, reloaded.PlayerPed.Weapons[5].Type);
        Assert.Equal(400, reloaded.PlayerPed.Weapons[5].AmmoTotal);
        Assert.Equal(1000f, reloaded.Stats.GetFloatStat(23));
        Assert.Equal(999, reloaded.Stats.GetIntStat(160));
        Assert.Equal(40, reloaded.Tags.SprayedCount);
        Assert.Equal(3, reloaded.SimpleVars.Hour);
        Assert.Equal(SanAndreasWeather.FoggySanFierro, reloaded.SimpleVars.CurrentWeather);
    }

    [Fact]
    public void DescribesSanAndreasOffsets()
    {
        var save = Load();

        Assert.Equal("Scripts", SaveMap.Describe(save, save.Globals.BaseOffset + 4).Region);
        Assert.Equal("SimpleVars", SaveMap.Describe(save, 0x78).Region);
        Assert.Equal("Game clock hour", SaveMap.Describe(save, 0x78).Detail);
        Assert.Equal("Tags", SaveMap.Describe(save, save.Layout[SaveBlockKind.Tags].DataOffset + 10).Region);
    }
}
