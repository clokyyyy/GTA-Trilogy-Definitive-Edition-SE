using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GtaDe.App.ViewModels;
using GtaDe.App.Views;

namespace GtaDe.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel();
            var window = new MainWindow { DataContext = viewModel };

            viewModel.StorageProvider = window.StorageProvider;
            viewModel.ConfirmAsync = (title, message) => ConfirmDialog.ShowAsync(window, title, message);

            // A path can be passed on the command line, which is what makes "open with" work.
            var path = desktop.Args?.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                viewModel.LoadFile(path);
            }

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
