namespace GtaDe.SaveFormat;

/// <summary>The pieces every Definitive Edition save is parsed into, shared by all three games.</summary>
public sealed record SaveParts(
    byte[] Data,
    SaveLayout Layout,
    ScriptSymbolTable Symbols,
    GlobalVariableSpace Globals,
    SealVariant SealVariant,
    bool SealWasValid);

/// <summary>
/// An open Definitive Edition save slot, independent of which game wrote it.
/// </summary>
/// <remarks>
/// The entire file is held as one mutable byte array and every edit is applied in place. Nothing
/// is ever re-serialised from parsed objects, so a save that is opened and written back without
/// changes is byte-for-byte identical to the original. That property is what keeps unmapped and
/// undocumented regions of the format safe.
/// </remarks>
public abstract class DeSaveFile
{
    protected DeSaveFile(SaveParts parts)
    {
        Data = parts.Data;
        Layout = parts.Layout;
        Symbols = parts.Symbols;
        Globals = parts.Globals;
        SealVariant = parts.SealVariant;
        SealWasValid = parts.SealWasValid;
        OriginalBytes = (byte[])parts.Data.Clone();
    }

    /// <summary>Path the save was loaded from, when it came from disk.</summary>
    public string? Path { get; protected set; }

    public GameKind Game => Layout.Game;

    /// <summary>The live save buffer. Edits are written straight into this array.</summary>
    public byte[] Data { get; }

    /// <summary>A pristine copy of the file as loaded, used for change detection and reverting.</summary>
    public byte[] OriginalBytes { get; private set; }

    public SaveLayout Layout { get; }

    public ScriptSymbolTable Symbols { get; }

    public GlobalVariableSpace Globals { get; }

    /// <summary>Which seal form the file used when loaded. Preserved on write.</summary>
    public SealVariant SealVariant { get; }

    /// <summary>Whether the seal matched when the file was loaded.</summary>
    public bool SealWasValid { get; }

    /// <summary>GXT key of the last mission passed, shown by the game in the load menu.</summary>
    public string LastMissionKey => Layout.Header.LastMissionKey;

    public DateTime SavedAtUtc => Layout.Header.Timestamp;

    /// <summary>True when the buffer differs from the file as it was loaded.</summary>
    public bool IsModified => !Data.AsSpan().SequenceEqual(OriginalBytes);

    /// <summary>Loads a save of any supported game, detecting which one from its header.</summary>
    public static DeSaveFile Load(string path)
    {
        var save = Parse(File.ReadAllBytes(path));
        save.Path = path;
        return save;
    }

    /// <summary>Parses a save of any supported game, detecting which one from its header.</summary>
    public static DeSaveFile Parse(byte[] data) => GameDetection.Detect(data) switch
    {
        GameKind.Gta3 => Gta3SaveFile.Parse(data),
        GameKind.ViceCity => ViceCitySaveFile.Parse(data),
        GameKind.SanAndreas => SanAndreasSaveFile.Parse(data),
        var other => throw new SaveFormatException(
            $"{other.DisplayName()} Definitive Edition saves are not supported yet."),
    };

    /// <summary>Splits a raw file into the parts every game shares, validating as it goes.</summary>
    protected static SaveParts ParseParts(byte[] data, GameKind expected)
    {
        var buffer = (byte[])data.Clone();

        var game = GameDetection.Detect(buffer);
        if (game != expected)
        {
            throw new SaveFormatException(
                $"This is a {game.DisplayName()} save, not a {expected.DisplayName()} save.");
        }

        var detected = SaveSeal.Detect(buffer);
        var sealVariant = detected ?? (game == GameKind.Gta3 ? SealVariant.Inverted : SealVariant.Plain);

        var layout = SaveLayout.Read(buffer, game);
        var symbols = ScriptSymbolTable.Parse(layout.Metadata.GetPayloadText());
        var globals = GlobalVariableSpace.Create(buffer, layout[SaveBlockKind.Scripts], symbols);

        return new SaveParts(buffer, layout, symbols, globals, sealVariant, detected is not null);
    }

    /// <summary>Discards every edit, restoring the buffer to the bytes that were loaded.</summary>
    public void Revert() => OriginalBytes.CopyTo(Data, 0);

    /// <summary>
    /// Produces the bytes to write to disk: the live buffer with a freshly computed seal.
    /// </summary>
    public byte[] ToBytes()
    {
        var output = (byte[])Data.Clone();
        SaveSeal.Apply(output, SealVariant);
        return output;
    }

    /// <summary>
    /// Writes the save, resealing it so the game accepts it. The replacement is written to a
    /// temporary file first, so an interrupted write cannot leave a truncated save behind.
    /// </summary>
    /// <param name="path">Destination path, or <c>null</c> to overwrite the loaded file.</param>
    /// <param name="createBackup">
    /// Keep a <c>.bak</c> copy of the file as it was before the editor first wrote to it. An
    /// existing backup is never overwritten, so repeatedly editing and testing a save cannot
    /// erase the pristine original — which is the only copy worth restoring.
    /// </param>
    public void Save(string? path = null, bool createBackup = true)
    {
        var target = path ?? Path
            ?? throw new InvalidOperationException("This save has no path; supply one explicitly.");

        var bytes = ToBytes();
        var temp = target + ".tmp";
        File.WriteAllBytes(temp, bytes);

        try
        {
            if (File.Exists(target))
            {
                var backup = target + ".bak";
                if (createBackup && !File.Exists(backup))
                {
                    File.Copy(target, backup);
                }

                // Swaps the files in one step, so the original stays put if the replacement fails.
                File.Replace(temp, target, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temp, target);
            }
        }
        catch
        {
            try
            {
                File.Delete(temp);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            throw;
        }

        Path = target;
        bytes.CopyTo(Data, 0);
        OriginalBytes = bytes;
    }
}
