using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GtaDe.App.ViewModels;
using GtaDe.App.Views;
using Xunit;
using Xunit.Abstractions;

namespace GtaDe.App.Tests;

/// <summary>
/// Guards against content that can never be scrolled into view. Every scrollable region on every
/// page is scrolled to its end, and the bottom edge of its content must then sit inside the
/// visible viewport.
/// </summary>
public class LayoutTests(ITestOutputHelper output)
{
    public static TheoryData<int, int> WindowSizes => new()
    {
        { 1240, 820 },
        { 1121, 768 },
        { 980, 640 },
    };

    [AvaloniaTheory]
    [MemberData(nameof(WindowSizes))]
    public void EveryScrollableRegionCanRevealAllOfItsContent(int width, int height)
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = width, Height = height };
        window.Show();
        vm.LoadFile(Path.Combine(AppContext.BaseDirectory, "Samples", "GTA3sf1.sav"));

        var failures = new List<string>();

        foreach (var page in vm.Pages)
        {
            vm.NavigateCommand.Execute(page);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            foreach (var scroller in window.GetVisualDescendants().OfType<ScrollViewer>()
                         .Where(s => s.IsEffectivelyVisible && s.Content is Control))
            {
                for (var i = 0; i < 20; i++)
                {
                    scroller.Offset = new Vector(0, double.MaxValue);
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                }

                var content = (Control)scroller.Content!;
                if (content is Avalonia.Controls.Presenters.ItemsPresenter)
                {
                    // Virtualised lists only realise visible rows; just check the end is reachable.
                    if (scroller.Offset.Y + scroller.Viewport.Height < scroller.Extent.Height - 1.5)
                    {
                        failures.Add($"{page.Title}: list cannot scroll to its end");
                    }
                    continue;
                }

                var bottom = content.TranslatePoint(new Point(0, content.Bounds.Height), scroller);
                var right = content.TranslatePoint(new Point(content.Bounds.Width, 0), scroller);
                if (right is { } r && r.X - scroller.Bounds.Width > 1.5)
                {
                    failures.Add($"{page.Title}: {r.X - scroller.Bounds.Width:F0}px cut off on the right");
                }
                if (bottom is null)
                {
                    continue;
                }

                // Allow a hair of rounding; anything more is content the user cannot reach.
                var hidden = bottom.Value.Y - scroller.Bounds.Height;
                if (hidden > 1.5)
                {
                    failures.Add($"{page.Title}: {hidden:F0}px hidden below the viewport " +
                                 $"(viewport {scroller.Bounds.Height:F0}, extent {scroller.Extent.Height:F0})");
                }

                // The scroller itself must not extend underneath the window edge or status bar.
                var scrollerBottom = scroller.TranslatePoint(new Point(0, scroller.Bounds.Height), window);
                if (scrollerBottom is { } sb && sb.Y > window.ClientSize.Height + 1.5)
                {
                    failures.Add($"{page.Title}: scroller extends {sb.Y - window.ClientSize.Height:F0}px past the window");
                }
            }
        }

        window.Close();

        foreach (var failure in failures)
        {
            output.WriteLine(failure);
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
