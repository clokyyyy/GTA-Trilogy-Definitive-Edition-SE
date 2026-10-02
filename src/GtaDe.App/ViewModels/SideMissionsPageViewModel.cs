using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GtaDe.Definitions;

namespace GtaDe.App.ViewModels;

/// <summary>One activity's subpage: its toggles plus whatever counters the game keeps for it.</summary>
public sealed partial class ActivityTab : ObservableObject
{
    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private double _progress;

    public ActivityTab(string name, string description, IEnumerable<ToggleItem> items)
    {
        Name = name;
        Description = description;
        Items = new ObservableCollection<ToggleItem>(items);
        Update();
    }

    public string Name { get; }

    public string Description { get; }

    public ObservableCollection<ToggleItem> Items { get; }

    public bool HasItems => Items.Count > 0;

    /// <summary>Read-only figures the game tracks for this activity, shown next to the toggles.</summary>
    public ObservableCollection<StatRow> Counters { get; } = [];

    public bool HasCounters => Counters.Count > 0;

    /// <summary>Called after the counter rows are rebuilt so the layout can show or hide the panel.</summary>
    public void NotifyCountersChanged() => OnPropertyChanged(nameof(HasCounters));

    public void Update()
    {
        var done = Items.Count(i => i.IsComplete);
        Summary = Items.Count == 0 ? string.Empty : $"{done} / {Items.Count}";
        Progress = Items.Count == 0 ? 0 : done * 100.0 / Items.Count;
    }
}

public sealed partial class SideMissionsPageViewModel(MainWindowViewModel shell) : ActivityTabsPageViewModel(shell)
{
    public override string Title => "Side missions";

    public override string Description =>
        "Rampages, stunt jumps, the vehicle missions and the RC and off-road challenges, each on its own tab.";

    public override string Icon => "M12 2 2 7l10 5 10-5-10-5Zm0 9L2 16l10 5 10-5-10-5Z";

    public override void Refresh()
    {
        if (Shell.Save is null || Shell.Progression is not { } editor)
        {
            Tabs.Clear();
            SelectedTab = null;
            return;
        }

        if (Tabs.Count == 0)
        {
            Build(editor);
            SelectedTab = Tabs.FirstOrDefault();
        }
        else
        {
            RefreshToggles(editor);
        }

        RefreshCounters();
    }

    private void Build(Editing.ProgressionEditor editor)
    {
        Tabs.Add(new ActivityTab(
            "Rampages",
            "Twenty timed killing sprees. Completing one updates the running total and the reward the script pays out next.",
            Gta3Catalogue.Rampages.Select(r => new ToggleItem(
                $"Rampage {r.Number}",
                $"{r.Location} — {r.Objective}",
                r.Flag,
                editor.IsRampageComplete(r),
                value => Edit(() => Shell.Progression!.SetRampageComplete(r, value))))));

        Tabs.Add(new ActivityTab(
            "Unique stunt jumps",
            "Twenty jumps. The cash reward the script holds is the value of the next payout, so it moves one step ahead of the count.",
            Gta3Catalogue.UniqueJumps.Select(j => new ToggleItem(
                $"Jump {j.Number}",
                j.Location,
                j.Flag,
                editor.IsUniqueJumpComplete(j),
                value => Edit(() => Shell.Progression!.SetUniqueJumpComplete(j, value))))));

        Tabs.Add(new ActivityTab(
            "Off-road challenges",
            "The four timed driving challenges.",
            Gta3Catalogue.OffRoadChallenges.Select(m => new ToggleItem(
                m.Name,
                m.Note,
                m.Flag,
                Shell.Save!.Globals.GetFlag(m.Flag),
                value => Edit(() => Shell.Save!.Globals.SetFlag(m.Flag, value))))));

        Tabs.Add(new ActivityTab(
            "RC Toyz",
            "The four remote-control car challenges.",
            Gta3Catalogue.RemoteControlChallenges.Select(m => new ToggleItem(
                m.Name,
                m.Note,
                m.Flag,
                Shell.Save!.Globals.GetFlag(m.Flag),
                value => Edit(() => Shell.Save!.Globals.SetFlag(m.Flag, value))))));

        Tabs.Add(new ActivityTab(
            "Vehicle missions",
            "Paramedic, Vigilante, Firefighter and Taxi. The game records these as levels and totals rather than completion flags, so they are shown as figures you can edit on the Stats page.",
            []));
    }

    private void RefreshToggles(Editing.ProgressionEditor editor)
    {
        foreach (var (item, rampage) in Tabs[0].Items.Zip(Gta3Catalogue.Rampages))
        {
            item.SetWithoutApplying(editor.IsRampageComplete(rampage));
        }

        foreach (var (item, jump) in Tabs[1].Items.Zip(Gta3Catalogue.UniqueJumps))
        {
            item.SetWithoutApplying(editor.IsUniqueJumpComplete(jump));
        }

        foreach (var (item, mission) in Tabs[2].Items.Zip(Gta3Catalogue.OffRoadChallenges))
        {
            item.SetWithoutApplying(Shell.Save!.Globals.GetFlag(mission.Flag));
        }

        foreach (var (item, mission) in Tabs[3].Items.Zip(Gta3Catalogue.RemoteControlChallenges))
        {
            item.SetWithoutApplying(Shell.Save!.Globals.GetFlag(mission.Flag));
        }

        foreach (var tab in Tabs)
        {
            tab.Update();
        }
    }

    private void RefreshCounters()
    {
        if (Shell.Save is not { } save)
        {
            return;
        }

        var globals = save.Globals;
        var stats = save.Stats;

        SetCounters(Tabs[0], [
            new StatRow("Total completed", globals.GetInt("TOTAL_RAMPAGES_PASSED").ToString()),
            new StatRow("Next reward", globals.GetInt("RAMPAGE_REWARD").ToString("N0")),
            new StatRow("Stats menu figure", stats.KillFrenziesPassed.ToString()),
        ]);

        SetCounters(Tabs[1], [
            new StatRow("Total completed", globals.GetInt("TOTAL_COMPLETED_USJ").ToString()),
            new StatRow("Next reward", globals.GetInt("CASH_REWARD_USJ").ToString("N0")),
            new StatRow("Best stunt jump", stats.BestStuntJump.ToString()),
        ]);

        SetCounters(Tabs[4], [
            new StatRow("Paramedic level reached", stats.HighestLevelAmbulanceMission.ToString()),
            new StatRow("Lives saved", stats.LivesSavedWithAmbulance.ToString()),
            new StatRow("Criminals caught", stats.CriminalsCaught.ToString()),
            new StatRow("Fires extinguished", stats.FiresExtinguished.ToString()),
            new StatRow("Taxi fares", stats.TaxiPassengersDroppedOff.ToString()),
            new StatRow("Taxi earnings", stats.MoneyMadeWithTaxi.ToString("N0")),
        ]);
    }

    protected override void OnSetSelected(bool value)
    {
        if (SelectedTab is null)
        {
            return;
        }

        switch (Tabs.IndexOf(SelectedTab))
        {
            case 0:
                Edit(() => Shell.Progression!.SetAllRampages(value));
                break;
            case 1:
                Edit(() => Shell.Progression!.SetAllUniqueJumps(value));
                break;
            default:
                base.OnSetSelected(value);
                break;
        }
    }
}