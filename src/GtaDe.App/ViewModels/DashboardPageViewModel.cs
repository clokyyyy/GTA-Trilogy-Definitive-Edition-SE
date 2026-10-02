using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GtaDe.Definitions;

namespace GtaDe.App.ViewModels;

/// <summary>A single headline figure on the dashboard.</summary>
public sealed partial class SummaryTile : ObservableObject
{
    [ObservableProperty]
    private string _value = "—";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _showProgress;

    public SummaryTile(string label, string caption)
    {
        Label = label;
        Caption = caption;
    }

    public string Label { get; }

    public string Caption { get; }
}

public sealed partial class DashboardPageViewModel : PageViewModel
{
    public DashboardPageViewModel(MainWindowViewModel shell) : base(shell)
    {
        Tiles =
        [
            Completion = new SummaryTile("Game completed", "Of 100%") { ShowProgress = true },
            Money = new SummaryTile("Money", "In hand"),
            Missions = new SummaryTile("Story missions", "Passed") { ShowProgress = true },
            Packages = new SummaryTile("Hidden packages", "Collected") { ShowProgress = true },
            Rampages = new SummaryTile("Rampages", "Completed") { ShowProgress = true },
            Jumps = new SummaryTile("Unique jumps", "Landed") { ShowProgress = true },
        ];
    }

    public override string Title => "Dashboard";

    public override string Description => "An overview of this save and the fastest way to spot what is left to do.";

    public override string Icon => "M4 13h6V4H4v9Zm0 7h6v-5H4v5Zm8 0h6V11h-6v9Zm0-16v5h6V4h-6Z";

    public override string Group => "Overview";

    public ObservableCollection<SummaryTile> Tiles { get; }

    /// <summary>The game whose artwork heads the page.</summary>
    public Services.GameProfile Game => Shell.SelectedGame;

    /// <summary>A one-line headline for the banner, e.g. "62.5% complete · $1,204,880".</summary>
    [ObservableProperty]
    private string _headline = string.Empty;

    public SummaryTile Completion { get; }

    public SummaryTile Money { get; }

    public SummaryTile Missions { get; }

    public SummaryTile Packages { get; }

    public SummaryTile Rampages { get; }

    public SummaryTile Jumps { get; }

    [ObservableProperty]
    private string _lastMission = "—";

    [ObservableProperty]
    private string _savedAt = "—";

    [ObservableProperty]
    private string _island = "—";

    [ObservableProperty]
    private string _gameClock = "—";

    [ObservableProperty]
    private string _playTime = "—";

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private bool _sealWasValid = true;

    public override void Refresh()
    {
        if (Shell.Save is not { } save)
        {
            foreach (var tile in Tiles)
            {
                tile.Value = "—";
                tile.Progress = 0;
            }

            LastMission = SavedAt = Island = GameClock = PlayTime = "—";
            FilePath = string.Empty;
            Headline = string.Empty;
            SealWasValid = true;
            return;
        }

        var stats = save.Stats;
        var player = save.PlayerInfo;
        var globals = save.Globals;

        SetTile(Completion, $"{Percent(stats.ProgressMade, stats.TotalProgressInGame):0.#}%", stats.ProgressMade, stats.TotalProgressInGame);
        Money.Value = player.Money.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        SetTile(Missions, $"{stats.MissionsPassed} / {Gta3Catalogue.TotalStoryMissions}", stats.MissionsPassed, Gta3Catalogue.TotalStoryMissions);
        SetTile(Packages, $"{player.HiddenPackagesCollected} / {player.TotalHiddenPackages}", player.HiddenPackagesCollected, player.TotalHiddenPackages);

        var rampages = globals.GetInt("TOTAL_RAMPAGES_PASSED");
        SetTile(Rampages, $"{rampages} / {Gta3Catalogue.TotalRampages}", rampages, Gta3Catalogue.TotalRampages);

        var jumps = globals.GetInt("TOTAL_COMPLETED_USJ");
        SetTile(Jumps, $"{jumps} / {Gta3Catalogue.TotalUniqueJumps}", jumps, Gta3Catalogue.TotalUniqueJumps);

        LastMission = string.IsNullOrWhiteSpace(save.LastMissionKey) ? "None yet" : save.LastMissionKey;
        SavedAt = save.SavedAtUtc.ToLocalTime().ToString("dddd d MMMM yyyy, HH:mm");
        Island = save.SimpleVars.Island.ToString();
        GameClock = $"{save.SimpleVars.Hour:00}:{save.SimpleVars.Minute:00}";
        PlayTime = $"{stats.DaysPassed} in-game days";
        FilePath = save.Path ?? string.Empty;
        SealWasValid = save.SealWasValid;
        Headline = $"{Completion.Value} complete  ·  {Money.Value}  ·  {Missions.Value} story missions";
        OnPropertyChanged(nameof(Game));
    }

    private static void SetTile(SummaryTile tile, string value, int current, int total)
    {
        tile.Value = value;
        tile.Progress = Percent(current, total);
    }

    private static double Percent(int current, int total) =>
        total <= 0 ? 0 : Math.Clamp(current * 100.0 / total, 0, 100);
}
