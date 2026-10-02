using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GtaDe.App.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();

        this.FindControl<Button>("CancelButton")!.Click += (_, _) => Close(false);
        this.FindControl<Button>("ConfirmButton")!.Click += (_, _) => Close(true);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public static async Task<bool> ShowAsync(Window owner, string title, string message)
    {
        var dialog = new ConfirmDialog { Title = title };
        dialog.FindControl<TextBlock>("TitleText")!.Text = title;
        dialog.FindControl<TextBlock>("MessageText")!.Text = message;

        return await dialog.ShowDialog<bool>(owner);
    }
}
