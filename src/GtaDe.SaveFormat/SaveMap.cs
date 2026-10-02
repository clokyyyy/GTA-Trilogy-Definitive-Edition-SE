namespace GtaDe.SaveFormat;

/// <summary>A run of bytes that differ between two saves, with the meaning of its start offset.</summary>
public sealed record SaveDifference(int Offset, int Length, string Region, string? Detail, byte[] Before, byte[] After)
{
    public string BeforeHex => Convert.ToHexString(Before);

    public string AfterHex => Convert.ToHexString(After);

    /// <summary>The run read as a 32-bit integer pair, when it is small enough to be one.</summary>
    public (int Before, int After)? AsInt32 => Length is > 0 and <= 4
        ? (ByteOps.ReadInt32(Pad(Before), 0), ByteOps.ReadInt32(Pad(After), 0))
        : null;

    private static byte[] Pad(byte[] value)
    {
        if (value.Length == 4)
        {
            return value;
        }

        var padded = new byte[4];
        value.CopyTo(padded, 0);
        return padded;
    }
}

/// <summary>
/// Names what lives at a byte offset, and compares two saves in those terms.
/// </summary>
/// <remarks>
/// This exists because a raw byte diff of two saves is almost unreadable — a change at 0x1A4C
/// means nothing on its own. Resolving each run to "Scripts globals -> RAMPAGE.TOTAL_RAMPAGES_PASSED"
/// is what turns a diff into evidence, and it is how the reward formulas and the real rampage flag
/// were worked out in the first place.
/// </remarks>
public static class SaveMap
{
    /// <summary>Describes the offset: which region it falls in, and the variable or field if known.</summary>
    public static (string Region, string? Detail) Describe(DeSaveFile save, int offset)
    {
        var layout = save.Layout;

        if (offset < 8)
        {
            return ("Header", "Magic and version");
        }

        if (offset < 24)
        {
            return ("Header", "Integrity seal");
        }

        if (offset < layout.SimpleVarsOffset)
        {
            return ("Header", "Timestamp and last mission key");
        }

        if (offset < layout.SimpleVarsOffset + layout.SimpleVarsLength)
        {
            return ("SimpleVars", DescribeSimpleVars(offset - layout.SimpleVarsOffset, layout.Game));
        }

        if (offset < layout.Metadata.FileOffset + layout.Metadata.Length)
        {
            return ("Metadata", "Compressed script symbol table");
        }

        if (layout.GapLength > 0 && offset >= layout.GapOffset && offset < layout.GapOffset + layout.GapLength)
        {
            return ("DE region", $"+0x{offset - layout.GapOffset:X}");
        }

        foreach (var block in layout.Blocks)
        {
            if (offset < block.Offset || offset >= block.EndOffset)
            {
                continue;
            }

            var detail = block.Kind switch
            {
                SaveBlockKind.Scripts => DescribeGlobal(save, offset),
                SaveBlockKind.PlayerInfo => $"+0x{offset - block.DataOffset:X}",
                SaveBlockKind.Stats => $"+0x{offset - block.DataOffset:X}",
                _ => $"+0x{offset - block.DataOffset:X}",
            };

            return (block.Kind.ToString(), detail);
        }

        return ("Trailer", $"+0x{offset - layout.TrailerOffset:X}");
    }

    private static string? DescribeGlobal(DeSaveFile save, int offset)
    {
        var globals = save.Globals;
        var index = offset - globals.BaseOffset;

        if (index < 0 || index >= globals.Size)
        {
            return "Script block header";
        }

        // Globals are four bytes each from the base, so round down to the variable that owns
        // this byte rather than requiring the run to start exactly on a boundary.
        var aligned = index - (index % 4);

        var candidates = save.Symbols.Globals
            .Where(g => g.Index <= aligned && aligned < g.Index + (g.ArrayCount * 4))
            .OrderByDescending(g => g.Index)
            .ToList();

        if (candidates.Count == 0)
        {
            return $"Unnamed global at index {aligned}";
        }

        var match = candidates[0];
        var element = (aligned - match.Index) / 4;
        var name = match.ArrayCount > 1 ? $"{match.Name}[{element}]" : match.Name;

        return $"{match.Scope}.{name}";
    }

    private static string DescribeSimpleVars(int relative, GameKind game) => game == GameKind.SanAndreas
        ? DescribeSanAndreasSimpleVars(relative)
        : relative switch
    {
        >= 0x00 and < 0x04 => "Signature",
        >= 0x04 and < 0x08 => game == GameKind.Gta3 ? "Island" : "Level",
        >= 0x08 and < 0x14 => "Camera position",
        >= 0x14 and < 0x18 => "Milliseconds per game minute",
        >= 0x18 and < 0x1C => "Last clock tick",
        0x1C => "Game clock hour",
        0x1D => "Game clock minute",
        >= 0x20 and < 0x24 => "Timer",
        >= 0x24 and < 0x28 => "Time scale",
        >= 0x30 and < 0x34 when game == GameKind.Gta3 => "Frame counter",
        >= 0x40 and < 0x42 => "Previous weather",
        >= 0x42 and < 0x44 => "Current weather",
        >= 0x44 and < 0x46 => "Forced weather",
        >= 0x46 and < 0x4A => "Weather interpolation",
        >= 0x74 and < 0x9C when game == GameKind.ViceCity => "Radio station positions",
        _ => $"+0x{relative:X}",
    };

    /// <summary>Relative to the SIMPLE_VARIABLES section start (its name length field).</summary>
    private static string DescribeSanAndreasSimpleVars(int relative) => relative switch
    {
        < 0x15 => "Section name",
        < 0x21 => "Last mission key",
        >= 0x21 and < 0x25 => "Version id",
        >= 0x2A and < 0x36 => "Camera position",
        >= 0x36 and < 0x3A => "Milliseconds per game minute",
        >= 0x3A and < 0x3E => "Last clock tick",
        0x3E => "Game clock month",
        0x3F => "Game clock day",
        0x40 => "Game clock hour",
        0x41 => "Game clock minute",
        >= 0x4C and < 0x50 => "Timer",
        >= 0x60 and < 0x62 => "Previous weather",
        >= 0x62 and < 0x64 => "Current weather",
        >= 0x64 and < 0x66 => "Forced weather",
        >= 0x66 and < 0x6A => "Weather interpolation",
        _ => $"+0x{relative:X}",
    };

    /// <summary>
    /// Lists every run of bytes that differs between two saves. Runs are joined across gaps of up
    /// to <paramref name="joinGap"/> bytes so a changed 32-bit value does not come back as four
    /// separate one-byte findings.
    /// </summary>
    public static IReadOnlyList<SaveDifference> Compare(DeSaveFile left, DeSaveFile right, int joinGap = 3)
    {
        var a = left.Data;
        var b = right.Data;
        var shared = Math.Min(a.Length, b.Length);
        var results = new List<SaveDifference>();

        var start = -1;
        var lastDiff = -1;

        for (var i = 0; i <= shared; i++)
        {
            var differs = i < shared && a[i] != b[i];

            if (differs)
            {
                if (start < 0)
                {
                    start = i;
                }

                lastDiff = i;
                continue;
            }

            if (start >= 0 && (i >= shared || i - lastDiff > joinGap))
            {
                var length = lastDiff - start + 1;
                var (region, detail) = Describe(left, start);

                results.Add(new SaveDifference(
                    start,
                    length,
                    region,
                    detail,
                    a[start..(start + length)],
                    b[start..(start + length)]));

                start = -1;
            }
        }

        if (a.Length != b.Length)
        {
            var longer = a.Length > b.Length ? a : b;
            results.Add(new SaveDifference(
                shared,
                longer.Length - shared,
                "File length",
                $"{a.Length:N0} bytes vs {b.Length:N0} bytes",
                [],
                []));
        }

        return results;
    }
}
