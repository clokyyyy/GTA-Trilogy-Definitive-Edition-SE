using System.Diagnostics;

namespace GtaDe.App.Services;

/// <summary>
/// Locates the folder the Definitive Edition keeps its saves in, and notices when the game is
/// running.
/// </summary>
/// <remarks>
/// The game rewrites save files while it is running, so anything the editor writes during a
/// session can be overwritten without warning. The editor checks for a running game before saving
/// rather than letting the user lose work silently.
/// </remarks>
public static class GameLocator
{
    private static readonly string[] ProcessNames =
    [
        "LibertyCity",
        "Gameface",
        "GTA3_DE",
        "ViceCity",
        "SanAndreas",
    ];

    /// <summary>
    /// The save folder, which is Documents\Rockstar Games\GTA III Definitive Edition\Profiles\&lt;id&gt;.
    /// The profile id is per Social Club account, so the folder is found by looking for the one
    /// that actually contains saves rather than by guessing the name.
    /// </summary>
    public static string? FindSaveFolder(
        string gameFolder = "GTA III Definitive Edition",
        string savePattern = "GTA3sf*.sav")
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrEmpty(documents))
        {
            return null;
        }

        var root = Path.Combine(documents, "Rockstar Games", gameFolder);
        if (!Directory.Exists(root))
        {
            return null;
        }

        var profiles = Path.Combine(root, "Profiles");
        if (Directory.Exists(profiles))
        {
            var withSaves = Directory.EnumerateDirectories(profiles)
                .Where(d => Directory.EnumerateFiles(d, savePattern).Any())
                .OrderByDescending(Directory.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (withSaves is not null)
            {
                return withSaves;
            }

            // No saves yet, but the folder is still the right place to start the file picker.
            var anyProfile = Directory.EnumerateDirectories(profiles).FirstOrDefault();
            if (anyProfile is not null)
            {
                return anyProfile;
            }

            return profiles;
        }

        return root;
    }

    /// <summary>True when a Definitive Edition process looks to be running right now.</summary>
    public static bool IsGameRunning()
    {
        foreach (var name in ProcessNames)
        {
            try
            {
                if (Process.GetProcessesByName(name).Length > 0)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // Process enumeration can fail under tight permissions; treat that as "not running"
                // rather than blocking the user from saving.
            }
        }

        return false;
    }
}
