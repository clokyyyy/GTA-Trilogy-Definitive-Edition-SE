using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GtaDe.App.Views;

public partial class StatsPage : UserControl
{
    public StatsPage() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
