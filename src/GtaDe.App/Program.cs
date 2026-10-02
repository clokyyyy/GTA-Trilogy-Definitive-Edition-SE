using Avalonia;

namespace GtaDe.App;

internal static class Program
{
    private const string InstanceMutexName = @"Local\GtaDeSaveEditor.SingleInstance";

    [STAThread]
    public static void Main(string[] args)
    {
        // Only one editor per user session: a second launch exits silently.
        using var mutex = new Mutex(initiallyOwned: true, InstanceMutexName, out bool createdNew);
        if (!createdNew)
            return;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
