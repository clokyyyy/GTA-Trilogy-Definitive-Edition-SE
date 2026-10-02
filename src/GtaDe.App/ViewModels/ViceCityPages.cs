using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GtaDe.Definitions;
using GtaDe.Editing;
using GtaDe.SaveFormat;

namespace GtaDe.App.ViewModels;

/// <summary>An editable number that may be stored as an int or a float.</summary>
public sealed partial class EditableNumber : ObservableObject
{
    private readonly Action<decimal> _apply;
    private bool _suppress;

    [ObservableProperty]
    private decimal _value;

    public EditableNumber(string label, string? hint, bool isFloat, decimal value, Action<decimal> apply)
    {
        Label = label;
        Hint = hint;
        IsFloat = isFloat;
        _apply = apply;
        SetWithoutApplying(value);
    }

    public string Label { get; }

    public string? Hint { get; }

    public bool HasHint => !string.IsNullOrWhiteSpace(Hint);

    public bool IsFloat { get; }

    public string Format => IsFloat ? "0.##" : "0";

    public decimal Increment => 1;

    public void SetWithoutApplying(decimal value)
    {
        _suppress = true;
        Value = value;
        _suppress = false;
    }

    partial void OnValueChanged(decimal value)
    {
        if (!_suppress)
        {
            _apply(value);
        }
    }
}

/// <summary>A titled list of editable numbers.</summary>
public sealed class NumberGroup(string name, string? note, IEnumerable<EditableNumber> items) : Controls.IFullRowCard
{
    private const int LongListThreshold = 14;

    /// <summary>Long lists span the page width at the bottom, their fields split into columns.</summary>
    public bool IsFullRow => Items.Count > LongListThreshold;

    /// <summary>Most field columns the group's own list may use.</summary>
    public int Columns => IsFullRow ? 3 : 1;

    public string Name { get; } = name;

    public string? Note { get; } = note;

    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    public ObservableCollection<EditableNumber> Items { get; } = new(items);
}

/// <summary>Shared helpers for the Vice City pages.</summary>
public abstract class ViceCityPage(MainWindowViewModel shell) : PageViewModel(shell)
{
    public override bool IsAvailable => Shell.ViceCity is not null;

    protected ViceCitySaveFile? Save => Shell.ViceCity;

    protected ViceCityProgressionEditor? Editor => Shell.VcProgression;

    protected static decimal ToDecimal(double value) =>
        double.IsFinite(value) ? (decimal)Math.Clamp(value, -7.9e27, 7.9e27) : 0m;
}

public sealed partial class VcDashboardPageViewModel : ViceCityPage
{
    public VcDashboardPageViewModel(MainWindowViewModel shell) : base(shell)
    {
        Tiles =
        [
            Completion = new SummaryTile("Game completed", "Of 100%") { ShowProgress = true },
            Money = new SummaryTile("Money", "In hand"),
            Missions = new SummaryTile("Story missions", "Passed") { ShowProgress = true },
            Packages = new SummaryTile("Hidden packages", "Collected") { ShowProgress = true },
            Rampages = new SummaryTile("Rampages", "Completed") { ShowProgress = true },
            Jumps = new SummaryTile("Unique jumps", "Landed") { ShowProgress = true },
            Assets = new SummaryTile("Business assets", "Acquired") { ShowProgress = true },
            Safehouses = new SummaryTile("Safehouses", "Bought") { ShowProgress = true },
        ];
    }

    public override string Title => "Dashboard";

    public override string Description => "An overview of this Vice City save and what is left to do.";

    public override string Icon => "M4 13h6V4H4v9Zm0 7h6v-5H4v5Zm8 0h6V11h-6v9Zm0-16v5h6V4h-6Z";

    public override string Group => "Overview";

    public ObservableCollection<SummaryTile> Tiles { get; }

    public Services.GameProfile Game => Shell.SelectedGame;

    public SummaryTile Completion { get; }

    public SummaryTile Money { get; }

    public SummaryTile Missions { get; }

    public SummaryTile Packages { get; }

    public SummaryTile Rampages { get; }

    public SummaryTile Jumps { get; }

    public SummaryTile Assets { get; }

    public SummaryTile Safehouses { get; }

    [ObservableProperty]
    private string _headline = string.Empty;

    [ObservableProperty]
    private string _lastMission = "—";

    [ObservableProperty]
    private string _savedAt = "—";

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
        if (Save is not { } save || Editor is not { } editor)
        {
            foreach (var tile in Tiles)
            {
                tile.Value = "—";
                tile.Progress = 0;
            }

            LastMission = SavedAt = GameClock = PlayTime = "—";
            FilePath = Headline = string.Empty;
            SealWasValid = true;
            return;
        }

        var stats = save.Stats;
        var total = stats.TotalProgressInGame;
        var percent = total <= 0 ? 0 : Math.Clamp(stats.ProgressMade * 100.0 / total, 0, 100);
        Completion.Value = $"{percent:0.#}%";
        Completion.Progress = percent;

        Money.Value = save.PlayerInfo.Money.ToString("C0", CultureInfo.GetCultureInfo("en-US"));

        var missions = ViceCityCatalogue.Strands.SelectMany(s => s.Entries).Where(editor.Has).ToList();
        Tile(Missions, editor.Count(missions), missions.Count);
        Tile(Packages, save.PlayerInfo.HiddenPackagesCollected, save.PlayerInfo.TotalHiddenPackages);
        Tile(Rampages, editor.Count(ViceCityCatalogue.Rampages), ViceCityCatalogue.RampageCount);
        Tile(Jumps, editor.Count(ViceCityCatalogue.UniqueJumps), ViceCityCatalogue.UniqueJumpCount);
        Tile(Assets, editor.Count(ViceCityCatalogue.Assets), ViceCityCatalogue.Assets.Count);
        Tile(Safehouses, editor.Count(ViceCityCatalogue.Safehouses), ViceCityCatalogue.Safehouses.Count);

        var last = stats.LastMissionPassedName;
        LastMission = string.IsNullOrWhiteSpace(last) ? (string.IsNullOrWhiteSpace(save.LastMissionKey) ? "None yet" : save.LastMissionKey) : last;
        SavedAt = save.SavedAtUtc.ToLocalTime().ToString("dddd d MMMM yyyy, HH:mm");
        GameClock = $"{save.SimpleVars.Hour:00}:{save.SimpleVars.Minute:00}";
        PlayTime = $"{stats.GetInt32(156)} in-game days";
        FilePath = save.Path ?? string.Empty;
        SealWasValid = save.SealWasValid;
        Headline = $"{Completion.Value} complete  ·  {Money.Value}  ·  {Missions.Value} story missions";
        OnPropertyChanged(nameof(Game));
    }

    private static void Tile(SummaryTile tile, int current, int total)
    {
        tile.Value = $"{current} / {total}";
        tile.Progress = total <= 0 ? 0 : Math.Clamp(current * 100.0 / total, 0, 100);
    }
}

/// <summary>One weapon option for a slot's combo box.</summary>
public sealed record WeaponOption(ViceCityWeapon? Weapon, string Name)
{
    public override string ToString() => Name;
}

/// <summary>One of the player's ten weapon slots.</summary>
public sealed partial class WeaponSlotRow : ObservableObject
{
    private readonly Action<WeaponSlotRow> _apply;
    private bool _suppress;

    [ObservableProperty]
    private WeaponOption? _selected;

    [ObservableProperty]
    private int _ammo;

    public WeaponSlotRow(int slot, Action<WeaponSlotRow> apply)
    {
        Slot = slot;
        _apply = apply;
        Options =
        [
            new WeaponOption(null, "Empty"),
            .. ViceCityWeapons.ForSlot(slot).Select(w => new WeaponOption(w.Weapon, w.Name)),
        ];
    }

    public int Slot { get; }

    public string SlotName => ViceCityWeapons.SlotNames[Slot];

    public IReadOnlyList<WeaponOption> Options { get; }

    public bool UsesAmmo => Selected?.Weapon is { } w && !ViceCityWeapons.Get(w).IsMelee;

    public void Load(ViceCityWeaponSlot slot)
    {
        _suppress = true;
        Selected = slot.IsEmpty || slot.Type == ViceCityWeapon.Unarmed
            ? Options[0]
            : Options.FirstOrDefault(o => o.Weapon == slot.Type) ?? Options[0];
        Ammo = slot.AmmoTotal;
        _suppress = false;
        OnPropertyChanged(nameof(UsesAmmo));
    }

    partial void OnSelectedChanged(WeaponOption? value)
    {
        OnPropertyChanged(nameof(UsesAmmo));
        if (_suppress)
        {
            return;
        }

        if (value?.Weapon is { } weapon && Ammo <= 0)
        {
            _suppress = true;
            Ammo = ViceCityWeapons.Get(weapon).DefaultAmmo;
            _suppress = false;
        }

        _apply(this);
    }

    partial void OnAmmoChanged(int value)
    {
        if (!_suppress)
        {
            _apply(this);
        }
    }
}

public sealed partial class VcPlayerPageViewModel : ViceCityPage
{
    public VcPlayerPageViewModel(MainWindowViewModel shell) : base(shell)
    {
        Weapons = new(Enumerable.Range(0, ViceCityWeapons.SlotCount).Select(i => new WeaponSlotRow(i, ApplyWeapon)));
    }

    public override string Title => "Player";

    public override string Description =>
        "Tommy's money, health, armour, weapons, hidden packages and the perks earned from side jobs.";

    public override string Icon => "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm0 2c-4.4 0-8 2.2-8 5v1h16v-1c0-2.8-3.6-5-8-5Z";

    public ObservableCollection<WeaponSlotRow> Weapons { get; }

    [ObservableProperty]
    private int _money;

    [ObservableProperty]
    private decimal _health;

    [ObservableProperty]
    private decimal _armour;

    [ObservableProperty]
    private int _maxHealth;

    [ObservableProperty]
    private int _maxArmour;

    [ObservableProperty]
    private int _hiddenPackages;

    [ObservableProperty]
    private bool _infiniteSprint;

    [ObservableProperty]
    private bool _fastReload;

    [ObservableProperty]
    private bool _fireproof;

    [ObservableProperty]
    private bool _getOutOfJailFree;

    [ObservableProperty]
    private bool _freeHealthCare;

    [ObservableProperty]
    private bool _pedAvailable;

    public int TotalHiddenPackages => ViceCityCatalogue.PackageCount;

    public override void Refresh()
    {
        if (Save is not { } save)
        {
            return;
        }

        var player = save.PlayerInfo;
        Money = player.Money;
        MaxHealth = player.MaxHealth;
        MaxArmour = player.MaxArmour;
        HiddenPackages = player.HiddenPackagesCollected;
        InfiniteSprint = player.InfiniteSprint;
        FastReload = player.FastReload;
        Fireproof = player.Fireproof;
        GetOutOfJailFree = player.GetOutOfJailFree;
        FreeHealthCare = player.FreeHealthCare;

        PedAvailable = save.PlayerPed.IsAvailable;
        if (PedAvailable)
        {
            Health = ToDecimal(save.PlayerPed.Health);
            Armour = ToDecimal(save.PlayerPed.Armour);
            foreach (var row in Weapons)
            {
                row.Load(save.PlayerPed.Weapons[row.Slot]);
            }
        }
    }

    partial void OnMoneyChanged(int value) => Edit(() => Save!.PlayerInfo.SetMoney(value));

    partial void OnHealthChanged(decimal value) => Edit(() =>
    {
        if (Save!.PlayerPed.IsAvailable)
        {
            Save.PlayerPed.Health = (float)value;
        }
    });

    partial void OnArmourChanged(decimal value) => Edit(() =>
    {
        if (Save!.PlayerPed.IsAvailable)
        {
            Save.PlayerPed.Armour = (float)value;
        }
    });

    partial void OnMaxHealthChanged(int value) => Edit(() => Save!.PlayerInfo.MaxHealth = Math.Clamp(value, 0, 255));

    partial void OnMaxArmourChanged(int value) => Edit(() => Save!.PlayerInfo.MaxArmour = Math.Clamp(value, 0, 255));

    partial void OnHiddenPackagesChanged(int value) => Edit(() => Editor!.SetPackages(value));

    partial void OnInfiniteSprintChanged(bool value) => Edit(() => Save!.PlayerInfo.InfiniteSprint = value);

    partial void OnFastReloadChanged(bool value) => Edit(() => Save!.PlayerInfo.FastReload = value);

    partial void OnFireproofChanged(bool value) => Edit(() => Save!.PlayerInfo.Fireproof = value);

    partial void OnGetOutOfJailFreeChanged(bool value) => Edit(() => Save!.PlayerInfo.GetOutOfJailFree = value);

    partial void OnFreeHealthCareChanged(bool value) => Edit(() => Save!.PlayerInfo.FreeHealthCare = value);

    private void ApplyWeapon(WeaponSlotRow row) => Edit(() =>
    {
        if (!Save!.PlayerPed.IsAvailable)
        {
            return;
        }

        var slot = Save.PlayerPed.Weapons[row.Slot];
        if (row.Selected?.Weapon is { } weapon)
        {
            slot.Set(weapon, Math.Max(0, row.Ammo));
        }
        else
        {
            slot.Clear();
        }
    });

    [RelayCommand]
    private void GiveMillion() => Money = (int)Math.Min(int.MaxValue, (long)Money + 1_000_000);

    [RelayCommand]
    private void MaxOut() => Edit(() =>
    {
        Save!.PlayerInfo.MaxHealth = 200;
        Save.PlayerInfo.MaxArmour = 200;
        if (Save.PlayerPed.IsAvailable)
        {
            Save.PlayerPed.Health = 200;
            Save.PlayerPed.Armour = 200;
        }
    });

    [RelayCommand]
    private void CollectAllPackages() => HiddenPackages = TotalHiddenPackages;
}

public sealed class VcMissionsPageViewModel(MainWindowViewModel shell) : ToggleGroupsPageViewModel(shell)
{
    public override string Title => "Story missions";

    public override string Description =>
        "Completion flags for every contact and asset strand, matched to the mission scripts listed in the save.";

    public override string Icon => "M4 4h16v2H4V4Zm0 5h16v2H4V9Zm0 5h10v2H4v-2Zm0 5h10v2H4v-2Z";

    public override bool IsAvailable => Shell.ViceCity is not null;

    public override void Refresh()
    {
        if (Shell.VcProgression is not { } editor)
        {
            Groups.Clear();
            OverallSummary = "—";
            return;
        }

        if (Groups.Count == 0)
        {
            foreach (var strand in ViceCityCatalogue.Strands)
            {
                var items = strand.Entries.Where(editor.Has).Select(m => new ToggleItem(
                    m.Name, m.Note, m.Flag, editor.IsSet(m),
                    value => Edit(() => Shell.VcProgression!.Set(m, value))));
                Groups.Add(new ToggleGroup(strand.Name, strand.Subtitle, items));
            }
        }
        else
        {
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    item.SetWithoutApplying(editor.IsSet(new FlagEntry(item.Name, item.Flag)));
                }

                group.UpdateSummary();
            }
        }

        var all = ViceCityCatalogue.Strands.SelectMany(s => s.Entries).Where(editor.Has).ToList();
        OverallSummary = $"{editor.Count(all)} of {all.Count} story missions complete";
    }

    protected override void OnCompleteEverything() => Edit(() =>
        Shell.VcProgression!.SetAll(ViceCityCatalogue.Strands.SelectMany(s => s.Entries), true));
}

/// <summary>Tabbed flag page whose tabs come straight from catalogue lists.</summary>
public abstract class VcFlagTabsPageViewModel(MainWindowViewModel shell) : ActivityTabsPageViewModel(shell)
{
    private readonly List<IReadOnlyList<FlagEntry>> _entries = [];

    public override bool IsAvailable => Shell.ViceCity is not null;

    protected abstract IEnumerable<(string Name, string Description, IReadOnlyList<FlagEntry> Entries)> Definitions { get; }

    public override void Refresh()
    {
        if (Shell.VcProgression is not { } editor || Shell.ViceCity is not { } save)
        {
            Tabs.Clear();
            _entries.Clear();
            SelectedTab = null;
            return;
        }

        if (Tabs.Count == 0)
        {
            foreach (var (name, description, entries) in Definitions)
            {
                var present = entries.Where(editor.Has).ToList();
                _entries.Add(present);
                Tabs.Add(new ActivityTab(name, description, present.Select(e => new ToggleItem(
                    e.Name, e.Note, e.Flag, editor.IsSet(e),
                    value => Edit(() => Shell.VcProgression!.Set(e, value))))));
            }

            SelectedTab = Tabs.FirstOrDefault();
        }
        else
        {
            foreach (var (tab, entries) in Tabs.Zip(_entries))
            {
                foreach (var (item, entry) in tab.Items.Zip(entries))
                {
                    item.SetWithoutApplying(editor.IsSet(entry));
                }

                tab.Update();
            }
        }

        RefreshCounters(save, editor);
    }

    protected virtual void RefreshCounters(ViceCitySaveFile save, ViceCityProgressionEditor editor)
    {
    }

    protected override void OnSetSelected(bool value)
    {
        if (SelectedTab is null)
        {
            return;
        }

        var entries = _entries[Tabs.IndexOf(SelectedTab)];
        Edit(() => Shell.VcProgression!.SetAll(entries, value));
    }

    protected static string Int(GlobalVariableSpace globals, string name) =>
        globals.Has(name) ? globals.GetInt(name).ToString("N0", CultureInfo.InvariantCulture) : "—";
}

public sealed class VcActivitiesPageViewModel(MainWindowViewModel shell) : VcFlagTabsPageViewModel(shell)
{
    public override string Title => "Side activities";

    public override string Description =>
        "Rampages, unique jumps, vehicle jobs, races, challenges and store robberies, each on its own tab.";

    public override string Icon => "M12 2 2 7l10 5 10-5-10-5Zm0 9L2 16l10 5 10-5-10-5Z";

    protected override IEnumerable<(string, string, IReadOnlyList<FlagEntry>)> Definitions =>
    [
        ("Rampages", "35 killing sprees. Completing one updates the running total, the next reward and the stats menu figure.", ViceCityCatalogue.Rampages),
        ("Unique jumps", "36 stunt jumps. The script's cash reward holds the next payout, 100 × (jumps + 1).", ViceCityCatalogue.UniqueJumps),
        ("Vehicle jobs", "Completion markers for the vehicle side missions. Levels and totals are editable on the Stats page under Script counters.", ViceCityCatalogue.VehicleJobs),
        ("Races & stadium", "Sunshine Autos street races and the Hyman Memorial Stadium events.", ViceCityCatalogue.Races),
        ("Challenges", "Chopper checkpoints, Top Fun RC missions, dirt-bike trials and the shooting range.", ViceCityCatalogue.Challenges),
        ("Store robberies", "The fifteen stores that can be held up. The stats menu's \"Stores knocked off\" follows the count.", ViceCityCatalogue.Robberies),
    ];

    protected override void RefreshCounters(ViceCitySaveFile save, ViceCityProgressionEditor editor)
    {
        var g = save.Globals;
        var s = save.Stats;
        SetCounters(Tabs[0], [
            new StatRow("Total completed", Int(g, "TOTAL_RAMPAGES_PASSED")),
            new StatRow("Reward value", Int(g, "RAMPAGE_REWARD")),
            new StatRow("Stats menu figure", s.GetInt32(ViceCityStatsBlock.KillFrenziesPassedOffset).ToString()),
        ]);
        SetCounters(Tabs[1], [
            new StatRow("Total completed", Int(g, "TOTAL_COMPLETED_USJ")),
            new StatRow("Next reward", Int(g, "CASH_REWARD_USJ")),
            new StatRow("Stats menu figure", s.GetInt32(ViceCityStatsBlock.UniqueJumpsFoundOffset).ToString()),
        ]);
        SetCounters(Tabs[2], [
            new StatRow("Paramedic level", Int(g, "AMBULANCE_LEVEL")),
            new StatRow("Vigilante level", Int(g, "COPCAR_LEVEL")),
            new StatRow("Firefighter level", Int(g, "FIRETRUCK_LEVEL")),
            new StatRow("Taxi fares", Int(g, "TAXI_PASSED")),
            new StatRow("Pizzas delivered", Int(g, "PIZZA_DELIVERED")),
        ]);
        SetCounters(Tabs[5], [
            new StatRow("Stores knocked off", s.GetSingle(ViceCityStatsBlock.StoresKnockedOffOffset).ToString("0")),
        ]);
    }
}

public sealed class VcEmpirePageViewModel(MainWindowViewModel shell) : VcFlagTabsPageViewModel(shell)
{
    public override string Title => "Empire";

    public override string Description =>
        "Business assets and safehouses. These are the script's ownership flags; the game re-reads them on load.";

    public override string Icon => "M3 21V9l9-6 9 6v12h-6v-7H9v7H3Z";

    protected override IEnumerable<(string, string, IReadOnlyList<FlagEntry>)> Definitions =>
    [
        ("Business assets", "Asset ownership flags. Marking an asset acquired does not replay its asset missions; complete those on the Story missions page for the property to pay out.", ViceCityCatalogue.Assets),
        ("Safehouses", "Purchasable safehouses. Marking one bought tells the script it is owned; the 'for sale' pickup may still show until the game refreshes it.", ViceCityCatalogue.Safehouses),
    ];

    protected override void RefreshCounters(ViceCitySaveFile save, ViceCityProgressionEditor editor)
    {
        var owned = Enumerable.Range(0, ViceCityStatsBlock.PropertyCount).Count(save.Stats.IsPropertyOwned);
        SetCounters(Tabs[0], [
            new StatRow("Properties in stats menu", save.Stats.GetInt32(ViceCityStatsBlock.NumPropertyOwnedOffset).ToString()),
            new StatRow("Property slots set", $"{owned} / {ViceCityStatsBlock.PropertyCount}"),
        ]);
    }
}

public sealed class VcStatsPageViewModel(MainWindowViewModel shell) : ViceCityPage(shell)
{
    public override string Title => "Stats";

    public override string Description =>
        "Every figure the in-game stats menu shows, plus the script counters behind the vehicle jobs.";

    public override string Icon => "M4 20h3v-8H4v8Zm6.5 0h3V4h-3v16Zm6.5 0h3v-12h-3v12Z";

    public ObservableCollection<NumberGroup> Groups { get; } = [];

    public override void Refresh()
    {
        if (Save is not { } save || Editor is not { } editor)
        {
            Groups.Clear();
            return;
        }

        if (Groups.Count == 0)
        {
            Build(save, editor);
            return;
        }

        var stats = save.Stats;
        var fields = ViceCityStatsBlock.Fields.GroupBy(f => f.Group).ToList();
        var index = 0;
        foreach (var group in fields)
        {
            foreach (var (item, field) in Groups[index].Items.Zip(group))
            {
                item.SetWithoutApplying(ToDecimal(stats.Read(field)));
            }

            index++;
        }

        foreach (var (item, pedType) in Groups[index].Items.Zip(Enumerable.Range(0, ViceCityStatsBlock.PedTypeCount)))
        {
            item.SetWithoutApplying(stats.GetPedsKilled(pedType));
        }

        index++;
        foreach (var (item, counter) in Groups[index].Items.Zip(ViceCityCatalogue.Counters.Where(editor.Has)))
        {
            item.SetWithoutApplying(editor.GetCounter(counter));
        }
    }

    private void Build(ViceCitySaveFile save, ViceCityProgressionEditor editor)
    {
        var stats = save.Stats;
        foreach (var group in ViceCityStatsBlock.Fields.GroupBy(f => f.Group))
        {
            Groups.Add(new NumberGroup(group.Key, null, group.Select(field => new EditableNumber(
                field.Name, field.Hint, field.Kind == StatKind.Float, ToDecimal(stats.Read(field)),
                v => Edit(() => Shell.ViceCity!.Stats.Write(field, (double)v))))));
        }

        Groups.Add(new NumberGroup(
            "Kills by type",
            "Feeds the stats menu breakdown of people wasted.",
            Enumerable.Range(0, ViceCityStatsBlock.PedTypeCount)
                .Where(i => !ViceCityStatsBlock.PedTypeNames[i].StartsWith("Unused", StringComparison.Ordinal))
                .Select(i => new EditableNumber(
                    ViceCityStatsBlock.PedTypeNames[i], null, false, stats.GetPedsKilled(i),
                    v => Edit(() => Shell.ViceCity!.Stats.SetPedsKilled(i, (int)v))))));

        Groups.Add(new NumberGroup(
            "Script counters",
            "Values the mission script keeps for the vehicle jobs. Level 12 completes Paramedic, Vigilante and Firefighter.",
            ViceCityCatalogue.Counters.Where(editor.Has).Select(c => new EditableNumber(
                c.Name, c.Hint, false, editor.GetCounter(c),
                v => Edit(() => Shell.VcProgression!.SetCounter(c, (int)v))))));
    }
}

public sealed partial class VcWorldPageViewModel(MainWindowViewModel shell) : ViceCityPage(shell)
{
    public override string Title => "World";

    public override string Description => "The game clock and weather the save resumes with.";

    public override string Icon => "M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18Zm0 2c.9 1.2 1.6 3 1.9 5H10.1c.3-2 1-3.8 1.9-5Z";

    public IReadOnlyList<ViceCityWeather> WeatherOptions { get; } = Enum.GetValues<ViceCityWeather>();

    [ObservableProperty]
    private int _hour;

    [ObservableProperty]
    private int _minute;

    [ObservableProperty]
    private ViceCityWeather _weather;

    [ObservableProperty]
    private bool _forceWeather;

    [ObservableProperty]
    private string _cameraPosition = "—";

    public override void Refresh()
    {
        if (Save is not { } save)
        {
            return;
        }

        var vars = save.SimpleVars;
        Hour = vars.Hour;
        Minute = vars.Minute;
        Weather = vars.CurrentWeather;
        ForceWeather = vars.ForcedWeather != ViceCitySimpleVars.WeatherNotForced;
        CameraPosition = $"{vars.CameraX:0.0}, {vars.CameraY:0.0}, {vars.CameraZ:0.0}";
    }

    partial void OnHourChanged(int value) => Edit(() => Save!.SimpleVars.Hour = Math.Clamp(value, 0, 23));

    partial void OnMinuteChanged(int value) => Edit(() => Save!.SimpleVars.Minute = Math.Clamp(value, 0, 59));

    partial void OnWeatherChanged(ViceCityWeather value) => Edit(() =>
    {
        Save!.SimpleVars.SetWeather(value);
        if (ForceWeather)
        {
            Save.SimpleVars.ForcedWeather = (ushort)value;
        }
    });

    partial void OnForceWeatherChanged(bool value) => Edit(() =>
        Save!.SimpleVars.ForcedWeather = value ? (ushort)Weather : ViceCitySimpleVars.WeatherNotForced);
}
