using Avalonia;
using Avalonia.Controls;

namespace GtaDe.App.Controls;

/// <summary>Implemented by item view models that should take a whole row of a <see cref="CardGrid"/>.</summary>
public interface IFullRowCard
{
    bool IsFullRow { get; }
}

/// <summary>
/// Lays cards out in tidy rows of equal columns: one column on a narrow window, more as the window
/// widens (up to <see cref="MaxColumns"/>). Cards in a row share the row's height, so their edges
/// line up. Full-row cards (long lists) span the width and come after the regular cards.
/// </summary>
public sealed class CardGrid : Panel
{
    public static readonly StyledProperty<double> MinColumnWidthProperty =
        AvaloniaProperty.Register<CardGrid, double>(nameof(MinColumnWidth), 420);

    public static readonly StyledProperty<int> MaxColumnsProperty =
        AvaloniaProperty.Register<CardGrid, int>(nameof(MaxColumns), 2);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<CardGrid, double>(nameof(Spacing), 20);

    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<CardGrid, double>(nameof(RowSpacing), double.NaN);

    /// <summary>Uses the fewest columns that keep the same number of rows, so the last row is not left nearly empty.</summary>
    public static readonly StyledProperty<bool> BalancedProperty =
        AvaloniaProperty.Register<CardGrid, bool>(nameof(Balanced));

    public bool Balanced
    {
        get => GetValue(BalancedProperty);
        set => SetValue(BalancedProperty, value);
    }

    public static readonly AttachedProperty<bool> FullRowProperty =
        AvaloniaProperty.RegisterAttached<CardGrid, Control, bool>("FullRow");

    static CardGrid()
    {
        AffectsMeasure<CardGrid>(MinColumnWidthProperty, MaxColumnsProperty, SpacingProperty, RowSpacingProperty, BalancedProperty);
        AffectsParentMeasure<CardGrid>(FullRowProperty);
    }

    public double MinColumnWidth
    {
        get => GetValue(MinColumnWidthProperty);
        set => SetValue(MinColumnWidthProperty, value);
    }

    public int MaxColumns
    {
        get => GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    /// <summary>Gap between columns (and between rows unless <see cref="RowSpacing"/> is set).</summary>
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    public static bool GetFullRow(Control control) => control.GetValue(FullRowProperty);

    public static void SetFullRow(Control control, bool value) => control.SetValue(FullRowProperty, value);

    private double VerticalGap => double.IsNaN(RowSpacing) ? Spacing : RowSpacing;

    private static bool IsFullRow(Control child) =>
        GetFullRow(child) || child.DataContext is IFullRowCard { IsFullRow: true };

    /// <summary>Visible children in display order: regular cards first, full-row cards last.</summary>
    private List<Control> Ordered()
    {
        var visible = Children.Where(c => c.IsVisible).ToList();
        return [.. visible.Where(c => !IsFullRow(c)), .. visible.Where(IsFullRow)];
    }

    private int ColumnCount(double width, int items)
    {
        if (items == 0 || double.IsInfinity(width))
        {
            return 1;
        }

        var fit = (int)Math.Floor((width + Spacing) / (MinColumnWidth + Spacing));
        var columns = Math.Clamp(fit, 1, Math.Max(1, Math.Min(MaxColumns, items)));
        if (Balanced)
        {
            var rows = (items + columns - 1) / columns;
            columns = (items + rows - 1) / rows;
        }

        return columns;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var hidden in Children.Where(c => !c.IsVisible))
        {
            hidden.Measure(availableSize);
        }

        var children = Ordered();
        if (double.IsInfinity(availableSize.Width))
        {
            double width = 0, height = 0;
            foreach (var child in children)
            {
                child.Measure(availableSize);
                width = Math.Max(width, child.DesiredSize.Width);
                height += child.DesiredSize.Height;
            }

            return new Size(width, height + (VerticalGap * Math.Max(0, children.Count - 1)));
        }

        return new Size(availableSize.Width, Layout(children, availableSize.Width, measure: true, arrange: false));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Layout(Ordered(), finalSize.Width, measure: false, arrange: true);
        return finalSize;
    }

    /// <summary>Breaks the cards into rows, optionally measuring and arranging them; returns the total height.</summary>
    private double Layout(List<Control> children, double width, bool measure, bool arrange)
    {
        var regular = children.Count(c => !IsFullRow(c));
        var columns = ColumnCount(width, regular);
        var columnWidth = Math.Max(0, (width - (Spacing * (columns - 1))) / columns);

        var rows = new List<List<Control>>();
        foreach (var child in children)
        {
            var full = IsFullRow(child);
            if (measure)
            {
                child.Measure(new Size(full ? width : columnWidth, double.PositiveInfinity));
            }

            if (full || rows.Count == 0 || rows[^1].Count == columns || IsFullRow(rows[^1][0]))
            {
                rows.Add([]);
            }

            rows[^1].Add(child);
        }

        double top = 0;
        foreach (var row in rows)
        {
            var height = row.Max(c => c.DesiredSize.Height);
            if (arrange)
            {
                for (var i = 0; i < row.Count; i++)
                {
                    var full = IsFullRow(row[i]);
                    row[i].Arrange(new Rect(
                        full ? 0 : i * (columnWidth + Spacing), top, full ? width : columnWidth, height));
                }
            }

            top += height + VerticalGap;
        }

        return Math.Max(0, top - VerticalGap);
    }
}
