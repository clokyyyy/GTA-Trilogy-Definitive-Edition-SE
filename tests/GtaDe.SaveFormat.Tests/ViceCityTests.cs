using GtaDe.Definitions;
using GtaDe.Editing;

namespace GtaDe.SaveFormat.Tests;

public class ViceCityTests
{
    private const string Sample = "GTAVCsf8.sav";

    private static ViceCitySaveFile Load() => ViceCitySaveFile.Load(SampleSaves.Path(Sample));

    [Fact]
    public void DetectsViceCityAndParses()
    {
        var generic = DeSaveFile.Load(SampleSaves.Path(Sample));

        var save = Assert.IsType<ViceCitySaveFile>(generic);
        Assert.Equal(GameKind.ViceCity, save.Game);
        Assert.True(save.SealWasValid);
        Assert.True(save.Symbols.Globals.Count > 0);
    }

    [Fact]
    public void RoundTripsByteIdentically()
    {
        var bytes = SampleSaves.Bytes(Sample);
        var save = ViceCitySaveFile.Parse(bytes);

        Assert.Equal(bytes, save.ToBytes());
    }

    [Fact]
    public void ReadsKnownValuesFromTheHundredPercentSave()
    {
        var save = Load();

        Assert.Equal(719678, save.PlayerInfo.Money);
        Assert.Equal(100, save.PlayerInfo.HiddenPackagesCollected);
        Assert.Equal(100, save.PlayerInfo.TotalHiddenPackages);
        Assert.True(save.PlayerPed.IsAvailable);
        Assert.Equal(200f, save.PlayerPed.Health);
        Assert.Equal(200f, save.PlayerPed.Armour);
        Assert.Equal(ViceCityWeapon.Python, save.PlayerPed.Weapons[3].Type);
        Assert.Equal(ViceCityWeapon.Minigun, save.PlayerPed.Weapons[7].Type);
        Assert.Equal(35, save.Stats.GetInt32(ViceCityStatsBlock.KillFrenziesPassedOffset));
        Assert.Equal(36, save.Stats.GetInt32(ViceCityStatsBlock.UniqueJumpsFoundOffset));
        Assert.InRange(save.SimpleVars.Hour, 0, 23);
        Assert.InRange(save.SimpleVars.Minute, 0, 59);
    }

    [Fact]
    public void CatalogueFlagsExistInTheSave()
    {
        var editor = new ViceCityProgressionEditor(Load());

        Assert.All(ViceCityCatalogue.Rampages, e => Assert.True(editor.Has(e), e.Flag));
        Assert.All(ViceCityCatalogue.UniqueJumps, e => Assert.True(editor.Has(e), e.Flag));
        Assert.All(ViceCityCatalogue.Robberies, e => Assert.True(editor.Has(e), e.Flag));
        Assert.All(ViceCityCatalogue.Safehouses, e => Assert.True(editor.Has(e), e.Flag));
        Assert.All(ViceCityCatalogue.Counters, e => Assert.True(editor.Has(e), e.Variable));

        var missions = ViceCityCatalogue.Strands.SelectMany(s => s.Entries).ToList();
        Assert.True(missions.Count(editor.Has) >= missions.Count - 2);
        Assert.Equal(ViceCityCatalogue.RampageCount, editor.Count(ViceCityCatalogue.Rampages));
        Assert.Equal(ViceCityCatalogue.UniqueJumpCount, editor.Count(ViceCityCatalogue.UniqueJumps));
    }

    [Fact]
    public void ClearingARampageResynchronisesTotals()
    {
        var save = Load();
        var editor = new ViceCityProgressionEditor(save);

        editor.Set(ViceCityCatalogue.Rampages[0], false);

        Assert.False(editor.IsSet(ViceCityCatalogue.Rampages[0]));
        Assert.Equal(34, save.Globals.GetInt("TOTAL_RAMPAGES_PASSED"));
        Assert.Equal(34, save.Stats.GetInt32(ViceCityStatsBlock.KillFrenziesPassedOffset));
    }

    [Fact]
    public void ClearingAJumpResynchronisesTotals()
    {
        var save = Load();
        var editor = new ViceCityProgressionEditor(save);

        editor.SetAll(ViceCityCatalogue.UniqueJumps.Take(6), false);

        Assert.Equal(30, save.Globals.GetInt("TOTAL_COMPLETED_USJ"));
        Assert.Equal(30, save.Stats.GetInt32(ViceCityStatsBlock.UniqueJumpsFoundOffset));
        Assert.Equal(3100, save.Globals.GetInt("CASH_REWARD_USJ"));
    }

    [Fact]
    public void EditsSurviveSaveAndReload()
    {
        var save = Load();
        var editor = new ViceCityProgressionEditor(save);

        save.PlayerInfo.SetMoney(4_242_424);
        save.PlayerPed.Health = 150;
        save.PlayerPed.Weapons[6].Set(ViceCityWeapon.Ruger, 500);
        save.SimpleVars.Hour = 21;
        save.SimpleVars.SetWeather(ViceCityWeather.Rainy);
        editor.SetPackages(42);

        var reloaded = ViceCitySaveFile.Parse(save.ToBytes());

        Assert.True(reloaded.SealWasValid);
        Assert.Equal(4_242_424, reloaded.PlayerInfo.Money);
        Assert.Equal(150f, reloaded.PlayerPed.Health);
        Assert.Equal(ViceCityWeapon.Ruger, reloaded.PlayerPed.Weapons[6].Type);
        Assert.Equal(500, reloaded.PlayerPed.Weapons[6].AmmoTotal);
        Assert.Equal(21, reloaded.SimpleVars.Hour);
        Assert.Equal(ViceCityWeather.Rainy, reloaded.SimpleVars.CurrentWeather);
        Assert.Equal(42, reloaded.PlayerInfo.HiddenPackagesCollected);
    }
}
