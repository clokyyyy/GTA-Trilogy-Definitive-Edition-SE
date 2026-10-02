using CommunityToolkit.Mvvm.ComponentModel;

namespace GtaDe.App.ViewModels;

/// <summary>
/// Base class for everything that appears in the navigation rail.
/// </summary>
/// <remarks>
/// Pages are built once and kept alive, so switching between them is instant and any scroll
/// position or expanded group the user left behind is still there when they come back. Each page
/// reads the save on <see cref="Refresh"/> rather than caching values in its constructor.
/// </remarks>
public abstract partial class PageViewModel(MainWindowViewModel shell) : ObservableObject
{
    /// <summary>True while this page is the one on screen, used to highlight the rail entry.</summary>
    [ObservableProperty]
    private bool _isSelected;

    protected MainWindowViewModel Shell { get; } = shell;

    /// <summary>Short label shown in the navigation rail.</summary>
    public abstract string Title { get; }

    /// <summary>One-line explanation shown under the page heading.</summary>
    public abstract string Description { get; }

    /// <summary>Path-data glyph shown in the navigation rail.</summary>
    public abstract string Icon { get; }

    /// <summary>Group heading the page sits under in the rail.</summary>
    public virtual string Group => "Save";

    /// <summary>Whether the page is usable right now.</summary>
    public virtual bool IsAvailable => Shell.HasSave;

    /// <summary>Re-evaluates <see cref="IsAvailable"/>, which depends on state the shell owns.</summary>
    public void NotifyAvailabilityChanged() => OnPropertyChanged(nameof(IsAvailable));

    /// <summary>Re-reads everything this page shows from the save buffer.</summary>
    public abstract void Refresh();

    /// <summary>
    /// Applies a change and tells the shell the document is dirty. Pages route every write through
    /// this so no edit can bypass the modified tracking.
    /// </summary>
    protected void Edit(Action change)
    {
        if (!Shell.HasSave || Shell.IsRefreshing)
        {
            return;
        }

        change();
        Shell.NotifyEdited();
    }
}
