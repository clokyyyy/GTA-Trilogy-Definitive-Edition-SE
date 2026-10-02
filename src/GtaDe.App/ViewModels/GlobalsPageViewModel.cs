using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GtaDe.SaveFormat;

namespace GtaDe.App.ViewModels;

/// <summary>A single script global shown in the advanced editor.</summary>
public sealed partial class GlobalRow : ObservableObject
{
    [ObservableProperty]
    private string _value = string.Empty;

    public GlobalRow(ScriptGlobal global, int element, string value)
    {
        Global = global;
        Element = element;
        Value = value;
    }

    public ScriptGlobal Global { get; }

    public int Element { get; }

    public string Name => Global.ArrayCount > 1 ? $"{Global.Name}[{Element}]" : Global.Name;

    public string Scope => Global.Scope;

    public string Type => Global.Type.ToString();

    public int Offset => Global.Index + (Element * 4);
}

/// <summary>
/// Direct access to all 4,385 script globals, for anything the curated pages do not cover.
/// </summary>
public sealed partial class GlobalsPageViewModel(MainWindowViewModel shell) : PageViewModel(shell)
{
    private const int MaxRows = 400;

    public override string Title => "Script globals";

    public override string Description =>
        "Every variable the mission script keeps. There are no guard rails here: changing the wrong one can leave a save the game cannot finish.";

    public override string Icon => "M9 3H5a2 2 0 0 0-2 2v4h2V5h4V3Zm6 0v2h4v4h2V5a2 2 0 0 0-2-2h-4ZM5 15H3v4a2 2 0 0 0 2 2h4v-2H5v-4Zm16 0h-2v4h-4v2h4a2 2 0 0 0 2-2v-4Z";

    public override string Group => "Advanced";

    public override bool IsAvailable => Shell.HasSave && Shell.Settings.ShowAdvancedTools;

    public ObservableCollection<GlobalRow> Rows { get; } = [];

    public ObservableCollection<string> Scopes { get; } = [];

    [ObservableProperty]
    private string _search = string.Empty;

    [ObservableProperty]
    private string _selectedScope = AllScopes;

    [ObservableProperty]
    private string _resultSummary = string.Empty;

    [ObservableProperty]
    private GlobalRow? _selected;

    [ObservableProperty]
    private string _editValue = string.Empty;

    [ObservableProperty]
    private bool _editAsFloat;

    public const string AllScopes = "All scopes";

    private object? _scopesSource;

    public override void Refresh()
    {
        if (Shell.Document is not { } save)
        {
            Rows.Clear();
            ResultSummary = string.Empty;
            return;
        }

        // Each game (and each load) has its own symbol table, so the scope list must follow it.
        // Clearing the list makes the ComboBox push null back into SelectedScope, so reset it after.
        if (!ReferenceEquals(_scopesSource, save.Symbols))
        {
            _scopesSource = save.Symbols;
            var previous = SelectedScope;

            Scopes.Clear();
            Scopes.Add(AllScopes);
            foreach (var scope in save.Symbols.Scopes.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            {
                Scopes.Add(scope);
            }

            SelectedScope = previous is not null && Scopes.Contains(previous) ? previous : AllScopes;
            Selected = null;
        }

        Populate();
    }

    partial void OnSearchChanged(string value) => Populate();

    partial void OnSelectedScopeChanged(string value) => Populate();

    partial void OnSelectedChanged(GlobalRow? value)
    {
        if (value is null || Shell.Document is null)
        {
            EditValue = string.Empty;
            return;
        }

        EditAsFloat = value.Global.Type == ScriptVariableType.Float;
        EditValue = value.Value;
    }

    private void Populate()
    {
        if (Shell.Document is not { } save)
        {
            return;
        }

        Rows.Clear();

        var query = save.Symbols.Globals.AsEnumerable();

        if (!string.IsNullOrEmpty(SelectedScope) && !string.Equals(SelectedScope, AllScopes, StringComparison.Ordinal))
        {
            query = query.Where(g => string.Equals(g.Scope, SelectedScope, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            query = query.Where(g => g.Name.Contains(Search, StringComparison.OrdinalIgnoreCase));
        }

        var matched = 0;
        foreach (var global in query)
        {
            for (var element = 0; element < global.ArrayCount; element++)
            {
                matched++;
                if (Rows.Count < MaxRows)
                {
                    Rows.Add(new GlobalRow(global, element, Format(save, global, element)));
                }
            }
        }

        ResultSummary = matched > Rows.Count
            ? $"Showing the first {Rows.Count} of {matched:N0} matches. Narrow the search to see the rest."
            : $"{matched:N0} variable{(matched == 1 ? string.Empty : "s")}.";
    }

    private static string Format(DeSaveFile save, ScriptGlobal global, int element)
    {
        var (asInt, asFloat) = save.Globals.GetRaw(global, element);
        return global.Type == ScriptVariableType.Float
            ? asFloat.ToString("R")
            : asInt.ToString();
    }

    [RelayCommand]
    private void Apply()
    {
        if (Selected is not { } row || Shell.Document is not { } save)
        {
            return;
        }

        Edit(() =>
        {
            if (EditAsFloat)
            {
                if (float.TryParse(EditValue, out var asFloat))
                {
                    save.Globals.SetFloat(row.Global, asFloat, row.Element);
                }
                else
                {
                    Shell.SetStatus($"'{EditValue}' is not a number.", isError: true);
                    return;
                }
            }
            else if (int.TryParse(EditValue, out var asInt))
            {
                save.Globals.SetInt(row.Global, asInt, row.Element);
            }
            else
            {
                Shell.SetStatus($"'{EditValue}' is not a whole number.", isError: true);
                return;
            }

            Shell.SetStatus($"Set {row.Scope}.{row.Name} to {EditValue}.");
        });

        Populate();
    }
}
