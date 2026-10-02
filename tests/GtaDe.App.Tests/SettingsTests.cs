using GtaDe.App.Services;
using Xunit;

namespace GtaDe.App.Tests;

public class SettingsTests
{
    [Theory]
    [InlineData("""{ "LastFiles": null, "RecentFiles": null }""")]
    [InlineData("""{ "SelectedGame": 99, "Theme": 42 }""")]
    [InlineData("""{ "LastFiles": { "Gta3": "" }, "RecentFiles": [ null, "" ] }""")]
    [InlineData("null")]
    public void ValidButNonsensicalJsonFallsBackToUsableSettings(string json)
    {
        var settings = SettingsStore.Parse(json);

        Assert.NotNull(settings.LastFiles);
        Assert.Empty(settings.LastFiles);
        Assert.NotNull(settings.RecentFiles);
        Assert.Empty(settings.RecentFiles);
        Assert.True(Enum.IsDefined(settings.SelectedGame));
        Assert.True(Enum.IsDefined(settings.Theme));
        _ = GameProfile.Get(settings.SelectedGame);
    }
}
