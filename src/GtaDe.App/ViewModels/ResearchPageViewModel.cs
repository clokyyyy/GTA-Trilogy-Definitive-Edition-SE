using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GtaDe.SaveFormat;

namespace GtaDe.App.ViewModels;

/// <summary>One annotated difference between the open save and a comparison file.</summary>
public sealed class DifferenceRow(SaveDifference difference)
{
    public string Offset { get; } = $"0x{difference.Offset:X6}";

    public int Length { get; } = difference.Length;

    public string Region { get; } = difference.Region;

    public string Detail { get; } = difference.Detail ?? string.Empty;

    public string Before { get; } = Describe(difference.Before, difference.AsInt32?.Before);

    public string After { get; } = Describe(difference.After, difference.AsInt32?.After);

    private static string Describe(byte[] bytes, int? asInt)
    {
        if (bytes.Length == 0)
        {
            return "—";
        }

        var hex = Convert.ToHexString(bytes);
        if (hex.Length > 24)
        {
            hex = hex[..24] + "…";
        }

        return asInt is null ? hex : $"{asInt} ({hex})";
    }
}

/// <summary>
/// The tools used to work the format out in the first place: a two-save diff that names what
/// changed, an offset inspector, and an export of the save's own symbol table.
/// </summary>
public sealed partial class ResearchPageViewModel(MainWindowViewModel shell) : PageViewModel(shell)
{
    public override string Title => "Research tools";

    public override string Description =>
        "Compare two saves to see exactly which variables differ, inspect any offset, and export the symbol table this save carries.";

    public override string Icon => "M15.5 14h-.8l-.3-.3a6.5 6.5 0 1 0-.7.7l.3.3v.8l5 5 1.5-1.5-5-5Zm-6 0a4.5 4.5 0 1 1 0-9 4.5 4.5 0 0 1 0 9Z";

    public override string Group => "Advanced";

    public override bool IsAvailable => Shell.HasSave && Shell.Settings.ShowAdvancedTools;

    public ObservableCollection<DifferenceRow> Differences { get; } = [];

    [ObservableProperty]
    private string _comparisonPath = string.Empty;

    [ObservableProperty]
    private string _comparisonSummary = "Pick a second save to compare against the one that is open.";

    [ObservableProperty]
    private string _inspectOffset = "0";

    [ObservableProperty]
    private string _inspectResult = string.Empty;

    [ObservableProperty]
    private string _symbolSummary = string.Empty;

    public override void Refresh()
    {
        if (Shell.Document is not { } save)
        {
            Differences.Clear();
            SymbolSummary = string.Empty;
            return;
        }

        var scopes = save.Symbols.Scopes.Count();
        SymbolSummary =
            $"{save.Symbols.Globals.Count:N0} named globals across {scopes:N0} {(scopes == 1 ? "scope" : "scopes")}, " +
            $"{save.Globals.Size:N0} bytes of variable space starting at 0x{save.Globals.BaseOffset:X}.";
    }

    [RelayCommand]
    private async Task CompareAsync()
    {
        if (Shell.Document is null || Shell.StorageProvider is null)
        {
            return;
        }

        var files = await Shell.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Pick a save to compare against",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Definitive Edition save") { Patterns = ["GTA*sf*.sav*", "*.sav"] }],
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        CompareWith(path);
    }

    /// <summary>Diffs the open save against the file at <paramref name="path"/>.</summary>
    public void CompareWith(string path)
    {
        if (Shell.Document is not { } save)
        {
            return;
        }

        try
        {
            var other = DeSaveFile.Load(path);
            var differences = SaveMap.Compare(save, other);

            Differences.Clear();
            foreach (var difference in differences.Take(2000))
            {
                Differences.Add(new DifferenceRow(difference));
            }

            ComparisonPath = path;
            ComparisonSummary = differences.Count == 0
                ? "The two files are byte identical."
                : $"{differences.Count:N0} changed run{(differences.Count == 1 ? string.Empty : "s")} " +
                  $"against {Path.GetFileName(path)}" +
                  (differences.Count > Differences.Count ? $", showing the first {Differences.Count:N0}." : ".");
        }
        catch (Exception ex)
        {
            ComparisonSummary = $"Could not read {Path.GetFileName(path)}: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Inspect()
    {
        if (Shell.Document is not { } save)
        {
            return;
        }

        var text = InspectOffset.Trim();
        var isHex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);

        var parsed = isHex
            ? int.TryParse(text[2..], System.Globalization.NumberStyles.HexNumber, null, out var hex) ? hex : -1
            : int.TryParse(text, out var dec) ? dec : -1;

        if (parsed < 0 || parsed >= save.Data.Length)
        {
            InspectResult = $"Offset must be between 0 and 0x{save.Data.Length - 1:X}.";
            return;
        }

        var (region, detail) = SaveMap.Describe(save, parsed);
        var remaining = Math.Min(16, save.Data.Length - parsed);
        var bytes = save.Data.AsSpan(parsed, remaining);

        var builder = new StringBuilder();
        builder.AppendLine($"0x{parsed:X6}  {region}{(detail is null ? string.Empty : $" — {detail}")}");
        builder.AppendLine($"bytes   {Convert.ToHexString(bytes)}");

        if (remaining >= 4)
        {
            builder.AppendLine($"int32   {BitConverter.ToInt32(bytes)}");
            builder.AppendLine($"uint32  {BitConverter.ToUInt32(bytes)}");
            builder.AppendLine($"float   {BitConverter.ToSingle(bytes):R}");
        }

        if (remaining >= 2)
        {
            builder.AppendLine($"int16   {BitConverter.ToInt16(bytes)}");
        }

        InspectResult = builder.ToString().TrimEnd();
    }

    [RelayCommand]
    private async Task ExportSymbolsAsync()
    {
        if (Shell.Document is not { } save || Shell.StorageProvider is null)
        {
            return;
        }

        var file = await Shell.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export the symbol table",
            SuggestedFileName = $"{save.Game.ToString().ToLowerInvariant()}-globals.csv",
            DefaultExtension = "csv",
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            var builder = new StringBuilder();
            builder.AppendLine("Scope,Name,Type,ArrayCount,Index,FileOffset,Value");

            foreach (var global in save.Symbols.Globals)
            {
                for (var element = 0; element < global.ArrayCount; element++)
                {
                    var (asInt, asFloat) = save.Globals.GetRaw(global, element);
                    var value = global.Type == ScriptVariableType.Float
                        ? asFloat.ToString("R")
                        : asInt.ToString();

                    var index = global.Index + (element * 4);

                    builder.Append(global.Scope).Append(',')
                        .Append(global.ArrayCount > 1 ? $"{global.Name}[{element}]" : global.Name).Append(',')
                        .Append(global.Type).Append(',')
                        .Append(global.ArrayCount).Append(',')
                        .Append(index).Append(',')
                        .Append(save.Globals.BaseOffset + index).Append(',')
                        .AppendLine(value);
                }
            }

            await File.WriteAllTextAsync(path, builder.ToString());
            Shell.SetStatus($"Exported the symbol table to {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            Shell.SetStatus($"Could not export: {ex.Message}", isError: true);
        }
    }
}
