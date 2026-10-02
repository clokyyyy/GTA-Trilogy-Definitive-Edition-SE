using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Logging;
using Avalonia.Threading;
using GtaDe.App;
using GtaDe.App.ViewModels;
using GtaDe.App.Views;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(GtaDe.App.Tests.TestAppBuilder))]

namespace GtaDe.App.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .UseSkia()
        .WithInterFont();
}

/// <summary>
/// Collects everything Avalonia logs at warning level or above. A failed binding is only a warning
/// at runtime, so without this a typo in a view would show up as a silently blank control rather
/// than a test failure.
/// </summary>
internal sealed class CollectingLogSink : ILogSink
{
    public List<string> Messages { get; } = [];

    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
        Record(level, area, messageTemplate, []);

    public void Log<T0>(LogEventLevel level, string area, object? source, string messageTemplate, T0 v0) =>
        Record(level, area, messageTemplate, [v0]);

    public void Log<T0, T1>(LogEventLevel level, string area, object? source, string messageTemplate, T0 v0, T1 v1) =>
        Record(level, area, messageTemplate, [v0, v1]);

    public void Log<T0, T1, T2>(LogEventLevel level, string area, object? source, string messageTemplate, T0 v0, T1 v1, T2 v2) =>
        Record(level, area, messageTemplate, [v0, v1, v2]);

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] values) =>
        Record(level, area, messageTemplate, values);

    private void Record(LogEventLevel level, string area, string template, object?[] values)
    {
        if (!IsEnabled(level, area))
        {
            return;
        }

        var message = template;
        foreach (var value in values)
        {
            var brace = message.IndexOf('{');
            var close = brace < 0 ? -1 : message.IndexOf('}', brace);
            if (close < 0)
            {
                break;
            }

            message = message[..brace] + value + message[(close + 1)..];
        }

        Messages.Add($"[{area}] {message}");
    }
}

public class UiSmokeTests
{
    private static string SamplePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Samples", name);

    private static (Window Window, MainWindowViewModel Vm, CollectingLogSink Sink) ShowWithSave(string sample)
    {
        var sink = new CollectingLogSink();
        Logger.Sink = sink;

        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        vm.LoadFile(SamplePath(sample));

        return (window, vm, sink);
    }

    [AvaloniaTheory]
    [InlineData("GTA3sf1.sav")]
    [InlineData("GTA3sf2.sav")]
    [InlineData("GTA3sf9.sav")]
    public void EveryPageRendersWithoutBindingErrors(string sample)
    {
        var (window, vm, sink) = ShowWithSave(sample);

        foreach (var page in vm.Pages)
        {
            vm.NavigateCommand.Execute(page);
            Dispatcher.UIThread.RunJobs();
        }

        window.Close();

        var failures = sink.Messages
            .Where(m => m.Contains("Binding", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [AvaloniaFact]
    public void SwitchingGamesRebrandsTheShellWithoutBindingErrors()
    {
        var sink = new CollectingLogSink();
        Logger.Sink = sink;

        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        try
        {
            var gta3Accent = ResolveAccent(window);

            foreach (var game in vm.Games)
            {
                vm.SelectGameCommand.Execute(game);
                Dispatcher.UIThread.RunJobs();

                Assert.Same(game.Profile, vm.SelectedGame);
                Assert.True(game.IsSelected);
                Assert.Single(vm.Games, g => g.IsSelected);
                Assert.NotNull(game.Profile.Banner);
                Assert.NotNull(game.Profile.Logo);
                Assert.NotNull(window.Icon);
                Assert.Equal(!game.Profile.IsSupported, vm.ShowComingSoon);
                Assert.Equal(game.Profile.IsSupported, vm.OpenCommand.CanExecute(null));
            }

            Assert.NotEqual(gta3Accent, ResolveAccent(window));

            // Opening a GTA III save brings its profile back regardless of what was selected.
            vm.LoadFile(SamplePath("GTA3sf1.sav"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(GtaDe.App.Services.GameId.Gta3, vm.SelectedGame.Id);
            Assert.True(vm.ShowEditor);
            Assert.Equal(gta3Accent, ResolveAccent(window));
        }
        finally
        {
            vm.SelectGameCommand.Execute(vm.Games[0]);
            window.Close();
        }

        var failures = sink.Messages
            .Where(m => m.Contains("Binding", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static object? ResolveAccent(Window window) =>
        window.TryFindResource("AccentColor", window.ActualThemeVariant, out var value) ? value : null;

    [AvaloniaFact]
    public void EveryViceCityPageRendersWithoutBindingErrors()
    {
        var (window, vm, sink) = ShowWithSave("GTAVCsf8.sav");

        try
        {
            Assert.Equal(GtaDe.App.Services.GameId.ViceCity, vm.SelectedGame.Id);
            Assert.True(vm.ShowEditor);
            Assert.Contains(vm.Pages, p => p is VcPlayerPageViewModel);

            foreach (var page in vm.Pages)
            {
                vm.NavigateCommand.Execute(page);
                Dispatcher.UIThread.RunJobs();
            }

            var dashboard = vm.Pages.OfType<VcDashboardPageViewModel>().Single();
            Assert.All(dashboard.Tiles, tile => Assert.NotEqual("—", tile.Value));

            var missions = vm.Pages.OfType<VcMissionsPageViewModel>().Single();
            Assert.NotEmpty(missions.Groups);

            var activities = vm.Pages.OfType<VcActivitiesPageViewModel>().Single();
            foreach (var tab in activities.Tabs)
            {
                activities.SelectedTab = tab;
                Dispatcher.UIThread.RunJobs();
            }
        }
        finally
        {
            vm.SelectGameCommand.Execute(vm.Games[0]);
            window.Close();
        }

        var failures = sink.Messages
            .Where(m => m.Contains("Binding", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [AvaloniaFact]
    public void ViceCityEditsMarkTheSaveModified()
    {
        var (window, vm, _) = ShowWithSave("GTAVCsf8.sav");

        try
        {
            var player = vm.Pages.OfType<VcPlayerPageViewModel>().Single();
            player.Money = 1_234_567;
            Assert.True(vm.IsModified);
            Assert.Equal(1_234_567, vm.ViceCity!.PlayerInfo.Money);

            var python = player.Weapons[3];
            python.Selected = python.Options[0];
            Assert.Equal(GtaDe.SaveFormat.ViceCityWeapon.Unarmed, vm.ViceCity.PlayerPed.Weapons[3].Type);

            var activities = vm.Pages.OfType<VcActivitiesPageViewModel>().Single();
            activities.SelectedTab = activities.Tabs[0];
            activities.ClearSelectedCommand.Execute(null);
            Assert.Equal(0, vm.ViceCity.Globals.GetInt("TOTAL_RAMPAGES_PASSED"));
            Assert.All(activities.Tabs[0].Items, i => Assert.False(i.IsComplete));

            vm.RevertCommand.Execute(null);
            Assert.False(vm.IsModified);
            Assert.Equal(719678, player.Money);
        }
        finally
        {
            vm.SelectGameCommand.Execute(vm.Games[0]);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EverySanAndreasPageRendersWithoutBindingErrors()
    {
        var (window, vm, sink) = ShowWithSave("GTASAsf1.sav");

        try
        {
            Assert.Equal(GtaDe.App.Services.GameId.SanAndreas, vm.SelectedGame.Id);
            Assert.True(vm.ShowEditor);
            Assert.Contains(vm.Pages, p => p is SaPlayerPageViewModel);

            foreach (var page in vm.Pages)
            {
                vm.NavigateCommand.Execute(page);
                Dispatcher.UIThread.RunJobs();
            }

            var dashboard = vm.Pages.OfType<SaDashboardPageViewModel>().Single();
            Assert.All(dashboard.Tiles, tile => Assert.NotEqual("—", tile.Value));

            var missions = vm.Pages.OfType<SaMissionsPageViewModel>().Single();
            Assert.NotEmpty(missions.Groups);
            Assert.All(missions.Groups.SelectMany(g => g.Items), i => Assert.True(i.IsComplete, i.Name));

            foreach (var tabs in vm.Pages.OfType<SaFlagTabsPageViewModel>())
            {
                foreach (var tab in tabs.Tabs)
                {
                    tabs.SelectedTab = tab;
                    Dispatcher.UIThread.RunJobs();
                }
            }
        }
        finally
        {
            vm.SelectGameCommand.Execute(vm.Games[0]);
            window.Close();
        }

        var failures = sink.Messages
            .Where(m => m.Contains("Binding", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [AvaloniaFact]
    public void SanAndreasEditsMarkTheSaveModified()
    {
        var (window, vm, _) = ShowWithSave("GTASAsf1.sav");

        try
        {
            var player = vm.Pages.OfType<SaPlayerPageViewModel>().Single();
            var originalMoney = player.Money;
            player.Money = 1_234_567;
            Assert.True(vm.IsModified);
            Assert.Equal(1_234_567, vm.SanAndreas!.PlayerInfo.Money);

            var missions = vm.Pages.OfType<SaMissionsPageViewModel>().Single();
            var sweet = missions.Groups.Single(g => g.Name == "Sweet");
            sweet.Items[2].IsComplete = false;
            Assert.False(sweet.Items[^1].IsComplete);
            Assert.True(sweet.Items[1].IsComplete);
            Assert.Equal(2, vm.SanAndreas.Globals.GetInt("FLAG_SWEET_MISSION_COUNTER"));

            var houses = vm.Pages.OfType<SaPropertiesPageViewModel>().Single();
            houses.SelectedTab = houses.Tabs[0];
            houses.ClearSelectedCommand.Execute(null);
            Assert.All(houses.Tabs[0].Items, i => Assert.False(i.IsComplete));

            var collectibles = vm.Pages.OfType<SaCollectiblesPageViewModel>().Single();
            collectibles.Groups[0].Items[0].Value = 7;
            Assert.Equal(7, vm.SaProgression!.TagsSprayed);

            vm.RevertCommand.Execute(null);
            Assert.False(vm.IsModified);
            Assert.Equal(originalMoney, player.Money);
            Assert.True(sweet.Items[^1].IsComplete);
        }
        finally
        {
            vm.SelectGameCommand.Execute(vm.Games[0]);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ScriptGlobalsFollowTheSaveAcrossGames()
    {
        var (window, vm, sink) = ShowWithSave("GTA3sf1.sav");

        try
        {
            var globals = vm.Pages.OfType<GlobalsPageViewModel>().Single();
            vm.NavigateCommand.Execute(globals);
            Dispatcher.UIThread.RunJobs();
            Assert.NotEmpty(globals.Rows);

            foreach (var sample in new[] { "GTAVCsf8.sav", "GTASAsf1.sav", "GTA3sf1.sav", "GTASAsf1.sav" })
            {
                vm.LoadFile(SamplePath(sample));
                vm.NavigateCommand.Execute(globals);
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(GlobalsPageViewModel.AllScopes, globals.SelectedScope);
                Assert.NotEmpty(globals.Rows);
                Assert.Equal(vm.Document!.Symbols.Scopes.Count() + 1, globals.Scopes.Count);
                Assert.All(globals.Rows, r => Assert.Contains(r.Global, vm.Document.Symbols.Globals));
            }
        }
        finally
        {
            vm.SelectGameCommand.Execute(vm.Games[0]);
            window.Close();
        }

        Assert.DoesNotContain(sink.Messages, m => m.Contains("Binding", StringComparison.OrdinalIgnoreCase));
    }

    [AvaloniaFact]
    public void TheLastSaveOfEachGameIsReopened()
    {
        var vm = new MainWindowViewModel();
        try
        {
            vm.Settings.ReopenLastSave = true;
            vm.LoadFile(SamplePath("GTAVCsf8.sav"));
            vm.LoadFile(SamplePath("GTASAsf1.sav"));

            // Switching back to Vice City brings its save back without a file picker.
            vm.SelectGameCommand.Execute(vm.Games.Single(g => g.Profile.Id == Services.GameId.ViceCity));
            Assert.NotNull(vm.ViceCity);
            Assert.True(vm.ShowEditor);
            Assert.StartsWith("Reopened", vm.StatusMessage);

            // A fresh start opens the last game's last save.
            var restarted = new MainWindowViewModel();
            Assert.Equal(Services.GameId.ViceCity, restarted.SelectedGame.Id);
            Assert.NotNull(restarted.ViceCity);
            Assert.True(restarted.ShowEditor);

            restarted.SelectGameCommand.Execute(restarted.Games.Single(g => g.Profile.Id == Services.GameId.SanAndreas));
            Assert.NotNull(restarted.SanAndreas);
        }
        finally
        {
            vm.Settings.ReopenLastSave = false;
            vm.Settings.LastFiles.Clear();
            vm.SelectGameCommand.Execute(vm.Games[0]);
        }
    }

    [AvaloniaFact]
    public void CardsLineUpInRowsWithLongListsAtTheBottom()
    {
        var (window, vm, _) = ShowWithSave("GTASAsf1.sav");

        try
        {
            window.Width = 1920;
            window.Height = 1080;
            vm.NavigateCommand.Execute(vm.Pages.OfType<SaStatsPageViewModel>().Single());
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var panel = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(window)
                .OfType<GtaDe.App.Controls.CardGrid>()
                .First(p => p.IsEffectivelyVisible && p.Children.Count > 2);
            var cards = panel.Children.Where(c => c.DataContext is NumberGroup { IsFullRow: false }).ToList();
            var wide = panel.Children.Where(c => c.DataContext is NumberGroup { IsFullRow: true }).ToList();

            Assert.Equal(2, cards.Select(c => Math.Round(c.Bounds.X)).Distinct().Count());
            Assert.All(cards.Chunk(2), row => Assert.Single(row.Select(c => Math.Round(c.Bounds.Y)).Distinct()));
            Assert.All(cards.Chunk(2), row => Assert.Single(row.Select(c => Math.Round(c.Bounds.Height)).Distinct()));
            Assert.NotEmpty(wide);
            Assert.All(wide, w =>
            {
                Assert.Equal(panel.Bounds.Width, w.Bounds.Width, 0.5);
                Assert.True(w.Bounds.Y > cards.Max(c => c.Bounds.Bottom));
            });

            window.Width = 900;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.Single(cards.Select(c => Math.Round(c.Bounds.X)).Distinct());
        }
        finally
        {
            vm.SelectGameCommand.Execute(vm.Games[0]);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LoadingASaveFillsTheDashboard()
    {
        var (window, vm, _) = ShowWithSave("GTA3sf1.sav");

        var dashboard = vm.Pages.OfType<DashboardPageViewModel>().Single();

        Assert.True(vm.HasSave);
        Assert.NotEqual("—", dashboard.Money.Value);
        Assert.NotEqual("—", dashboard.LastMission);
        Assert.All(dashboard.Tiles, tile => Assert.NotEqual("—", tile.Value));

        window.Close();
    }

    [AvaloniaFact]
    public void EditingMoneyMarksTheSaveModifiedAndRipplesToTheDashboard()
    {
        var (window, vm, _) = ShowWithSave("GTA3sf1.sav");

        var player = vm.Pages.OfType<PlayerPageViewModel>().Single();
        var dashboard = vm.Pages.OfType<DashboardPageViewModel>().Single();

        player.Money = 1_234_567;

        Assert.True(vm.IsModified);
        Assert.Equal(1_234_567, vm.Save!.PlayerInfo.Money);
        Assert.Contains("1,234,567", dashboard.Money.Value);

        vm.RevertCommand.Execute(null);

        Assert.False(vm.IsModified);
        Assert.NotEqual(1_234_567, vm.Save!.PlayerInfo.Money);

        window.Close();
    }

    [AvaloniaFact]
    public void BothThemesResolveEveryBrushTheViewsAskFor()
    {
        var (window, vm, sink) = ShowWithSave("GTA3sf1.sav");

        foreach (var theme in new[] { Services.ThemePreference.Light, Services.ThemePreference.Dark })
        {
            vm.ApplyTheme(theme);

            foreach (var page in vm.Pages)
            {
                vm.NavigateCommand.Execute(page);
                Dispatcher.UIThread.RunJobs();
            }
        }

        window.Close();

        var missing = sink.Messages
            .Where(m => m.Contains("resource", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
    }

    [AvaloniaFact]
    public void TheResearchPageNamesTheVariablesThatDifferBetweenTwoSaves()
    {
        var (window, vm, _) = ShowWithSave("GTA3sf2.sav");

        var research = vm.Pages.OfType<ResearchPageViewModel>().Single();
        research.CompareWith(SamplePath("GTA3sf1.sav"));

        var details = research.Differences.Select(d => d.Detail).ToList();

        Assert.NotEmpty(research.Differences);
        Assert.Contains(details, d => d.Contains("RAMPAGE_", StringComparison.Ordinal));
        Assert.Contains(details, d => d.Contains("FLAG_USJ", StringComparison.Ordinal));
        Assert.Contains("GTA3sf1.sav", research.ComparisonSummary);

        research.InspectOffset = "0x" + (vm.Save!.Globals.BaseOffset).ToString("X");
        research.InspectCommand.Execute(null);

        Assert.Contains("ONE_SIXTEENTH", research.InspectResult);
        Assert.Contains("0.0625", research.InspectResult);

        window.Close();
    }
}

