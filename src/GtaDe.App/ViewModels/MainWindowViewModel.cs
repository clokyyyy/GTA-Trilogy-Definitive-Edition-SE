using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GtaDe.App.Services;
using GtaDe.Editing;
using GtaDe.SaveFormat;

namespace GtaDe.App.ViewModels;

/// <summary>A heading in the navigation rail and the pages filed under it.</summary>
public sealed class NavGroup(string name, IEnumerable<PageViewModel> pages)
{
    public string Name { get; } = name;

    public ObservableCollection<PageViewModel> Pages { get; } = new(pages);
}

/// <summary>One entry in the rail's game switcher.</summary>
public sealed partial class GameChoice(GameProfile profile) : ObservableObject
{
    public GameProfile Profile { get; } = profile;

    [ObservableProperty]
    private bool _isSelected;

    public string ToolTip => Profile.IsSupported ? Profile.Name : $"{Profile.Name} — coming soon";
}

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly SettingsStore _settings = new();
    private readonly SaveFileWatcher _watcher;

    [ObservableProperty]
    private DeSaveFile? _document;

    /// <summary>Set when the file on disk changed after it was opened here.</summary>
    [ObservableProperty]
    private bool _fileChangedOnDisk;

    [ObservableProperty]
    private PageViewModel? _currentPage;

    [ObservableProperty]
    private string _statusMessage = "Open a GTA III Definitive Edition save to begin.";

    [ObservableProperty]
    private bool _statusIsError;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private GameProfile _selectedGame = GameProfile.Get(GameId.Gta3);

    public MainWindowViewModel()
    {
        _watcher = new SaveFileWatcher(OnFileChangedOnDisk);

        PageViewModel globals = new GlobalsPageViewModel(this);
        PageViewModel research = new ResearchPageViewModel(this);
        PageViewModel settings = new SettingsPageViewModel(this, _settings);

        _gta3Pages =
        [
            new DashboardPageViewModel(this),
            new PlayerPageViewModel(this),
            new MissionsPageViewModel(this),
            new SideMissionsPageViewModel(this),
            new CollectiblesPageViewModel(this),
            new StatsPageViewModel(this),
            new WorldPageViewModel(this),
            new GaragesPageViewModel(this),
            globals,
            research,
            settings,
        ];

        _viceCityPages =
        [
            new VcDashboardPageViewModel(this),
            new VcPlayerPageViewModel(this),
            new VcMissionsPageViewModel(this),
            new VcActivitiesPageViewModel(this),
            new VcEmpirePageViewModel(this),
            new VcStatsPageViewModel(this),
            new VcWorldPageViewModel(this),
            globals,
            research,
            settings,
        ];

        _sanAndreasPages =
        [
            new SaDashboardPageViewModel(this),
            new SaPlayerPageViewModel(this),
            new SaMissionsPageViewModel(this),
            new SaActivitiesPageViewModel(this),
            new SaCollectiblesPageViewModel(this),
            new SaPropertiesPageViewModel(this),
            new SaStatsPageViewModel(this),
            new SaWorldPageViewModel(this),
            globals,
            research,
            settings,
        ];

        Pages = [];
        UsePageSet(GameKind.Gta3);
        ApplyTheme(_settings.Current.Theme);

        Games = new(GameProfile.All.Select(g => new GameChoice(g)));
        SelectedGame = GameProfile.Get(_settings.Current.SelectedGame);
        OnSelectedGameChanged(SelectedGame);

        if (RememberedSaveFor(SelectedGame) is { } last)
        {
            LoadFile(last, reopened: true);
        }
    }

    /// <summary>The save to reopen for a game, when that setting is on and the file still exists.</summary>
    private string? RememberedSaveFor(GameProfile profile)
    {
        if (!_settings.Current.ReopenLastSave || !profile.IsSupported)
        {
            return null;
        }

        if (_settings.Current.LastFiles.TryGetValue(profile.Id, out var path))
        {
            return File.Exists(path) ? path : null;
        }

        // Settings written before per-game memory existed: take the newest recent file named like this game's slots.
        var prefix = profile.SavePattern.Split('*')[0];
        return _settings.Current.RecentFiles.FirstOrDefault(f =>
            Path.GetFileName(f).StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && File.Exists(f));
    }

    private readonly IReadOnlyList<PageViewModel> _gta3Pages;
    private readonly IReadOnlyList<PageViewModel> _viceCityPages;
    private readonly IReadOnlyList<PageViewModel> _sanAndreasPages;
    private GameKind? _activePageSet;

    /// <summary>Every page of every game, used by tests and for refreshing.</summary>
    public IEnumerable<PageViewModel> AllPages => _gta3Pages.Concat(_viceCityPages).Concat(_sanAndreasPages).Distinct();

    /// <summary>Swaps the rail to the pages of one game. Shared pages (globals, research, settings) stay put.</summary>
    private void UsePageSet(GameKind game)
    {
        if (_activePageSet == game)
        {
            return;
        }

        _activePageSet = game;
        var set = game switch
        {
            GameKind.ViceCity => _viceCityPages,
            GameKind.SanAndreas => _sanAndreasPages,
            _ => _gta3Pages,
        };

        Pages.Clear();
        foreach (var page in set)
        {
            Pages.Add(page);
        }

        NavGroups.Clear();
        foreach (var group in Pages.GroupBy(p => p.Group))
        {
            NavGroups.Add(new NavGroup(group.Key, group));
        }

        if (CurrentPage is null || !Pages.Contains(CurrentPage))
        {
            CurrentPage = Pages[0];
        }
        else
        {
            OnCurrentPageChanged(CurrentPage);
        }

        RefreshAllPages();
    }

    public ObservableCollection<PageViewModel> Pages { get; }

    /// <summary>The three games of the trilogy, shown as a switcher at the top of the rail.</summary>
    public ObservableCollection<GameChoice> Games { get; }

    /// <summary>True when the editor pages should show: a supported game is selected and one of its saves is open.</summary>
    public bool ShowEditor => SelectedGame.IsSupported && Document is not null && Document.Game == SelectedGame.Kind;

    /// <summary>True when the "open a save" landing should show for the selected game.</summary>
    public bool ShowLanding => SelectedGame.IsSupported && !ShowEditor;

    /// <summary>True when the selected game cannot be edited yet.</summary>
    public bool ShowComingSoon => !SelectedGame.IsSupported;

    public string LandingHint =>
        $@"Open a save from Documents\Rockstar Games\{SelectedGame.SaveFolderName}\Profiles to begin. " +
        "The editor keeps a .bak copy of the original before it writes anything.";

    /// <summary>The rail entries bundled under their headings, so the list reads as sections.</summary>
    public ObservableCollection<NavGroup> NavGroups { get; } = [];

    /// <summary>Set while a page is reloading, so refreshing controls does not mark the save dirty.</summary>
    public bool IsRefreshing { get; private set; }

    public AppSettings Settings => _settings.Current;

    public ProgressionEditor? Progression { get; private set; }

    public ViceCityProgressionEditor? VcProgression { get; private set; }

    public SanAndreasProgressionEditor? SaProgression { get; private set; }

    /// <summary>The open GTA III save, or null when nothing (or another game's save) is open.</summary>
    public Gta3SaveFile? Save => Document as Gta3SaveFile;

    /// <summary>The open Vice City save, or null.</summary>
    public ViceCitySaveFile? ViceCity => Document as ViceCitySaveFile;

    /// <summary>The open San Andreas save, or null.</summary>
    public SanAndreasSaveFile? SanAndreas => Document as SanAndreasSaveFile;

    public bool HasSave => Document is not null;

    private string EditorTitle => Document is null
        ? $"{SelectedGame.ShortName} Definitive Edition Save Editor"
        : $"{GameProfile.For(Document.Game).ShortName} Definitive Edition Save Editor";

    public string WindowTitle => Document is null
        ? EditorTitle
        : $"{Path.GetFileName(Document.Path)}{(IsModified ? " *" : string.Empty)} — {EditorTitle}";

    /// <summary>Raised when a page needs the host window to ask the user something.</summary>
    public Func<string, string, Task<bool>>? ConfirmAsync { get; set; }

    /// <summary>Supplied by the window so view models can open file pickers without touching UI types.</summary>
    public IStorageProvider? StorageProvider { get; set; }

    partial void OnCurrentPageChanged(PageViewModel? value)
    {
        foreach (var page in Pages)
        {
            page.IsSelected = ReferenceEquals(page, value);
        }
    }

    partial void OnSelectedGameChanged(GameProfile value)
    {
        // Games is still null during the field initialiser; the constructor re-invokes this afterwards.
        if (Games is null)
        {
            return;
        }

        foreach (var choice in Games)
        {
            choice.IsSelected = ReferenceEquals(choice.Profile, value);
        }

        value.ApplyAccent(Avalonia.Application.Current);
        if (Document?.Game != value.Kind)
        {
            SetStatus($"Open a {value.ShortName} Definitive Edition save to begin.");
        }

        if (value.IsSupported)
        {
            UsePageSet(value.Kind);
        }

        if (_settings.Current.SelectedGame != value.Id)
        {
            _settings.Current.SelectedGame = value.Id;
            _settings.Save();
        }

        OnPropertyChanged(nameof(ShowEditor));
        OnPropertyChanged(nameof(ShowLanding));
        OnPropertyChanged(nameof(ShowComingSoon));
        OnPropertyChanged(nameof(LandingHint));
        OnPropertyChanged(nameof(WindowTitle));
        OpenCommand.NotifyCanExecuteChanged();
    }

    partial void OnDocumentChanged(DeSaveFile? value)
    {
        Progression = value is Gta3SaveFile gta3 ? new ProgressionEditor(gta3) : null;
        VcProgression = value is ViceCitySaveFile vc ? new ViceCityProgressionEditor(vc) : null;
        OnPropertyChanged(nameof(Save));
        SaProgression = value is SanAndreasSaveFile sa ? new SanAndreasProgressionEditor(sa) : null;
        OnPropertyChanged(nameof(ViceCity));
        OnPropertyChanged(nameof(SanAndreas));
        OnPropertyChanged(nameof(HasSave));
        OnPropertyChanged(nameof(ShowEditor));
        OnPropertyChanged(nameof(ShowLanding));
        OnPropertyChanged(nameof(WindowTitle));
        RefreshAllPages();
        NotifyCommands();
    }

    partial void OnIsModifiedChanged(bool value)
    {
        OnPropertyChanged(nameof(WindowTitle));
        RevertCommand.NotifyCanExecuteChanged();
    }

    public void NotifyEdited()
    {
        IsModified = Document?.IsModified ?? false;
        RefreshAllPages();
    }

    /// <summary>
    /// Reloads every page from the save buffer. Edits often ripple across pages — completing a
    /// Rampage changes the dashboard's completion percentage and the stats page too — so the
    /// simplest correct approach is to refresh everything rather than track dependencies.
    /// </summary>
    public void RefreshAllPages()
    {
        IsRefreshing = true;
        try
        {
            foreach (var page in Pages)
            {
                page.Refresh();
                page.NotifyAvailabilityChanged();
            }
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }

    public void ApplyTheme(ThemePreference preference)
    {
        var app = Avalonia.Application.Current;
        if (app is null)
        {
            return;
        }

        app.RequestedThemeVariant = preference switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }

    [RelayCommand]
    private void Navigate(PageViewModel page) => CurrentPage = page;

    [RelayCommand]
    private async Task SelectGameAsync(GameChoice choice)
    {
        var profile = choice.Profile;
        if (ReferenceEquals(profile, SelectedGame))
        {
            return;
        }

        // Switching to a game whose save is not open brings back the one used last time.
        if (RememberedSaveFor(profile) is { } last && Document?.Game != profile.Kind)
        {
            if (!await ConfirmDiscardAsync())
            {
                return;
            }

            SelectedGame = profile;
            LoadFile(last, reopened: true);
            return;
        }

        SelectedGame = profile;
    }

    private bool CanOpen() => SelectedGame.IsSupported;

    [RelayCommand(CanExecute = nameof(CanOpen))]
    private async Task OpenAsync()
    {
        if (StorageProvider is null)
        {
            return;
        }

        if (!await ConfirmDiscardAsync())
        {
            return;
        }

        var profile = SelectedGame;
        var remembered = _settings.Current.LastSaveDirectory;
        var start = remembered is not null && remembered.Contains(profile.SaveFolderName, StringComparison.OrdinalIgnoreCase)
            ? remembered
            : GameLocator.FindSaveFolder(profile.SaveFolderName, profile.SavePattern) ?? remembered;
        var startFolder = start is not null && Directory.Exists(start)
            ? await StorageProvider.TryGetFolderFromPathAsync(start)
            : null;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = $"Open a {profile.ShortName} Definitive Edition save",
            AllowMultiple = false,
            SuggestedStartLocation = startFolder,
            FileTypeFilter =
            [
                new FilePickerFileType($"{profile.ShortName} save") { Patterns = [profile.SavePattern, "*.sav"] },
                new FilePickerFileType("All files") { Patterns = ["*"] },
            ],
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            LoadFile(path);
        }
    }

    public void LoadFile(string path) => LoadFile(path, reopened: false);

    private void LoadFile(string path, bool reopened)
    {
        DeSaveFile? loaded = null;
        try
        {
            loaded = DeSaveFile.Load(path);
            var profile = GameProfile.For(loaded.Game);
            if (!profile.IsSupported)
            {
                SetStatus($"{Path.GetFileName(path)} is a {profile.Name} save, which the editor cannot edit yet.", isError: true);
                return;
            }

            Document = null;
            SelectedGame = profile;
            Document = loaded;
            IsModified = false;
            FileChangedOnDisk = false;
            _settings.RememberFile(path, profile.Id);
            _watcher.Watch(path);

            var warning = loaded.SealWasValid
                ? string.Empty
                : "  Warning: this file's integrity value did not match, so it may already have been edited by another tool.";

            var verb = reopened ? "Reopened" : "Loaded";
            SetStatus($"{verb} {Path.GetFileName(path)}.{warning}", !loaded.SealWasValid);
        }
        catch (Exception ex)
        {
            // A save that parses but breaks a page must not stay open half-loaded with Save enabled.
            if (loaded is not null && ReferenceEquals(Document, loaded))
            {
                _watcher.Stop();
                Document = null;
                IsModified = false;
                FileChangedOnDisk = false;
            }

            SetStatus($"Could not open {Path.GetFileName(path)}: {ex.Message}", isError: true);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task SaveAsync()
    {
        if (Document is null)
        {
            return;
        }

        if (_settings.Current.WarnWhenGameRunning && GameLocator.IsGameRunning() && ConfirmAsync is not null)
        {
            var proceed = await ConfirmAsync(
                "The game appears to be running",
                "The Definitive Edition rewrites its save files while it is running, so it can overwrite these changes " +
                "without warning. Quit to the desktop first, then save.\n\nWrite the file anyway?");

            if (!proceed)
            {
                return;
            }
        }

        try
        {
            using (_watcher.Suppress())
            {
                Document.Save(createBackup: _settings.Current.CreateBackups);
            }

            IsModified = false;
            FileChangedOnDisk = false;

            var note = _settings.Current.CreateBackups ? " The original was kept as .bak." : string.Empty;
            SetStatus($"Saved {Path.GetFileName(Document.Path)}.{note}");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not save: {ex.Message}", isError: true);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task SaveAsAsync()
    {
        if (Document is null || StorageProvider is null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save a copy",
            SuggestedFileName = Path.GetFileName(Document.Path) ?? "GTA3sf1.sav",
            DefaultExtension = "sav",
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            using (_watcher.Suppress())
            {
                Document.Save(path, createBackup: false);
            }

            IsModified = false;
            FileChangedOnDisk = false;
            _watcher.Watch(path);
            _settings.RememberFile(path, GameProfile.For(Document.Game).Id);
            SetStatus($"Saved a copy to {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not save: {ex.Message}", isError: true);
        }
    }

    [RelayCommand(CanExecute = nameof(IsModified))]
    private void Revert()
    {
        if (Document is null)
        {
            return;
        }

        Document.Revert();
        IsModified = false;
        RefreshAllPages();
        SetStatus("Reverted every change back to the file on disk.");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private void Reload()
    {
        if (Document?.Path is { } path)
        {
            LoadFile(path);
        }
    }

    private void OnFileChangedOnDisk()
    {
        if (Document is null)
        {
            return;
        }

        FileChangedOnDisk = true;

        SetStatus(
            IsModified
                ? "This file changed on disk while you were editing it — probably the game writing a save. Saving now will overwrite what the game wrote."
                : "This file changed on disk. Use Reload to pick up the new version.",
            isError: true);
    }

    private async Task<bool> ConfirmDiscardAsync()
    {
        if (!IsModified || ConfirmAsync is null)
        {
            return true;
        }

        return await ConfirmAsync(
            "Discard unsaved changes?",
            "This save has edits that have not been written to disk yet.");
    }

    private void NotifyCommands()
    {
        SaveCommand.NotifyCanExecuteChanged();
        SaveAsCommand.NotifyCanExecuteChanged();
        RevertCommand.NotifyCanExecuteChanged();
        ReloadCommand.NotifyCanExecuteChanged();
    }
}
