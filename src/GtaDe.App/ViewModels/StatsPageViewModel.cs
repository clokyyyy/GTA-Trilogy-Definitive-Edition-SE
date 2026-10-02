using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GtaDe.App.ViewModels;

/// <summary>A read-only label and value pair.</summary>
public sealed record StatRow(string Label, string Value);

/// <summary>An editable integer stat, written straight back to the Stats block.</summary>
public sealed partial class EditableStat : ObservableObject
{
    private readonly Action<int> _apply;
    private bool _suppress;

    [ObservableProperty]
    private int _value;

    public EditableStat(string label, string? hint, int value, Action<int> apply)
    {
        Label = label;
        Hint = hint;
        _apply = apply;
        _suppress = true;
        Value = value;
        _suppress = false;
    }

    public string Label { get; }

    public string? Hint { get; }

    public bool HasHint => !string.IsNullOrWhiteSpace(Hint);

    public void SetWithoutApplying(int value)
    {
        _suppress = true;
        Value = value;
        _suppress = false;
    }

    partial void OnValueChanged(int value)
    {
        if (!_suppress)
        {
            _apply(value);
        }
    }
}

public sealed partial class StatsPageViewModel(MainWindowViewModel shell) : PageViewModel(shell)
{
    public override string Title => "Stats";

    public override string Description =>
        "Everything the in-game stats menu shows. The vehicle side missions live here because the game stores them as levels and totals, not as completion flags.";

    public override string Icon => "M4 20h3v-8H4v8Zm6.5 0h3V4h-3v16Zm6.5 0h3v-12h-3v12Z";

    public ObservableCollection<EditableStat> VehicleMissions { get; } = [];

    public ObservableCollection<EditableStat> Criminal { get; } = [];

    public ObservableCollection<EditableStat> Driving { get; } = [];

    public ObservableCollection<StatRow> ReadOnly { get; } = [];

    [ObservableProperty]
    private string _progressSummary = "—";

    public override void Refresh()
    {
        if (Shell.Save is not { } save)
        {
            VehicleMissions.Clear();
            Criminal.Clear();
            Driving.Clear();
            ReadOnly.Clear();
            ProgressSummary = "—";
            return;
        }

        var stats = save.Stats;

        if (VehicleMissions.Count == 0)
        {
            Build();
        }
        else
        {
            RefreshValues();
        }

        ReadOnly.Clear();
        ReadOnly.Add(new StatRow("People wasted by the player", stats.PeopleWastedByPlayer.ToString("N0")));
        ReadOnly.Add(new StatRow("People wasted by others", stats.PeopleWastedByOthers.ToString("N0")));
        ReadOnly.Add(new StatRow("Rounds fired", stats.RoundsFiredByPlayer.ToString("N0")));
        ReadOnly.Add(new StatRow("Accuracy", Accuracy(stats.InstantHitsFiredByPlayer, stats.InstantHitsHitByPlayer)));
        ReadOnly.Add(new StatRow("Cars exploded", stats.CarsExploded.ToString("N0")));
        ReadOnly.Add(new StatRow("Helicopters destroyed", stats.HelicoptersDestroyed.ToString("N0")));
        ReadOnly.Add(new StatRow("Distance on foot", $"{stats.DistanceTravelledOnFoot:N0} m"));
        ReadOnly.Add(new StatRow("Distance by car", $"{stats.DistanceTravelledByCar:N0} m"));
        ReadOnly.Add(new StatRow("Longest Dodo flight", stats.LongestFlightInDodo.ToString("N0")));
        ReadOnly.Add(new StatRow("Largest jump distance", $"{stats.MaximumJumpDistance:N1}"));
        ReadOnly.Add(new StatRow("Largest jump height", $"{stats.MaximumJumpHeight:N1}"));
        ReadOnly.Add(new StatRow("In-game days passed", stats.DaysPassed.ToString("N0")));
        ReadOnly.Add(new StatRow("Last mission passed", stats.LastMissionPassed));

        ProgressSummary = $"{stats.ProgressMade} of {stats.TotalProgressInGame} progress points";
    }

    private static string Accuracy(int fired, int hit) =>
        fired <= 0 ? "—" : $"{hit * 100.0 / fired:N1}%";

    private void Build()
    {
        var stats = Shell.Save!.Stats;

        VehicleMissions.Add(new EditableStat("Paramedic level reached", "Lives saved is kept in step: level L means L x (L+1) / 2 patients.", stats.HighestLevelAmbulanceMission, v => Edit(() =>
        {
            var level = Math.Max(0, v);
            Shell.Save!.Stats.HighestLevelAmbulanceMission = level;
            Shell.Save.Stats.LivesSavedWithAmbulance = level * (level + 1) / 2;
        })));

        VehicleMissions.Add(new EditableStat("Lives saved", null, stats.LivesSavedWithAmbulance, v => Edit(() => Shell.Save!.Stats.LivesSavedWithAmbulance = v)));
        VehicleMissions.Add(new EditableStat("Criminals caught (Vigilante)", null, stats.CriminalsCaught, v => Edit(() => Shell.Save!.Stats.CriminalsCaught = v)));
        VehicleMissions.Add(new EditableStat("Fires extinguished", null, stats.FiresExtinguished, v => Edit(() => Shell.Save!.Stats.FiresExtinguished = v)));
        VehicleMissions.Add(new EditableStat("Taxi fares dropped off", null, stats.TaxiPassengersDroppedOff, v => Edit(() => Shell.Save!.Stats.TaxiPassengersDroppedOff = v)));
        VehicleMissions.Add(new EditableStat("Taxi earnings", null, stats.MoneyMadeWithTaxi, v => Edit(() => Shell.Save!.Stats.MoneyMadeWithTaxi = v)));

        Criminal.Add(new EditableStat("Times arrested", null, stats.TimesArrested, v => Edit(() => Shell.Save!.Stats.TimesArrested = v)));
        Criminal.Add(new EditableStat("Times died", null, stats.TimesDied, v => Edit(() => Shell.Save!.Stats.TimesDied = v)));
        Criminal.Add(new EditableStat("Kills since last checkpoint", null, stats.KillsSinceLastCheckpoint, v => Edit(() => Shell.Save!.Stats.KillsSinceLastCheckpoint = v)));
        Criminal.Add(new EditableStat("Cars crushed", null, stats.CarsCrushed, v => Edit(() => Shell.Save!.Stats.CarsCrushed = v)));
        Criminal.Add(new EditableStat("Explosives used (kg)", null, stats.KgsOfExplosivesUsed, v => Edit(() => Shell.Save!.Stats.KgsOfExplosivesUsed = v)));

        Driving.Add(new EditableStat("Flips", null, stats.MaximumJumpFlips, v => Edit(() => Shell.Save!.Stats.MaximumJumpFlips = v)));
        Driving.Add(new EditableStat("360 spins", null, stats.MaximumJumpSpins, v => Edit(() => Shell.Save!.Stats.MaximumJumpSpins = v)));
        Driving.Add(new EditableStat("Best stunt jump", null, stats.BestStuntJump, v => Edit(() => Shell.Save!.Stats.BestStuntJump = v)));
        Driving.Add(new EditableStat("Bomb defuse record", null, stats.TimeTakenDefuseMission, v => Edit(() => Shell.Save!.Stats.TimeTakenDefuseMission = v)));
    }

    private void RefreshValues()
    {
        var stats = Shell.Save!.Stats;

        VehicleMissions[0].SetWithoutApplying(stats.HighestLevelAmbulanceMission);
        VehicleMissions[1].SetWithoutApplying(stats.LivesSavedWithAmbulance);
        VehicleMissions[2].SetWithoutApplying(stats.CriminalsCaught);
        VehicleMissions[3].SetWithoutApplying(stats.FiresExtinguished);
        VehicleMissions[4].SetWithoutApplying(stats.TaxiPassengersDroppedOff);
        VehicleMissions[5].SetWithoutApplying(stats.MoneyMadeWithTaxi);

        Criminal[0].SetWithoutApplying(stats.TimesArrested);
        Criminal[1].SetWithoutApplying(stats.TimesDied);
        Criminal[2].SetWithoutApplying(stats.KillsSinceLastCheckpoint);
        Criminal[3].SetWithoutApplying(stats.CarsCrushed);
        Criminal[4].SetWithoutApplying(stats.KgsOfExplosivesUsed);

        Driving[0].SetWithoutApplying(stats.MaximumJumpFlips);
        Driving[1].SetWithoutApplying(stats.MaximumJumpSpins);
        Driving[2].SetWithoutApplying(stats.BestStuntJump);
        Driving[3].SetWithoutApplying(stats.TimeTakenDefuseMission);
    }
}
