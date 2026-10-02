using System.Runtime.CompilerServices;

namespace GtaDe.App.Tests;

internal static class TestEnvironment
{
    /// <summary>
    /// Redirects persisted settings into the test output folder so running the suite never
    /// touches (or pollutes the recent-files list of) the real installation.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "TestAppData");
        Directory.CreateDirectory(folder);
        Environment.SetEnvironmentVariable("GTADE_SAVE_EDITOR_DATA", folder);

        // Start every run from known settings; reopening saves is switched on only by the test for it.
        File.WriteAllText(Path.Combine(folder, "settings.json"), """{ "ReopenLastSave": false }""");
    }
}
