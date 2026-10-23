using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Logging;
using Avalonia.Threading;
using GtaDe.App.ViewModels;
using GtaDe.App.Views;
using GtaDe.SaveFormat;
using Xunit;

namespace GtaDe.App.Tests;

public class CompatibilityUiTests
{
    private static string SamplePath(string name) => Path.Combine(AppContext.BaseDirectory, "Samples", name);

    [AvaloniaTheory]
    [InlineData("GTA3sf1.sav", true)]
    [InlineData("GTAVCsf8.sav", true)]
    [InlineData("GTASAsf1.sav", true)]
    [InlineData("GTASA-revision41.sav", true)]
    [InlineData("GTASA-revision41.sav", false)]
    public void CompatibleRevisionsOpenAndEveryPageRenders(string sample, bool useUnknownRevision)
    {
        var path = Path.Combine(Path.GetTempPath(), $"gtade-compatible-{Guid.NewGuid():N}.sav");
        var bytes = File.ReadAllBytes(SamplePath(sample));
        var original = DeSaveFile.Parse(bytes);
        if (useUnknownRevision)
        {
            ByteOps.WriteUInt32(bytes, SaveHeader.VersionOffset, 1001);
            ByteOps.WriteUInt32(bytes, SaveHeader.VersionRepeatOffset, 1001);
            if (original.Game == GameKind.SanAndreas)
            {
                ByteOps.WriteUInt32(bytes, original.Layout.Header.Length, 1001);
                ByteOps.WriteUInt32(bytes, original.Layout.Header.Length + 4, 1001);
            }

            SaveSeal.Apply(bytes, original.SealVariant);
        }

        File.WriteAllBytes(path, bytes);
        var sink = new CollectingLogSink();
        Logger.Sink = sink;
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();
        try
        {
            vm.LoadFile(path);
            Assert.True(vm.HasSave, vm.StatusMessage);
            Assert.Equal(original.Game, vm.Document!.Game);
            Assert.True(vm.SaveCommand.CanExecute(null));
            Assert.Equal(useUnknownRevision, vm.Document.IsUntestedRevision);
            if (useUnknownRevision)
            {
                Assert.Contains("Untested revision 1001", vm.StatusMessage);
            }

            foreach (var page in vm.Pages)
            {
                vm.NavigateCommand.Execute(page);
                Dispatcher.UIThread.RunJobs();
            }

            Assert.NotEmpty(vm.Pages.OfType<GlobalsPageViewModel>().Single().Rows);
            if (vm.SanAndreas is { } sa)
            {
                var player = vm.Pages.OfType<SaPlayerPageViewModel>().Single();
                player.Money = 42;
                Assert.True(vm.IsModified);
                Assert.Equal(42, sa.PlayerInfo.Money);
                vm.RevertCommand.Execute(null);
                Assert.False(vm.IsModified);
            }

            Assert.DoesNotContain(sink.Messages, m => m.Contains("Binding", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void SwitchingRevisionsAndGamesRefreshesGlobalsAndRejectsAnUnsafeLoad()
    {
        var sink = new CollectingLogSink();
        Logger.Sink = sink;
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();
        var badPath = Path.Combine(Path.GetTempPath(), $"gtade-invalid-{Guid.NewGuid():N}.sav");
        try
        {
            foreach (var sample in new[] { "GTASAsf1.sav", "GTASA-revision41.sav", "GTAVCsf8.sav", "GTA3sf1.sav", "GTASA-revision41.sav" })
            {
                vm.LoadFile(SamplePath(sample));
                Assert.True(vm.HasSave, vm.StatusMessage);
                var globals = vm.Pages.OfType<GlobalsPageViewModel>().Single();
                vm.NavigateCommand.Execute(globals);
                Dispatcher.UIThread.RunJobs();
                Assert.NotEmpty(globals.Rows);
                Assert.All(globals.Rows, row => Assert.Contains(row.Global, vm.Document!.Symbols.Globals));
            }

            var previous = vm.Document!;
            var bytes = previous.ToBytes();
            ByteOps.WriteUInt32(bytes, SaveHeader.VersionOffset, 1001);
            ByteOps.WriteUInt32(bytes, SaveHeader.VersionRepeatOffset, 1001);
            ByteOps.WriteUInt32(bytes, previous.Layout.Header.Length, 1001);
            ByteOps.WriteUInt32(bytes, previous.Layout.Header.Length + 4, 1001);
            bytes[previous.Layout[SaveBlockKind.Stats].Offset + 4] = (byte)'X';
            SaveSeal.Apply(bytes, previous.SealVariant);
            File.WriteAllBytes(badPath, bytes);
            vm.LoadFile(badPath);
            Assert.StartsWith("Could not open", vm.StatusMessage);
            Assert.Same(previous, vm.Document);

            var emptyVm = new MainWindowViewModel();
            emptyVm.LoadFile(badPath);
            Assert.False(emptyVm.HasSave);
            Assert.False(emptyVm.SaveCommand.CanExecute(null));
            Assert.False(emptyVm.SaveAsCommand.CanExecute(null));
            Assert.DoesNotContain(sink.Messages, m => m.Contains("Binding", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            window.Close();
            File.Delete(badPath);
        }
    }
}
