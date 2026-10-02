using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GtaDe.Definitions;

namespace GtaDe.App.ViewModels;

/// <summary>A single completion toggle backed by one script flag.</summary>
public sealed partial class ToggleItem : ObservableObject
{
    private readonly Action<bool> _apply;
    private bool _suppress;

    [ObservableProperty]
    private bool _isComplete;

    public ToggleItem(string name, string? detail, string flag, bool isComplete, Action<bool> apply)
    {
        Name = name;
        Detail = detail;
        Flag = flag;
        _apply = apply;
        _suppress = true;
        IsComplete = isComplete;
        _suppress = false;
    }

    public string Name { get; }

    public string? Detail { get; }

    /// <summary>The script global behind this toggle, shown so the user can verify what it edits.</summary>
    public string Flag { get; }

    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);

    /// <summary>Updates the checkbox without writing to the save, used when reloading from the buffer.</summary>
    public void SetWithoutApplying(bool value)
    {
        _suppress = true;
        IsComplete = value;
        _suppress = false;
    }

    partial void OnIsCompleteChanged(bool value)
    {
        if (!_suppress)
        {
            _apply(value);
        }
    }
}

/// <summary>A named group of toggles, drawn as one card.</summary>
public sealed partial class ToggleGroup : ObservableObject
{
    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private double _progress;

    public ToggleGroup(string name, string? subtitle, IEnumerable<ToggleItem> items)
    {
        Name = name;
        Subtitle = subtitle;
        Items = new ObservableCollection<ToggleItem>(items);
        UpdateSummary();
    }

    public string Name { get; }

    public string? Subtitle { get; }

    public ObservableCollection<ToggleItem> Items { get; }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    public void UpdateSummary()
    {
        var done = Items.Count(i => i.IsComplete);
        Summary = $"{done} / {Items.Count}";
        Progress = Items.Count == 0 ? 0 : done * 100.0 / Items.Count;
    }
}

public sealed partial class MissionsPageViewModel(MainWindowViewModel shell) : ToggleGroupsPageViewModel(shell)
{
    public override string Title => "Story missions";

    public override string Description =>
        "Completion flags for every contact. Toggling a mission also updates the strand's unlock flag and the mission counter the stats menu reads.";

    public override string Icon => "M4 4h16v2H4V4Zm0 5h16v2H4V9Zm0 5h10v2H4v-2Zm0 5h10v2H4v-2Z";

    public override void Refresh()
    {
        if (Shell.Save is null || Shell.Progression is not { } editor)
        {
            Groups.Clear();
            OverallSummary = "—";
            return;
        }

        if (Groups.Count == 0)
        {
            Build(editor);
        }
        else
        {
            foreach (var (group, strand) in Groups.Zip(Gta3Catalogue.Strands))
            {
                foreach (var (item, mission) in group.Items.Zip(strand.Missions))
                {
                    item.SetWithoutApplying(editor.IsMissionComplete(mission));
                }

                group.UpdateSummary();
            }
        }

        var all = Gta3Catalogue.Strands.SelectMany(s => s.Missions).ToList();
        var done = all.Count(m => editor.IsMissionComplete(m));
        OverallSummary = $"{done} of {all.Count} tracked missions complete";
    }

    private void Build(Editing.ProgressionEditor editor)
    {
        foreach (var strand in Gta3Catalogue.Strands)
        {
            var items = strand.Missions.Select(mission => new ToggleItem(
                mission.Name,
                mission.Note,
                mission.Flag,
                editor.IsMissionComplete(mission),
                value => Edit(() => Shell.Progression!.SetMissionComplete(strand, mission, value))));

            Groups.Add(new ToggleGroup(strand.Name, strand.Island.ToDisplayName(), items));
        }
    }

    protected override void OnCompleteEverything() => Edit(() =>
    {
        foreach (var strand in Gta3Catalogue.Strands)
        {
            Shell.Progression!.SetStrandComplete(strand, true);
        }
    });
}

internal static class IslandDisplay
{
    public static string ToDisplayName(this Island island) => island switch
    {
        Island.Portland => "Portland",
        Island.StauntonIsland => "Staunton Island",
        Island.ShoresideVale => "Shoreside Vale",
        _ => island.ToString(),
    };
}
