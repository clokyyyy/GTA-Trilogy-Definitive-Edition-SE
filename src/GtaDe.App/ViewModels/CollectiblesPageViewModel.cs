using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GtaDe.Definitions;

namespace GtaDe.App.ViewModels;

public sealed partial class CollectiblesPageViewModel(MainWindowViewModel shell) : PageViewModel(shell)
{
    public override string Title => "Collectibles";

    public override string Description =>
        "Hidden packages and the island completion flags the game uses to gate the story.";

    public override string Icon => "M12 2 3 7v10l9 5 9-5V7l-9-5Zm0 2.3 6.5 3.6L12 11.5 5.5 7.9 12 4.3Z";

    public ObservableCollection<ToggleItem> IslandFlags { get; } = [];

    [ObservableProperty]
    private int _packages;

    [ObservableProperty]
    private int _packageTotal = 100;

    [ObservableProperty]
    private double _packageProgress;

    [ObservableProperty]
    private string _bonusSummary = string.Empty;

    public override void Refresh()
    {
        if (Shell.Save is not { } save)
        {
            IslandFlags.Clear();
            return;
        }

        var player = save.PlayerInfo;
        Packages = player.HiddenPackagesCollected;
        PackageTotal = player.TotalHiddenPackages;
        PackageProgress = PackageTotal == 0 ? 0 : Packages * 100.0 / PackageTotal;

        var earned = new List<string>();
        if (player.InfiniteSprint)
        {
            earned.Add("infinite sprint");
        }

        if (player.FastReload)
        {
            earned.Add("fast reload");
        }

        if (player.GetOutOfJailFree)
        {
            earned.Add("get out of jail free");
        }

        if (player.FreeHealthCare)
        {
            earned.Add("free health care");
        }

        BonusSummary = earned.Count == 0
            ? "No package bonuses are active."
            : "Active bonuses: " + string.Join(", ", earned) + ".";

        if (IslandFlags.Count == 0)
        {
            foreach (var (name, flag) in Gta3Catalogue.IslandCompletionFlags)
            {
                IslandFlags.Add(new ToggleItem(
                    name,
                    "Set by the script once that island's story is finished.",
                    flag,
                    save.Globals.GetFlag(flag),
                    value => Edit(() => Shell.Save!.Globals.SetFlag(flag, value))));
            }
        }
        else
        {
            foreach (var (item, entry) in IslandFlags.Zip(Gta3Catalogue.IslandCompletionFlags))
            {
                item.SetWithoutApplying(save.Globals.GetFlag(entry.Flag));
            }
        }
    }

    partial void OnPackagesChanged(int value) => Edit(() =>
    {
        var player = Shell.Save!.PlayerInfo;
        var clamped = Math.Clamp(value, 0, player.TotalHiddenPackages);

        player.HiddenPackagesCollected = clamped;
        player.InfiniteSprint = clamped >= 10;
        player.FastReload = clamped >= 30;
        player.GetOutOfJailFree = clamped >= 50;
        player.FreeHealthCare = clamped >= 70;
    });

    [RelayCommand]
    private void CollectAll() => Packages = PackageTotal;

    [RelayCommand]
    private void CollectNone() => Packages = 0;
}
