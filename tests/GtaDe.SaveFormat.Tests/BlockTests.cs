namespace GtaDe.SaveFormat.Tests;

public class PlayerInfoTests
{
    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void TheFieldMapIsSelfConsistent(string name)
    {
        var save = SampleSaves.Load(name);
        var player = save.PlayerInfo;

        // The HUD readout tracks the wallet, so a save written by the game always has them equal.
        Assert.Equal(player.Money, player.VisibleMoney);
        Assert.Equal(PlayerInfoBlock.HiddenPackageTotal, player.TotalHiddenPackages);
        Assert.InRange(player.HiddenPackagesCollected, 0, PlayerInfoBlock.HiddenPackageTotal);
        Assert.InRange(player.RoadDensity, 0f, 10f);
        Assert.InRange(player.TrafficMultiplier, 0, 100);
    }

    [Fact]
    public void SettingMoneyUpdatesBothFields()
    {
        var save = SampleSaves.Load("GTA3sf1.sav");

        save.PlayerInfo.SetMoney(1337);

        Assert.Equal(1337, save.PlayerInfo.Money);
        Assert.Equal(1337, save.PlayerInfo.VisibleMoney);
    }

    [Fact]
    public void EditsStayInsideTheirOwnFields()
    {
        var save = SampleSaves.Load("GTA3sf1.sav");
        var before = (byte[])save.Data.Clone();
        var block = save.Layout[SaveBlockKind.PlayerInfo];

        save.PlayerInfo.HiddenPackagesCollected = 42;

        for (var i = 0; i < save.Data.Length; i++)
        {
            var isTargetField = i >= block.DataOffset + 0x13 && i < block.DataOffset + 0x17;
            if (!isTargetField)
            {
                Assert.Equal(before[i], save.Data[i]);
            }
        }
    }
}

public class StatsTests
{
    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void ThePedKillBreakdownSumsToTheTotal(string name)
    {
        var save = SampleSaves.Load(name);

        Assert.Equal(save.Stats.PeopleWastedByPlayer, save.Stats.SumPedTypeKills());
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void TheLastMissionMatchesTheFileHeader(string name)
    {
        var save = SampleSaves.Load(name);

        Assert.Equal(save.LastMissionKey, save.Stats.LastMissionPassed);
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void TheTotalsMatchGtaThree(string name)
    {
        var save = SampleSaves.Load(name);

        Assert.Equal(20, save.Stats.UniqueJumpsTotal);
        Assert.Equal(20, save.Stats.KillFrenziesTotal);
        Assert.Equal(154, save.Stats.TotalProgressInGame);
        Assert.InRange(save.Stats.ProgressMade, 0, save.Stats.TotalProgressInGame);
        Assert.InRange(save.Stats.UniqueJumpsFound, 0, save.Stats.UniqueJumpsTotal);
        Assert.InRange(save.Stats.KillFrenziesPassed, 0, save.Stats.KillFrenziesTotal);
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void ParamedicLivesSavedMatchItsMissionLevel(string name)
    {
        var save = SampleSaves.Load(name);
        var level = save.Stats.HighestLevelAmbulanceMission;

        // Each Paramedic level n rescues n patients, so completing level L saves L*(L+1)/2 lives.
        Assert.Equal(level * (level + 1) / 2, save.Stats.LivesSavedWithAmbulance);
    }

    [Fact]
    public void TheLastMissionKeyRoundTrips()
    {
        var save = SampleSaves.Load("GTA3sf1.sav");

        save.Stats.LastMissionPassed = "LM3";

        Assert.Equal("LM3", save.Stats.LastMissionPassed);
    }
}

public class SimpleVarsTests
{
    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void TheFieldMapIsSelfConsistent(string name)
    {
        var save = SampleSaves.Load(name);
        var vars = save.SimpleVars;

        Assert.Equal(SimpleVars.Signature, vars.SignatureValue);
        Assert.InRange(vars.Hour, 0, 23);
        Assert.InRange(vars.Minute, 0, 59);
        Assert.Equal(1000u, vars.MillisecondsPerGameMinute);
        Assert.InRange((int)vars.Island, 1, 3);
        Assert.InRange(vars.WeatherInterpolation, 0f, 1f);
        Assert.InRange(vars.CameraZ, -200f, 2000f);
    }

    [Fact]
    public void ClockAndWeatherRoundTrip()
    {
        var save = SampleSaves.Load("GTA3sf1.sav");

        save.SimpleVars.Hour = 7;
        save.SimpleVars.Minute = 45;
        save.SimpleVars.SetWeather(WeatherType.Rainy);

        Assert.Equal(7, save.SimpleVars.Hour);
        Assert.Equal(45, save.SimpleVars.Minute);
        Assert.Equal(WeatherType.Rainy, save.SimpleVars.CurrentWeather);
        Assert.Equal(WeatherType.Rainy, save.SimpleVars.PreviousWeather);
        Assert.Equal(0f, save.SimpleVars.WeatherInterpolation);
    }
}
