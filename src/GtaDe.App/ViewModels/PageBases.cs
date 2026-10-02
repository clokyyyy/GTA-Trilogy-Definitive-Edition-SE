using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GtaDe.App.ViewModels;

/// <summary>
/// A page drawn as a list of toggle cards (one per mission strand). Every game's missions page
/// derives from this so they can share one view.
/// </summary>
public abstract partial class ToggleGroupsPageViewModel(MainWindowViewModel shell) : PageViewModel(shell)
{
    public ObservableCollection<ToggleGroup> Groups { get; } = [];

    [ObservableProperty]
    private string _overallSummary = "—";

    public virtual string CompleteEverythingLabel => "Complete every mission";

    [RelayCommand]
    private void CompleteEverything() => OnCompleteEverything();

    protected abstract void OnCompleteEverything();
}

/// <summary>
/// A page split into activity subpages, each a list of toggles with optional counters. Shared by
/// every game's side-activity pages.
/// </summary>
public abstract partial class ActivityTabsPageViewModel(MainWindowViewModel shell) : PageViewModel(shell)
{
    public ObservableCollection<ActivityTab> Tabs { get; } = [];

    [ObservableProperty]
    private ActivityTab? _selectedTab;

    [RelayCommand]
    private void CompleteSelected() => OnSetSelected(true);

    [RelayCommand]
    private void ClearSelected() => OnSetSelected(false);

    /// <summary>Marks every toggle on the selected tab; override when totals must be resynchronised.</summary>
    protected virtual void OnSetSelected(bool value) => Edit(() =>
    {
        foreach (var item in SelectedTab?.Items ?? [])
        {
            item.IsComplete = value;
        }
    });

    protected static void SetCounters(ActivityTab tab, IEnumerable<StatRow> rows)
    {
        tab.Counters.Clear();
        foreach (var row in rows)
        {
            tab.Counters.Add(row);
        }

        tab.NotifyCountersChanged();
    }
}
