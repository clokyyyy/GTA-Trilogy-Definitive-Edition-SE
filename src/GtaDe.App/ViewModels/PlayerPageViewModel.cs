using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GtaDe.App.ViewModels;

public sealed partial class PlayerPageViewModel(MainWindowViewModel shell) : PageViewModel(shell)
{
    public override string Title => "Player";

    public override string Description =>
        "Money, the pickup bonuses and the collectibles the player carries between missions.";

    public override string Icon => "M12 12a5 5 0 1 0 0-10 5 5 0 0 0 0 10Zm0 2c-5 0-9 2.5-9 5.5V22h18v-2.5c0-3-4-5.5-9-5.5Z";

    public override string Group => "Save";

    [ObservableProperty]
    private int _money;

    [ObservableProperty]
    private int _hiddenPackages;

    [ObservableProperty]
    private int _totalHiddenPackages = 100;

    [ObservableProperty]
    private bool _infiniteSprint;

    [ObservableProperty]
    private bool _fastReload;

    [ObservableProperty]
    private bool _getOutOfJailFree;

    [ObservableProperty]
    private bool _freeHealthCare;

    [ObservableProperty]
    private double _roadDensity = 1;

    [ObservableProperty]
    private int _trafficMultiplier;

    public override void Refresh()
    {
        if (Shell.Save is not { } save)
        {
            return;
        }

        var player = save.PlayerInfo;

        Money = player.Money;
        HiddenPackages = player.HiddenPackagesCollected;
        TotalHiddenPackages = player.TotalHiddenPackages;
        InfiniteSprint = player.InfiniteSprint;
        FastReload = player.FastReload;
        GetOutOfJailFree = player.GetOutOfJailFree;
        FreeHealthCare = player.FreeHealthCare;
        RoadDensity = player.RoadDensity;
        TrafficMultiplier = player.TrafficMultiplier;
    }

    partial void OnMoneyChanged(int value) => Edit(() => Shell.Save!.PlayerInfo.SetMoney(value));

    partial void OnHiddenPackagesChanged(int value) => Edit(() =>
    {
        var clamped = Math.Clamp(value, 0, Shell.Save!.PlayerInfo.TotalHiddenPackages);
        Shell.Save.PlayerInfo.HiddenPackagesCollected = clamped;

        // The game grants the four pickup bonuses as the package count passes each threshold, so
        // the editor applies the same rule rather than leaving a save the game would disagree with.
        var player = Shell.Save.PlayerInfo;
        player.InfiniteSprint = clamped >= 10;
        player.FastReload = clamped >= 30;
        player.GetOutOfJailFree = clamped >= 50;
        player.FreeHealthCare = clamped >= 70;
    });

    partial void OnInfiniteSprintChanged(bool value) => Edit(() => Shell.Save!.PlayerInfo.InfiniteSprint = value);

    partial void OnFastReloadChanged(bool value) => Edit(() => Shell.Save!.PlayerInfo.FastReload = value);

    partial void OnGetOutOfJailFreeChanged(bool value) => Edit(() => Shell.Save!.PlayerInfo.GetOutOfJailFree = value);

    partial void OnFreeHealthCareChanged(bool value) => Edit(() => Shell.Save!.PlayerInfo.FreeHealthCare = value);

    partial void OnRoadDensityChanged(double value) => Edit(() => Shell.Save!.PlayerInfo.RoadDensity = (float)value);

    partial void OnTrafficMultiplierChanged(int value) => Edit(() => Shell.Save!.PlayerInfo.TrafficMultiplier = (ushort)Math.Clamp(value, 0, ushort.MaxValue));

    [RelayCommand]
    private void CollectAllPackages() => HiddenPackages = TotalHiddenPackages;

    [RelayCommand]
    private void GiveMillion() => Money = 1_000_000;
}
