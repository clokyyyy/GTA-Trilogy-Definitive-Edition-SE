using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GtaDe.App.Views;

public partial class WorldPage : UserControl
{
    public WorldPage() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
