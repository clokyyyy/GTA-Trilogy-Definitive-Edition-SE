using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GtaDe.App.Services;

namespace GtaDe.App.ViewModels;

public sealed partial class SettingsPageViewModel : PageViewModel
{
    private readonly SettingsStore _store;

    public SettingsPageViewModel(MainWindowViewModel shell, SettingsStore store) : base(shell)
    {
        _store = store;
        _theme = store.Current.Theme;
        _createBackups = store.Current.CreateBackups;
        _warnWhenGameRunning = store.Current.WarnWhenGameRunning;
        _showAdvancedTools = store.Current.ShowAdvancedTools;
        _reopenLastSave = store.Current.ReopenLastSave;
    }

    public override string Title => "Settings";

    public override string Description => "Appearance and the safety checks the editor performs.";

    public override string Icon => "M12 15.5A3.5 3.5 0 1 1 12 8.5a3.5 3.5 0 0 1 0 7Zm7.4-2.6.1-.9-.1-.9 2-1.5-2-3.4-2.3.9a7.6 7.6 0 0 0-1.6-.9L15 3H9l-.5 2.2c-.6.2-1.1.5-1.6.9l-2.3-.9-2 3.4 2 1.5-.1.9.1.9-2 1.5 2 3.4 2.3-.9c.5.4 1 .7 1.6.9L9 21h6l.5-2.2c.6-.2 1.1-.5 1.6-.9l2.3.9 2-3.4-2-1.5Z";

    public override string Group => "Advanced";

    public override bool IsAvailable => true;

    public ObservableCollection<ThemePreference> Themes { get; } =
        [ThemePreference.System, ThemePreference.Light, ThemePreference.Dark];

    [ObservableProperty]
    private ThemePreference _theme;

    [ObservableProperty]
    private bool _createBackups;

    [ObservableProperty]
    private bool _warnWhenGameRunning;

    [ObservableProperty]
    private bool _showAdvancedTools;

    [ObservableProperty]
    private bool _reopenLastSave;

    partial void OnReopenLastSaveChanged(bool value)
    {
        _store.Current.ReopenLastSave = value;
        _store.Save();
    }

    [ObservableProperty]
    private string _saveFolder = "Not found";

    [ObservableProperty]
    private string _gameRunningHint = string.Empty;

    [ObservableProperty]
    private string _gameNotes = string.Empty;

    private GameProfile? _shownGame;

    public override void Refresh()
    {
        // Every edit refreshes all pages, so the disk scan only runs when the game changes.
        var game = Shell.SelectedGame;
        if (ReferenceEquals(game, _shownGame))
        {
            return;
        }

        _shownGame = game;
        SaveFolder = GameLocator.FindSaveFolder(game.SaveFolderName, game.SavePattern) ?? "Not found";
        GameRunningHint = $"{game.ShortName} rewrites its save files while it is open, which silently discards edits.";
        GameNotes = game.Id == GameId.Gta3
            ? "What it cannot change: health, armour and weapons are not stored in a GTA III: Definitive Edition save. " +
              "The game restores the player's loadout from the mission script on load, so no editor can set them."
            : $"{game.ShortName} keeps the player's health, armour and weapons in the save, so the Player page edits them directly. " +
              "Achievements are not stored in any save; the platform awards them as you play.";
    }

    partial void OnThemeChanged(ThemePreference value)
    {
        _store.Current.Theme = value;
        _store.Save();
        Shell.ApplyTheme(value);
    }

    partial void OnCreateBackupsChanged(bool value)
    {
        _store.Current.CreateBackups = value;
        _store.Save();
    }

    partial void OnWarnWhenGameRunningChanged(bool value)
    {
        _store.Current.WarnWhenGameRunning = value;
        _store.Save();
    }

    partial void OnShowAdvancedToolsChanged(bool value)
    {
        _store.Current.ShowAdvancedTools = value;
        _store.Save();

        foreach (var page in Shell.Pages)
        {
            page.NotifyAvailabilityChanged();
        }
    }

    [RelayCommand]
    private void OpenSaveFolder()
    {
        if (!Directory.Exists(SaveFolder))
        {
            Shell.SetStatus("The game's save folder could not be found.", isError: true);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = SaveFolder,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Shell.SetStatus($"Could not open the folder: {ex.Message}", isError: true);
        }
    }
}
