using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace GtaDe.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // The game artwork is drawn well below its native size; render options flow down the
        // visual tree, so setting this once keeps every logo, icon and banner smooth.
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
