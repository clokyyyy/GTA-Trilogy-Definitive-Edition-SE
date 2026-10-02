using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using GtaDe.App.Services;
using GtaDe.App.ViewModels;
using GtaDe.App.Views;
using Xunit;

namespace GtaDe.App.Tests;

/// <summary>
/// Renders each page to a PNG so the visual design can be reviewed without launching the app.
/// The files land in the test output folder's "Screenshots" directory.
/// </summary>
public class ScreenshotTests
{
    [AvaloniaFact]
    public void CaptureEveryPageInBothThemes()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Screenshots");
        Directory.CreateDirectory(folder);

        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1240, Height = 820 };
        window.Show();

        foreach (var (prefix, sample) in new[] { ("", "GTA3sf1.sav"), ("VC-", "GTAVCsf8.sav"), ("SA-", "GTASAsf1.sav") })
        {
        vm.LoadFile(Path.Combine(AppContext.BaseDirectory, "Samples", sample));

        foreach (var theme in new[] { ThemePreference.Dark, ThemePreference.Light })
        {
            vm.ApplyTheme(theme);

            // The default window size in both themes, plus a full-screen widescreen pass in dark.
            var sizes = theme == ThemePreference.Dark
                ? new[] { (1240, 820, ""), (1920, 1080, "Wide-") }
                : new[] { (1240, 820, "") };

            foreach (var (width, height, sizeTag) in sizes)
            {
            window.Width = width;
            window.Height = height;

            foreach (var page in vm.Pages)
            {
                vm.NavigateCommand.Execute(page);

                window.Measure(new Size(width, height));
                window.Arrange(new Rect(0, 0, width, height));
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();

                using var frame = window.CaptureRenderedFrame();
                var name = $"{prefix}{sizeTag}{theme}-{page.Title.Replace(" ", string.Empty).Replace("/", string.Empty)}.png";
                frame?.Save(Path.Combine(folder, name));

                // Long pages: also capture the bottom on the wide pass.
                var scroller = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(window)
                    .OfType<Avalonia.Controls.ScrollViewer>()
                    .FirstOrDefault(s => s.IsEffectivelyVisible && s.Extent.Height > s.Viewport.Height + 1);
                if (sizeTag.Length > 0 && scroller is not null)
                {
                    scroller.ScrollToEnd();
                    window.Measure(new Size(width, height));
                    window.Arrange(new Rect(0, 0, width, height));
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                    using var end = window.CaptureRenderedFrame();
                    end?.Save(Path.Combine(folder, name.Replace(".png", "-End.png")));
                    scroller.ScrollToHome();
                }
            }
            }

            window.Width = 1240;
            window.Height = 820;
        }
        }

        vm.SelectGameCommand.Execute(vm.Games[0]);
        window.Close();

        Assert.NotEmpty(Directory.GetFiles(folder, "*.png"));
    }

    [AvaloniaFact]
    public void CaptureEachGameLandingInBothThemes()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Screenshots");
        Directory.CreateDirectory(folder);

        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1240, Height = 820 };
        window.Show();

        try
        {
            foreach (var theme in new[] { ThemePreference.Dark, ThemePreference.Light })
            {
                vm.ApplyTheme(theme);

                foreach (var game in vm.Games)
                {
                    vm.SelectGameCommand.Execute(game);
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();

                    using var frame = window.CaptureRenderedFrame();
                    frame?.Save(Path.Combine(folder, $"{theme}-Landing-{game.Profile.Id}.png"));
                }
            }
        }
        finally
        {
            vm.SelectGameCommand.Execute(vm.Games[0]);
            vm.ApplyTheme(ThemePreference.Dark);
            window.Close();
        }
    }
}
