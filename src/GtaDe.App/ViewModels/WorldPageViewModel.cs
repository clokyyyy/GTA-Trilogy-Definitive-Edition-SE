using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GtaDe.SaveFormat;

namespace GtaDe.App.ViewModels;

public sealed partial class WorldPageViewModel(MainWindowViewModel shell) : PageViewModel(shell)
{
    public override string Title => "World";

    public override string Description =>
        "The world clock, the weather and which island the save is set on.";

    public override string Icon => "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Zm1 5h-2v6l5 3 1-1.6-4-2.4V7Z";

    public ObservableCollection<WeatherType> WeatherOptions { get; } =
        [WeatherType.Sunny, WeatherType.Cloudy, WeatherType.Rainy, WeatherType.Foggy];

    public ObservableCollection<IslandLevel> IslandOptions { get; } =
        [IslandLevel.Portland, IslandLevel.StauntonIsland, IslandLevel.ShoresideVale];

    [ObservableProperty]
    private int _hour;

    [ObservableProperty]
    private int _minute;

    [ObservableProperty]
    private WeatherType _weather;

    [ObservableProperty]
    private IslandLevel _island;

    [ObservableProperty]
    private bool _forceWeather;

    [ObservableProperty]
    private string _cameraPosition = "—";

    public override void Refresh()
    {
        if (Shell.Save is not { } save)
        {
            return;
        }

        var vars = save.SimpleVars;
        Hour = vars.Hour;
        Minute = vars.Minute;
        Weather = vars.CurrentWeather;
        Island = vars.Island;
        ForceWeather = vars.ForcedWeather != SimpleVars.WeatherNotForced;
        CameraPosition = $"X {vars.CameraX:N1}, Y {vars.CameraY:N1}, Z {vars.CameraZ:N1}";
    }

    partial void OnHourChanged(int value) => Edit(() => Shell.Save!.SimpleVars.Hour = value);

    partial void OnMinuteChanged(int value) => Edit(() => Shell.Save!.SimpleVars.Minute = value);

    partial void OnIslandChanged(IslandLevel value) => Edit(() => Shell.Save!.SimpleVars.Island = value);

    partial void OnWeatherChanged(WeatherType value) => Edit(() =>
    {
        Shell.Save!.SimpleVars.SetWeather(value);

        if (ForceWeather)
        {
            Shell.Save.SimpleVars.ForcedWeather = (ushort)value;
        }
    });

    partial void OnForceWeatherChanged(bool value) => Edit(() =>
        Shell.Save!.SimpleVars.ForcedWeather = value
            ? (ushort)Weather
            : SimpleVars.WeatherNotForced);
}
