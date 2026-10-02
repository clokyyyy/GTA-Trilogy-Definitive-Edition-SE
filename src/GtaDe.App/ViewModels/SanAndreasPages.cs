using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GtaDe.Definitions;
using GtaDe.Editing;
using GtaDe.SaveFormat;

namespace GtaDe.App.ViewModels;

/// <summary>Shared helpers for the San Andreas pages.</summary>
public abstract class SanAndreasPage(MainWindowViewModel shell) : PageViewModel(shell)
{
    public override bool IsAvailable => Shell.SanAndreas is not null;

    protected SanAndreasSaveFile? Save => Shell.SanAndreas;

    protected SanAndreasProgressionEditor? Editor => Shell.SaProgression;

    protected static decimal ToDecimal(double value) =>
        double.IsFinite(value) ? (decimal)Math.Clamp(value, 0, 999_999_999) : 0m;

    protected static void Tile(SummaryTile tile, int current, int total)
    {
        tile.Value = $"{current} / {total}";
        tile.Progress = total <= 0 ? 0 : Math.Clamp(current * 100.0 / total, 0, 100);
    }

    /// <summary>Builds an editable number bound to one stat.</summary>
    protected EditableNumber StatNumber(StatEntry entry) => new(
        entry.Name, entry.Hint, SanAndreasStatsBlock.IsFloatStat(entry.Id), ToDecimal(Save!.Stats.Get(entry.Id)),
        v => Edit(() => Shell.SanAndreas!.Stats.Set(entry.Id, (double)v)));
}

public sealed partial class SaDashboardPageViewModel : SanAndreasPage
{
    public SaDashboardPageViewModel(MainWindowViewModel shell) : base(shell)
    {
        Tiles =
        [
            Completion = new SummaryTile("Game completed", "Of 100%") { ShowProgress = true },
            Money = new SummaryTile("Money", "In hand"),
            Missions = new SummaryTile("Story missions", "Passed") { ShowProgress = true },
            Tags = new SummaryTile("Gang tags", "Sprayed") { ShowProgress = true },
            Snapshots = new SummaryTile("Snapshots", "Taken") { ShowProgress = true },
            Horseshoes = new SummaryTile("Horseshoes", "Collected") { ShowProgress = true },
            Oysters = new SummaryTile("Oysters", "Collected") { ShowProgress = true },
            Safehouses = new SummaryTile("Safehouses", "Bought") { ShowProgress = true },
        ];
    }

    public override string Title => "Dashboard";

    public override string Description => "An overview of this San Andreas save and what is left to do.";

    public override string Icon => "M4 13h6V4H4v9Zm0 7h6v-5H4v5Zm8 0h6V11h-6v9Zm0-16v5h6V4h-6Z";

    public override string Group => "Overview";

    public ObservableCollection<SummaryTile> Tiles { get; }

    public Services.GameProfile Game => Shell.SelectedGame;

    public SummaryTile Completion { get; }

    public SummaryTile Money { get; }

    public SummaryTile Missions { get; }

    public SummaryTile Tags { get; }

    public SummaryTile Snapshots { get; }

    public SummaryTile Horseshoes { get; }

    public SummaryTile Oysters { get; }

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
        var total = stats.GetFloatStat(1);
        var percent = total <= 0 ? 0 : Math.Clamp(stats.GetFloatStat(0) * 100.0 / total, 0, 100);
        Completion.Value = $"{percent:0.#}%";
        Completion.Progress = percent;

        Money.Value = save.PlayerInfo.Money.ToString("C0", CultureInfo.GetCultureInfo("en-US"));

        var (done, all) = editor.StoryProgress();
        Tile(Missions, done, all);
        Tile(Tags, editor.TagsSprayed, SanAndreasTagsBlock.TotalTags);
        var collectibles = SanAndreasCatalogue.Collectibles;
        Tile(Snapshots, editor.GetCollected(collectibles[0].Stat), collectibles[0].Total);
        Tile(Horseshoes, editor.GetCollected(collectibles[1].Stat), collectibles[1].Total);
        Tile(Oysters, editor.GetCollected(collectibles[2].Stat), collectibles[2].Total);
        Tile(Safehouses, editor.Count(SanAndreasCatalogue.Safehouses), SanAndreasCatalogue.Safehouses.Count);

        LastMission = string.IsNullOrWhiteSpace(save.LastMissionKey) ? "None yet" : save.LastMissionKey;
        SavedAt = save.SavedAtUtc.ToLocalTime().ToString("dddd d MMMM yyyy, HH:mm");
        GameClock = $"{save.SimpleVars.Hour:00}:{save.SimpleVars.Minute:00}";
        PlayTime = $"{stats.GetIntStat(134)} in-game days";
        FilePath = save.Path ?? string.Empty;
        SealWasValid = save.SealWasValid;
        Headline = $"{Completion.Value} complete  ·  {Money.Value}  ·  {Missions.Value} story missions";
        OnPropertyChanged(nameof(Game));
    }
}

/// <summary>One weapon option for a San Andreas slot's combo box.</summary>
public sealed record SaWeaponOption(SanAndreasWeapon? Weapon, string Name)
{
    public override string ToString() => Name;
}

/// <summary>One of CJ's thirteen weapon slots.</summary>
public sealed partial class SaWeaponSlotRow : ObservableObject
{
    private readonly Action<SaWeaponSlotRow> _apply;
    private bool _suppress;

    [ObservableProperty]
    private SaWeaponOption? _selected;

    [ObservableProperty]
    private int _ammo;

    public SaWeaponSlotRow(int slot, Action<SaWeaponSlotRow> apply)
    {
        Slot = slot;
        _apply = apply;
        Options =
        [
            new SaWeaponOption(null, "Empty"),
            .. SanAndreasWeapons.ForSlot(slot).Select(w => new SaWeaponOption(w.Weapon, w.Name)),
        ];
    }

    public int Slot { get; }

    public string SlotName => SanAndreasWeapons.SlotNames[Slot];

    public IReadOnlyList<SaWeaponOption> Options { get; }

    public bool UsesAmmo => Selected?.Weapon is { } w && SanAndreasWeapons.Get(w).UsesAmmo;

    public void Load(SanAndreasWeaponSlot slot)
    {
        _suppress = true;
        Selected = slot.IsEmpty ? Options[0] : Options.FirstOrDefault(o => o.Weapon == slot.Type) ?? Options[0];
        Ammo = slot.AmmoTotal;
        _suppress = false;
        OnPropertyChanged(nameof(UsesAmmo));
    }

    partial void OnSelectedChanged(SaWeaponOption? value)
    {
        OnPropertyChanged(nameof(UsesAmmo));
        if (_suppress)
        {
            return;
        }

        if (value?.Weapon is { } weapon && Ammo <= 1)
        {
            _suppress = true;
            Ammo = SanAndreasWeapons.Get(weapon).DefaultAmmo;
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

public sealed partial class SaPlayerPageViewModel : SanAndreasPage
{
    public SaPlayerPageViewModel(MainWindowViewModel shell) : base(shell)
    {
        Weapons = new(Enumerable.Range(0, SanAndreasWeapons.SlotCount).Select(i => new SaWeaponSlotRow(i, ApplyWeapon)));
    }

    public override string Title => "Player";

    public override string Description =>
        "CJ's money, health, armour, weapons and the perks earned from the vehicle sub-missions.";

    public override string Icon => "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm0 2c-4.4 0-8 2.2-8 5v1h16v-1c0-2.8-3.6-5-8-5Z";

    public ObservableCollection<SaWeaponSlotRow> Weapons { get; }

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
    private bool _driveByAllowed;

    [ObservableProperty]
    private bool _pedAvailable;

    [ObservableProperty]
    private string _position = "—";

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
        InfiniteSprint = player.InfiniteSprint;
        FastReload = player.FastReload;
        Fireproof = player.Fireproof;
        GetOutOfJailFree = player.GetOutOfJailFree;
        FreeHealthCare = player.FreeHealthCare;
        DriveByAllowed = player.DriveByAllowed;

        PedAvailable = save.PlayerPed.IsAvailable;
        if (PedAvailable)
        {
            Health = ToDecimal(save.PlayerPed.Health);
            Armour = ToDecimal(save.PlayerPed.Armour);
            Position = $"{save.PlayerPed.PositionX:0.0}, {save.PlayerPed.PositionY:0.0}, {save.PlayerPed.PositionZ:0.0}";
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

    partial void OnMaxHealthChanged(int value) => Edit(() => Save!.PlayerInfo.MaxHealth = value);

    partial void OnMaxArmourChanged(int value) => Edit(() => Save!.PlayerInfo.MaxArmour = value);

    partial void OnInfiniteSprintChanged(bool value) => Edit(() => Save!.PlayerInfo.InfiniteSprint = value);

    partial void OnFastReloadChanged(bool value) => Edit(() => Save!.PlayerInfo.FastReload = value);

    partial void OnFireproofChanged(bool value) => Edit(() => Save!.PlayerInfo.Fireproof = value);

    partial void OnGetOutOfJailFreeChanged(bool value) => Edit(() => Save!.PlayerInfo.GetOutOfJailFree = value);

    partial void OnFreeHealthCareChanged(bool value) => Edit(() => Save!.PlayerInfo.FreeHealthCare = value);

    partial void OnDriveByAllowedChanged(bool value) => Edit(() => Save!.PlayerInfo.DriveByAllowed = value);

    private void ApplyWeapon(SaWeaponSlotRow row) => Edit(() =>
    {
        if (!Save!.PlayerPed.IsAvailable)
        {
            return;
        }

        var slot = Save.PlayerPed.Weapons[row.Slot];
        if (row.Selected?.Weapon is { } weapon)
        {
            slot.Set(weapon, Math.Max(1, row.Ammo));
        }
        else
        {
            slot.Clear();
        }
    });

    [RelayCommand]
    private void GiveMillion() => Money = (int)Math.Min(999_999_999, (long)Money + 1_000_000);

    [RelayCommand]
    private void MaxOut() => Edit(() =>
    {
        // Never lowers a value: a 100% save can already sit above the Paramedic/Vigilante caps.
        var info = Save!.PlayerInfo;
        info.MaxHealth = Math.Max(info.MaxHealth, 176);
        info.MaxArmour = Math.Max(info.MaxArmour, 150);
        Save.Stats.SetFloatStat(24, 1000);
        if (Save.PlayerPed.IsAvailable)
        {
            Save.PlayerPed.Health = Math.Max(Save.PlayerPed.Health, info.MaxHealth);
            Save.PlayerPed.Armour = Math.Max(Save.PlayerPed.Armour, info.MaxArmour);
        }
    });

    [RelayCommand]
    private void AllPerks() => Edit(() =>
    {
        var p = Save!.PlayerInfo;
        p.InfiniteSprint = p.FastReload = p.Fireproof = p.GetOutOfJailFree = p.FreeHealthCare = true;
    });
}

public sealed class SaMissionsPageViewModel(MainWindowViewModel shell) : ToggleGroupsPageViewModel(shell)
{
    private readonly List<Func<bool>> _readers = [];

    public override string Title => "Story missions";

    public override string Description =>
        "Every story strand, read from its mission counter. Passing a mission moves the strand past it; un-passing rewinds the strand to that mission.";

    public override string Icon => "M4 4h16v2H4V4Zm0 5h16v2H4V9Zm0 5h10v2H4v-2Zm0 5h10v2H4v-2Z";

    public override bool IsAvailable => Shell.SanAndreas is not null;

    public override void Refresh()
    {
        if (Shell.SaProgression is not { } editor)
        {
            Groups.Clear();
            _readers.Clear();
            OverallSummary = "—";
            return;
        }

        if (Groups.Count == 0)
        {
            foreach (var strand in SanAndreasCatalogue.Strands.Where(editor.Has))
            {
                var items = strand.Missions.Select(m =>
                {
                    _readers.Add(() => Shell.SaProgression!.IsComplete(strand, m));
                    return new ToggleItem(
                        m.Name, m.Note, $"{strand.Counter} > {m.Launch}", editor.IsComplete(strand, m),
                        value => Edit(() => Shell.SaProgression!.SetComplete(strand, m, value)));
                }).ToList();
                Groups.Add(new ToggleGroup(strand.Name, strand.Region, items));
            }

            var catalina = SanAndreasCatalogue.CatalinaRobberies.Append(SanAndreasCatalogue.KingInExile).Where(editor.Has).Select(f =>
            {
                _readers.Add(() => Shell.SaProgression!.IsSet(f));
                return new ToggleItem(f.Name, f.Note, f.Variable, editor.IsSet(f),
                    value => Edit(() => Shell.SaProgression!.Set(f, value)));
            }).ToList();
            Groups.Add(new ToggleGroup("Catalina", "Red County. First Date has no flag of its own and follows the robberies.", catalina));
        }
        else
        {
            var index = 0;
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    item.SetWithoutApplying(_readers[index++]());
                }

                group.UpdateSummary();
            }
        }

        var (done, total) = editor.StoryProgress();
        OverallSummary = $"{done} of {total} story missions complete";
    }

    protected override void OnCompleteEverything() => Edit(() =>
    {
        var editor = Shell.SaProgression!;
        foreach (var strand in SanAndreasCatalogue.Strands)
        {
            editor.SetStrandComplete(strand, true);
        }

        editor.SetAll(SanAndreasCatalogue.CatalinaRobberies.Append(SanAndreasCatalogue.KingInExile), true);
    });
}

/// <summary>Tabbed San Andreas flag page whose tabs come from catalogue sections.</summary>
public abstract class SaFlagTabsPageViewModel(MainWindowViewModel shell) : ActivityTabsPageViewModel(shell)
{
    private readonly List<IReadOnlyList<SanAndreasFlag>> _entries = [];

    public override bool IsAvailable => Shell.SanAndreas is not null;

    protected abstract IReadOnlyList<SanAndreasSection> Sections { get; }

    protected static string FlagLabel(SanAndreasFlag flag) =>
        flag.Element > 0 || flag.Variable == SanAndreasCatalogue.HouseArray ? $"{flag.Variable}[{flag.Element}]" : flag.Variable;

    public override void Refresh()
    {
        if (Shell.SaProgression is not { } editor || Shell.SanAndreas is not { } save)
        {
            Tabs.Clear();
            _entries.Clear();
            SelectedTab = null;
            return;
        }

        if (Tabs.Count == 0)
        {
            foreach (var section in Sections)
            {
                var present = section.Entries.Where(editor.Has).ToList();
                _entries.Add(present);
                Tabs.Add(new ActivityTab(section.Name, section.Description, present.Select(e => new ToggleItem(
                    e.Name, e.Note, FlagLabel(e), editor.IsSet(e),
                    value => Edit(() => Shell.SaProgression!.Set(e, value))))));
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

        foreach (var (tab, section) in Tabs.Zip(Sections))
        {
            SetCounters(tab, Counters(save, editor, section));
        }
    }

    protected virtual IEnumerable<StatRow> Counters(SanAndreasSaveFile save, SanAndreasProgressionEditor editor, SanAndreasSection section) =>
        (section.Stats ?? []).Select(id => new StatRow(SanAndreasStatNames.Get(id), save.Stats.Get(id).ToString("N0", CultureInfo.InvariantCulture)));

    protected override void OnSetSelected(bool value)
    {
        if (SelectedTab is null)
        {
            return;
        }

        var entries = _entries[Tabs.IndexOf(SelectedTab)];
        Edit(() => Shell.SaProgression!.SetAll(entries, value));
    }

    protected static StatRow Global(SanAndreasProgressionEditor editor, string label, string name) =>
        new(label, editor.HasVariable(name) ? editor.GetVariable(name).ToString("N0", CultureInfo.InvariantCulture) : "—");
}

public sealed class SaActivitiesPageViewModel(MainWindowViewModel shell) : SaFlagTabsPageViewModel(shell)
{
    public override string Title => "Side activities";

    public override string Description =>
        "Vehicle jobs, schools, races and challenges, gyms and wardrobe unlocks, each on its own tab.";

    public override string Icon => "M12 2 2 7l10 5 10-5-10-5Zm0 9L2 16l10 5 10-5-10-5Z";

    protected override IReadOnlyList<SanAndreasSection> Sections => SanAndreasCatalogue.ActivityTabs;

    protected override IEnumerable<StatRow> Counters(SanAndreasSaveFile save, SanAndreasProgressionEditor editor, SanAndreasSection section)
    {
        var rows = base.Counters(save, editor, section).ToList();
        switch (section.Id)
        {
            case "jobs":
                rows.Insert(0, Global(editor, "Taxi fares", "TAXI_PASSED"));
                rows.Add(Global(editor, "Trucking missions", "TRUCK.G_NTRUCKMISSIONSPASSED"));
                rows.Add(Global(editor, "Quarry missions (current run)", "QUARRY.G_NQUARRYMISSIONSPASSED"));
                break;
            case "races":
                rows.Insert(0, Global(editor, "Races won", "TOTAL_RACES_COMPLETED"));
                break;
        }

        return rows;
    }
}

public sealed class SaPropertiesPageViewModel(MainWindowViewModel shell) : SaFlagTabsPageViewModel(shell)
{
    public override string Title => "Safehouses";

    public override string Description =>
        "Every buyable house and hotel suite, by region. These are the script's ownership flags; the game places the save points from them on load.";

    public override string Icon => "M3 21V9l9-6 9 6v12h-6v-7H9v7H3Z";

    protected override IReadOnlyList<SanAndreasSection> Sections => SanAndreasCatalogue.SafehouseRegions;

    protected override IEnumerable<StatRow> Counters(SanAndreasSaveFile save, SanAndreasProgressionEditor editor, SanAndreasSection section)
    {
        var owned = editor.Count(SanAndreasCatalogue.Safehouses);
        return [new StatRow("Owned across the state", $"{owned} / {SanAndreasCatalogue.Safehouses.Count}")];
    }
}

public sealed partial class SaCollectiblesPageViewModel : SanAndreasPage
{
    public SaCollectiblesPageViewModel(MainWindowViewModel shell) : base(shell)
    {
    }

    public override string Title => "Collectibles";

    public override string Description =>
        "Gang tags, snapshots, horseshoes and oysters. Reaching each total writes the reward flag the script checks for the bonus.";

    public override string Icon => "M12 2 15 9l7 .6-5.3 4.6L18.3 21 12 17.3 5.7 21l1.6-6.8L2 9.6 9 9l3-7Z";

    public ObservableCollection<NumberGroup> Groups { get; } = [];

    public override void Refresh()
    {
        if (Save is null || Editor is not { } editor)
        {
            Groups.Clear();
            return;
        }

        if (Groups.Count == 0)
        {
            var items = new List<EditableNumber>
            {
                new("Gang tags", "Los Santos. Reward: spray-can, Tec-9s, Molotovs and an AK-47 at the Grove Street house.", false, editor.TagsSprayed,
                    v => Edit(() => Shell.SaProgression!.SetTags((int)v))),
            };
            items.AddRange(SanAndreasCatalogue.Collectibles.Select(c => new EditableNumber(
                c.Name, c.Note, false, editor.GetCollected(c.Stat),
                v => Edit(() => Shell.SaProgression!.SetCollected(c, (int)v)))));
            Groups.Add(new NumberGroup("Collected", "Tags are sprayed in order, so a partial count marks the first tags in the game's list.", items));
            return;
        }

        var group = Groups[0].Items;
        group[0].SetWithoutApplying(editor.TagsSprayed);
        foreach (var (item, c) in group.Skip(1).Zip(SanAndreasCatalogue.Collectibles))
        {
            item.SetWithoutApplying(editor.GetCollected(c.Stat));
        }
    }

    [RelayCommand]
    private void CollectEverything() => Edit(() =>
    {
        var editor = Shell.SaProgression!;
        editor.SetTags(SanAndreasTagsBlock.TotalTags);
        foreach (var c in SanAndreasCatalogue.Collectibles)
        {
            editor.SetCollected(c, c.Total);
        }
    });
}

public sealed class SaStatsPageViewModel(MainWindowViewModel shell) : SanAndreasPage(shell)
{
    private readonly List<(EditableNumber Item, int Id)> _bound = [];

    public override string Title => "Stats";

    public override string Description =>
        "CJ's body, skills, respect and girlfriend progress, followed by every other figure the stats menu keeps.";

    public override string Icon => "M4 20h3v-8H4v8Zm6.5 0h3V4h-3v16Zm6.5 0h3v-12h-3v12Z";

    public ObservableCollection<NumberGroup> Groups { get; } = [];

    public override void Refresh()
    {
        if (Save is not { } save)
        {
            Groups.Clear();
            _bound.Clear();
            return;
        }

        if (Groups.Count == 0)
        {
            Build();
            return;
        }

        foreach (var (item, id) in _bound)
        {
            item.SetWithoutApplying(ToDecimal(save.Stats.Get(id)));
        }
    }

    private void Build()
    {
        void Add(string name, string? note, IEnumerable<StatEntry> entries)
        {
            var items = entries.Select(e =>
            {
                var item = StatNumber(e);
                _bound.Add((item, e.Id));
                return item;
            }).ToList();
            Groups.Add(new NumberGroup(name, note, items));
        }

        Add("Body", "Fat and muscle change CJ's build; stamina and lung capacity are capped at 1000.", SanAndreasCatalogue.BodyStats);
        Add("Girlfriends", "Relationship progress out of 100.", SanAndreasCatalogue.GirlfriendStats);
        Add("Vehicle skills", "1000 is the maximum.", SanAndreasCatalogue.DrivingStats);
        Add("Respect", null, SanAndreasCatalogue.RespectStats);
        Add("Weapon skills", "Gangster at 200, hitman at 999 (dual-wield for pistols, sawn-offs and micro SMGs).", SanAndreasCatalogue.WeaponSkills);
        Add("Progress", null, SanAndreasCatalogue.ProgressStats);

        var shown = _bound.Select(b => b.Id).ToHashSet();
        Add("Every other stat", "The remaining stats-menu figures: distances, budgets, records and mission tallies.",
            SanAndreasStatNames.All.Keys.Where(id => !shown.Contains(id) && SanAndreasStatsBlock.IsValid(id))
                .Order()
                .Select(id => new StatEntry(id, SanAndreasStatNames.Get(id), 999_999_999)));
    }
}

/// <summary>A weather choice with a readable name.</summary>
public sealed record SaWeatherOption(SanAndreasWeather Weather, string Name)
{
    public override string ToString() => Name;
}

public sealed partial class SaWorldPageViewModel(MainWindowViewModel shell) : SanAndreasPage(shell)
{
    public override string Title => "World";

    public override string Description => "The game clock and weather the save resumes with.";

    public override string Icon => "M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18Zm0 2c.9 1.2 1.6 3 1.9 5H10.1c.3-2 1-3.8 1.9-5Z";

    public IReadOnlyList<SaWeatherOption> WeatherOptions { get; } = Enum.GetValues<SanAndreasWeather>()
        .Select(w => new SaWeatherOption(w, Regex.Replace(w.ToString(), "(?<=[a-z0-9])(?=[A-Z])", " ")))
        .ToList();

    [ObservableProperty]
    private int _hour;

    [ObservableProperty]
    private int _minute;

    [ObservableProperty]
    private SaWeatherOption? _weather;

    [ObservableProperty]
    private bool _forceWeather;

    [ObservableProperty]
    private string _cameraPosition = "—";

    [ObservableProperty]
    private string _date = "—";

    public override void Refresh()
    {
        if (Save is not { } save)
        {
            return;
        }

        var vars = save.SimpleVars;
        Hour = vars.Hour;
        Minute = vars.Minute;
        Weather = WeatherOptions.FirstOrDefault(o => o.Weather == vars.CurrentWeather) ?? WeatherOptions[0];
        ForceWeather = vars.ForcedWeather != SanAndreasSimpleVars.WeatherNotForced;
        CameraPosition = $"{vars.CameraX:0.0}, {vars.CameraY:0.0}, {vars.CameraZ:0.0}";
        Date = $"Day {vars.Day}, month {vars.Month}";
    }

    partial void OnHourChanged(int value) => Edit(() => Save!.SimpleVars.Hour = value);

    partial void OnMinuteChanged(int value) => Edit(() => Save!.SimpleVars.Minute = value);

    partial void OnWeatherChanged(SaWeatherOption? value) => Edit(() =>
    {
        if (value is null)
        {
            return;
        }

        Save!.SimpleVars.SetWeather(value.Weather);
        if (ForceWeather)
        {
            Save.SimpleVars.ForcedWeather = (short)value.Weather;
        }
    });

    partial void OnForceWeatherChanged(bool value) => Edit(() =>
        Save!.SimpleVars.ForcedWeather = value && Weather is { } w ? (short)w.Weather : SanAndreasSimpleVars.WeatherNotForced);
}
