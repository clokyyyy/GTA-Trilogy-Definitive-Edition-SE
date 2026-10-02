using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GtaDe.App.Views;

public partial class VcStatsPage : UserControl
{
    public VcStatsPage() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
