using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;

namespace GtaDe.App.Services;

public enum GameId
{
    Gta3,
    ViceCity,
    SanAndreas,
}

/// <summary>
/// Branding and support status for one game of the trilogy: its artwork, its accent colours in
/// both themes, and whether the editor can open its saves yet.
/// </summary>
public sealed class GameProfile
{
    private readonly string _assetFolder;
    private Bitmap? _banner;
    private Bitmap? _logo;
    private Bitmap? _icon;
    private WindowIcon? _windowIcon;

    private GameProfile(
        GameId id,
        string name,
        string shortName,
        string assetFolder,
        bool isSupported,
        string saveFolderName,
        string savePattern,
        string tagline,
        AccentSet dark,
        AccentSet light)
    {
        Id = id;
        Name = name;
        ShortName = shortName;
        _assetFolder = assetFolder;
        IsSupported = isSupported;
        SaveFolderName = saveFolderName;
        SavePattern = savePattern;
        Tagline = tagline;
        Dark = dark;
        Light = light;
    }

    public GameId Id { get; }

    /// <summary>Full title, e.g. "Grand Theft Auto III".</summary>
    public string Name { get; }

    /// <summary>Compact title for tight spaces, e.g. "GTA III".</summary>
    public string ShortName { get; }

    public bool IsSupported { get; }

    public bool IsComingSoon => !IsSupported;

    /// <summary>The folder under <c>Documents\Rockstar Games</c> that holds this game's profiles.</summary>
    public string SaveFolderName { get; }

    /// <summary>File-name pattern of this game's save slots, e.g. <c>GTA3sf*.sav</c>.</summary>
    public string SavePattern { get; }

    /// <summary>The save-format family this profile edits.</summary>
    public SaveFormat.GameKind Kind => Id switch
    {
        GameId.ViceCity => SaveFormat.GameKind.ViceCity,
        GameId.SanAndreas => SaveFormat.GameKind.SanAndreas,
        _ => SaveFormat.GameKind.Gta3,
    };

    public static GameProfile For(SaveFormat.GameKind kind) => All.First(g => g.Kind == kind);

    public string Tagline { get; }

    public AccentSet Dark { get; }

    public AccentSet Light { get; }

    public Bitmap Banner => _banner ??= LoadBitmap("banner.jpg");

    public Bitmap Logo => _logo ??= LoadBitmap("logo.png");

    public Bitmap Icon => _icon ??= LoadBitmap("icon.png");

    public WindowIcon WindowIcon => _windowIcon ??= new WindowIcon(AssetLoader.Open(AssetUri("icon.ico")));

    public static IReadOnlyList<GameProfile> All { get; } =
    [
        new(GameId.Gta3, "Grand Theft Auto III", "GTA III", "Gta3", isSupported: true,
            "GTA III Definitive Edition", "GTA3sf*.sav",
            "Liberty City, 2001. Edit Claude's progress, missions, side jobs and collectibles.",
            dark: new("#F0A431", "#FFB849", "#D08A1F", "#3A2D16"),
            light: new("#C87A0A", "#E08E16", "#A66206", "#FBEFD9")),

        new(GameId.ViceCity, "Grand Theft Auto: Vice City", "Vice City",         "ViceCity", isSupported: true,
                    "GTA Vice City Definitive Edition", "GTAVCsf*.sav",
                    "Vice City, 1986. Edit Tommy Vercetti's empire: missions, assets, rampages, jumps and stats.",
            dark: new("#F2649E", "#FF7FB2", "#D24C84", "#3A1A2A"),
            light: new("#C2185B", "#D63A76", "#9C1249", "#FCE4EF")),

        new(GameId.SanAndreas, "Grand Theft Auto: San Andreas", "San Andreas",         "SanAndreas", isSupported: true,
            "GTA San Andreas Definitive Edition", "GTASAsf*.sav",
                    "San Andreas, 1992. Edit CJ's story, side activities, collectibles, safehouses, skills and stats.",
            dark: new("#7CC657", "#93D872", "#62A843", "#1D3016"),
            light: new("#2E7D32", "#3D9442", "#216226", "#E3F2DC")),
    ];

    public static GameProfile Get(GameId id) => All.First(g => g.Id == id);

    /// <summary>
    /// Points the palette's accent colours at this game. The palette's theme dictionaries are
    /// edited in place so every <c>DynamicResource</c> picks the change up immediately.
    /// </summary>
    public void ApplyAccent(Application? app)
    {
        if (app is null)
        {
            return;
        }

        foreach (var dictionary in EnumerateDictionaries(app.Resources))
        {
            if (dictionary.ThemeDictionaries.TryGetValue(ThemeVariant.Dark, out var dark) && dark is IResourceDictionary d)
            {
                Dark.WriteTo(d);
            }

            if (dictionary.ThemeDictionaries.TryGetValue(ThemeVariant.Light, out var light) && light is IResourceDictionary l)
            {
                Light.WriteTo(l);
            }
        }
    }

    private static IEnumerable<IResourceDictionary> EnumerateDictionaries(IResourceDictionary root)
    {
        yield return root;
        foreach (var merged in root.MergedDictionaries)
        {
            if (merged is IResourceDictionary child)
            {
                foreach (var nested in EnumerateDictionaries(child))
                {
                    yield return nested;
                }
            }
        }
    }

    private Uri AssetUri(string file) => new($"avares://GtaDeSaveEditor/Assets/Games/{_assetFolder}/{file}");

    private Bitmap LoadBitmap(string file)
    {
        using var stream = AssetLoader.Open(AssetUri(file));
        return new Bitmap(stream);
    }
}

/// <summary>The four accent shades the palette uses for one theme.</summary>
public sealed record AccentSet(string Accent, string Hover, string Pressed, string Soft)
{
    public void WriteTo(IResourceDictionary dictionary)
    {
        dictionary["AccentColor"] = Color.Parse(Accent);
        dictionary["AccentHoverColor"] = Color.Parse(Hover);
        dictionary["AccentPressedColor"] = Color.Parse(Pressed);
        dictionary["AccentSoftColor"] = Color.Parse(Soft);
    }
}
