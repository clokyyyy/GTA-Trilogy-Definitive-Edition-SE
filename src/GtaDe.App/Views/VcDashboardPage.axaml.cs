using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GtaDe.App.Views;

public partial class VcDashboardPage : UserControl
{
    public VcDashboardPage() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
