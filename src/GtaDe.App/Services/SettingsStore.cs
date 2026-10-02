using System.Text.Json;
using System.Text.Json.Serialization;

namespace GtaDe.App.Services;

public enum ThemePreference
{
    System,
    Light,
    Dark,
}

/// <summary>Settings that persist between runs, stored next to the user's other app data.</summary>
public sealed class AppSettings
{
    public ThemePreference Theme { get; set; } = ThemePreference.Dark;

    /// <summary>Write a <c>.bak</c> copy before overwriting a save.</summary>
    public bool CreateBackups { get; set; } = true;

    /// <summary>Warn before saving while the game appears to be running.</summary>
    public bool WarnWhenGameRunning { get; set; } = true;

    /// <summary>Show the raw script globals page, which has no guard rails.</summary>
    public bool ShowAdvancedTools { get; set; } = true;

    public string? LastSaveDirectory { get; set; }

    /// <summary>Reopen each game's last save on start-up and when switching to that game.</summary>
    public bool ReopenLastSave { get; set; } = true;

    /// <summary>The save last opened for each game.</summary>
    public Dictionary<GameId, string> LastFiles { get; set; } = new();

    /// <summary>The game profile picked in the rail; drives artwork, accent colour and save folder.</summary>
    public GameId SelectedGame { get; set; } = GameId.Gta3;

    public List<string> RecentFiles { get; set; } = [];
}

/// <summary>Loads and stores <see cref="AppSettings"/> as JSON under the user's app data folder.</summary>
public sealed class SettingsStore
{
    private const int MaxRecentFiles = 10;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _path;

    public SettingsStore()
    {
        var folder = Environment.GetEnvironmentVariable("GTADE_SAVE_EDITOR_DATA") ?? ResolveConfigFolder();

        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception)
        {
            // Load and Save already tolerate a missing folder; the editor then runs on defaults.
        }

        _path = Path.Combine(folder, "settings.json");
        Current = Load();
    }

    /// <summary>Where settings live when no override is set.</summary>
    public string FilePath => _path;

    private static string LegacyFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GtaDeSaveEditor");

    /// <summary>
    /// Prefers a portable <c>config</c> folder beside the executable so the app's files stay
    /// together. Falls back to the user's app data when that folder is not writable (for example
    /// under Program Files). Settings from the old app-data location are carried over once.
    /// </summary>
    private static string ResolveConfigFolder()
    {
        var portable = Path.Combine(AppContext.BaseDirectory, "config");

        try
        {
            Directory.CreateDirectory(portable);
            var probe = Path.Combine(portable, ".write-test");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
        }
        catch (Exception)
        {
            return LegacyFolder;
        }

        var target = Path.Combine(portable, "settings.json");
        var legacy = Path.Combine(LegacyFolder, "settings.json");
        if (!File.Exists(target) && File.Exists(legacy))
        {
            try
            {
                File.Copy(legacy, target);
            }
            catch (Exception)
            {
                // Starting with defaults is fine if the old settings cannot be read.
            }
        }

        return portable;
    }

    public AppSettings Current { get; private set; }

    private AppSettings Load()
    {
        try
        {
            return File.Exists(_path) ? Parse(File.ReadAllText(_path)) : new AppSettings();
        }
        catch (Exception)
        {
            // A corrupt settings file must never stop the editor from starting.
            return new AppSettings();
        }
    }

    /// <summary>Reads settings JSON, repairing values that are valid JSON but not valid settings.</summary>
    public static AppSettings Parse(string json) =>
        Normalise(JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings());

    private static AppSettings Normalise(AppSettings settings)
    {
        settings.LastFiles ??= new();
        settings.RecentFiles ??= [];
        settings.RecentFiles.RemoveAll(string.IsNullOrWhiteSpace);

        foreach (var key in settings.LastFiles.Keys.ToList())
        {
            if (!Enum.IsDefined(key) || string.IsNullOrWhiteSpace(settings.LastFiles[key]))
            {
                settings.LastFiles.Remove(key);
            }
        }

        if (!Enum.IsDefined(settings.SelectedGame))
        {
            settings.SelectedGame = GameId.Gta3;
        }

        if (!Enum.IsDefined(settings.Theme))
        {
            settings.Theme = ThemePreference.Dark;
        }

        return settings;
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(Current, Options));
        }
        catch (Exception)
        {
            // Settings are a convenience; failing to persist them is not worth interrupting the user.
        }
    }

    public void RememberFile(string path, GameId? game = null)
    {
        Current.LastSaveDirectory = Path.GetDirectoryName(path);
        if (game is { } id)
        {
            Current.LastFiles[id] = path;
        }

        Current.RecentFiles.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        Current.RecentFiles.Insert(0, path);

        if (Current.RecentFiles.Count > MaxRecentFiles)
        {
            Current.RecentFiles.RemoveRange(MaxRecentFiles, Current.RecentFiles.Count - MaxRecentFiles);
        }

        Save();
    }
}
