using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GtaDe.App.Views;

public partial class VcPlayerPage : UserControl
{
    public VcPlayerPage() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
