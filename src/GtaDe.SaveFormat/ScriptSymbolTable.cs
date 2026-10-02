namespace GtaDe.SaveFormat;

/// <summary>Declared storage type of a script variable.</summary>
public enum ScriptVariableType
{
    Int = 0,
    Float = 1,
}

/// <summary>
/// One entry from the <c>//Globalvariables</c> section of the script symbol table.
/// </summary>
/// <param name="Scope">
/// The compilation unit the variable was declared in, e.g. <c>MAIN</c>, <c>RAMPAGE</c>,
/// <c>TAXI1</c>. Used to group variables into editor pages.
/// </param>
/// <param name="Name">Variable name as written in the script source.</param>
/// <param name="ArrayCount">Element count; 1 for scalars.</param>
/// <param name="Index">Byte offset of the variable within the global variable space.</param>
/// <param name="Used">Compiler usage flags, preserved but not interpreted.</param>
/// <param name="Type">Storage type of each element.</param>
public readonly record struct ScriptGlobal(
    string Scope,
    string Name,
    int ArrayCount,
    int Index,
    int Used,
    ScriptVariableType Type)
{
    /// <summary>Fully qualified name, e.g. <c>RAMPAGE.TOTAL_RAMPAGES_PASSED</c>.</summary>
    public string QualifiedName => $"{Scope}.{Name}";

    public bool IsArray => ArrayCount > 1;

    /// <summary>Total byte size of the variable, including every array element.</summary>
    public int ByteLength => ArrayCount * 4;
}

/// <summary>One entry from the <c>//ScriptNames</c> section.</summary>
public readonly record struct ScriptName(string Name, int Index, int Line);

/// <summary>
/// One entry from the <c>//Mission</c> or <c>//Subscripts</c> sections.
/// </summary>
/// <remarks>
/// The column-header comment in the file claims the index comes first, but the rows actually
/// begin with the source file name, e.g. <c>INTRO.SC,140,0,116418,14396,1568,9411</c>.
/// </remarks>
/// <param name="FileName">Source file the unit was compiled from, e.g. <c>RAMPAGE.SC</c>.</param>
/// <param name="ScriptIndex">Index of the unit within the compiled script.</param>
/// <param name="Ordinal">Position of the unit within its own section.</param>
/// <param name="StartCompiled">Byte offset of the unit inside the compiled script.</param>
/// <param name="StartLine">First source line of the unit.</param>
/// <param name="LineCount">Number of source lines.</param>
/// <param name="ByteCount">Compiled size in bytes.</param>
public readonly record struct ScriptUnit(
    string FileName,
    int ScriptIndex,
    int Ordinal,
    int StartCompiled,
    int StartLine,
    int LineCount,
    int ByteCount)
{
    /// <summary>File name without its <c>.SC</c> extension, which is how scopes are named.</summary>
    public string Scope => System.IO.Path.GetFileNameWithoutExtension(FileName);
}

/// <summary>
/// The script symbol table the Definitive Edition ships inside every save's METADATA block.
/// </summary>
/// <remarks>
/// <para>
/// This is plain Latin-1 text prefixed with a uint32 length. It is the compiler's symbol dump
/// for the mission script, and it is what makes a precise editor possible: every global variable
/// is listed with its name, scope, type and byte offset, so the editor never has to hard-code
/// offsets that could drift between game patches.
/// </para>
/// <para>
/// Sections are delimited by <c>//Name</c> and <c>//EndName</c> lines, with an optional
/// column-header comment line after the opener.
/// </para>
/// </remarks>
public sealed class ScriptSymbolTable
{
    private readonly Dictionary<string, ScriptGlobal> _byQualifiedName;
    private readonly Dictionary<string, List<ScriptGlobal>> _byScope;
    private readonly Dictionary<string, List<ScriptGlobal>> _byBareName;

    private ScriptSymbolTable(
        IReadOnlyList<ScriptGlobal> globals,
        IReadOnlyList<ScriptName> scriptNames,
        IReadOnlyList<ScriptUnit> missions,
        IReadOnlyList<ScriptUnit> subscripts)
    {
        Globals = globals;
        ScriptNames = scriptNames;
        Missions = missions;
        Subscripts = subscripts;

        _byQualifiedName = new Dictionary<string, ScriptGlobal>(StringComparer.OrdinalIgnoreCase);
        _byScope = new Dictionary<string, List<ScriptGlobal>>(StringComparer.OrdinalIgnoreCase);
        _byBareName = new Dictionary<string, List<ScriptGlobal>>(StringComparer.OrdinalIgnoreCase);

        foreach (var global in globals)
        {
            _byQualifiedName[global.QualifiedName] = global;

            if (!_byScope.TryGetValue(global.Scope, out var list))
            {
                list = [];
                _byScope[global.Scope] = list;
            }

            list.Add(global);

            if (!_byBareName.TryGetValue(global.Name, out var sameName))
            {
                sameName = [];
                _byBareName[global.Name] = sameName;
            }

            sameName.Add(global);
        }
    }

    public IReadOnlyList<ScriptGlobal> Globals { get; }

    public IReadOnlyList<ScriptName> ScriptNames { get; }

    public IReadOnlyList<ScriptUnit> Missions { get; }

    public IReadOnlyList<ScriptUnit> Subscripts { get; }

    /// <summary>Scope names in declaration order, e.g. MAIN, INTRO, RAMPAGE, TAXI1.</summary>
    public IEnumerable<string> Scopes => _byScope.Keys;

    /// <summary>Highest byte offset referenced by any global, used to sanity-check the space size.</summary>
    public int MaxIndex => Globals.Count == 0 ? 0 : Globals.Max(g => g.Index + g.ByteLength - 4);

    public bool TryGet(string qualifiedName, out ScriptGlobal global) =>
        _byQualifiedName.TryGetValue(qualifiedName, out global);

    public ScriptGlobal Get(string qualifiedName) =>
        _byQualifiedName.TryGetValue(qualifiedName, out var global)
            ? global
            : throw new SaveFormatException($"The script symbol table has no global named '{qualifiedName}'.");

    public IReadOnlyList<ScriptGlobal> InScope(string scope) =>
        _byScope.TryGetValue(scope, out var list) ? list : [];

    /// <summary>Every global sharing an unqualified name, across all scopes.</summary>
    public IReadOnlyList<ScriptGlobal> ByName(string name) =>
        _byBareName.TryGetValue(name, out var list) ? list : [];

    /// <summary>
    /// Looks a global up by name alone, accepting either <c>SCOPE.NAME</c> or a bare
    /// <c>NAME</c> that is unambiguous across the whole script.
    /// </summary>
    /// <returns>
    /// The matching global, or <see langword="null"/> when the name is unknown or when several
    /// scopes declare it and the caller did not say which one it meant.
    /// </returns>
    public ScriptGlobal? FindGlobal(string name)
    {
        if (_byQualifiedName.TryGetValue(name, out var qualified))
        {
            return qualified;
        }

        var candidates = ByName(name);
        return candidates.Count == 1 ? candidates[0] : null;
    }

    /// <summary>
    /// Like <see cref="FindGlobal"/> but throws rather than returning <see langword="null"/>, so
    /// a mistyped or ambiguous name fails loudly instead of silently doing nothing.
    /// </summary>
    public ScriptGlobal RequireGlobal(string name)
    {
        if (FindGlobal(name) is { } global)
        {
            return global;
        }

        var candidates = ByName(name);
        throw new SaveFormatException(candidates.Count > 1
            ? $"'{name}' is declared in {candidates.Count} scopes ({string.Join(", ", candidates.Select(c => c.Scope))}). Qualify it as SCOPE.NAME."
            : $"The script symbol table has no global named '{name}'.");
    }

    public static ScriptSymbolTable Parse(string text)
    {
        var globals = new List<ScriptGlobal>();
        var scriptNames = new List<ScriptName>();
        var missions = new List<ScriptUnit>();
        var subscripts = new List<ScriptUnit>();

        string? section = null;
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim('\r', ' ', '\t');
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("//", StringComparison.Ordinal))
            {
                var directive = line[2..].Trim();
                var word = directive.Split(' ', ',')[0];

                if (word.StartsWith("End", StringComparison.OrdinalIgnoreCase))
                {
                    section = null;
                }
                else if (IsSectionName(word))
                {
                    section = word;
                }

                // Column-header comments are ignored; they always start with a field name.
                continue;
            }

            switch (section)
            {
                case "Globalvariables" when TryParseGlobal(line, out var global):
                    globals.Add(global);
                    break;
                case "ScriptNames" when TryParseScriptName(line, out var scriptName):
                    scriptNames.Add(scriptName);
                    break;
                case "Mission" when TryParseUnit(line, out var mission):
                    missions.Add(mission);
                    break;
                case "Subscripts" when TryParseUnit(line, out var subscript):
                    subscripts.Add(subscript);
                    break;
            }
        }

        if (globals.Count == 0)
        {
            throw new SaveFormatException(
                "The script symbol table contains no global variables. The metadata block may be corrupt.");
        }

        return new ScriptSymbolTable(globals, scriptNames, missions, subscripts);
    }

    private static bool IsSectionName(string word) => word is
        "Globalvariables" or "LocalVariables" or "Labels" or "ScriptNames" or "VariableInfo" or
        "ConstInfo" or "ConstVals" or "Scripts" or "Subscripts" or "Mission" or "SyncPoints";

    private static bool TryParseGlobal(string line, out ScriptGlobal global)
    {
        global = default;
        var parts = line.Split(',');
        if (parts.Length < 6 ||
            !int.TryParse(parts[2], out var arrayCount) ||
            !int.TryParse(parts[3], out var index) ||
            !int.TryParse(parts[4], out var used) ||
            !int.TryParse(parts[5], out var type))
        {
            return false;
        }

        global = new ScriptGlobal(
            parts[0].Trim(),
            parts[1].Trim(),
            Math.Max(1, arrayCount),
            index,
            used,
            type == 1 ? ScriptVariableType.Float : ScriptVariableType.Int);
        return true;
    }

    private static bool TryParseScriptName(string line, out ScriptName scriptName)
    {
        scriptName = default;
        var parts = line.Split(',');
        if (parts.Length < 3 || !int.TryParse(parts[1], out var index) || !int.TryParse(parts[2], out var lineNumber))
        {
            return false;
        }

        scriptName = new ScriptName(parts[0].Trim(), index, lineNumber);
        return true;
    }

    // Rows are: Filename, ScriptIndex, Ordinal, startCompiled, StartLine, NumberLines, NumBytes
    private static bool TryParseUnit(string line, out ScriptUnit unit)
    {
        unit = default;
        var parts = line.Split(',');
        if (parts.Length < 7)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var scriptIndex) ||
            !int.TryParse(parts[2], out var ordinal) ||
            !int.TryParse(parts[3], out var startCompiled) ||
            !int.TryParse(parts[4], out var startLine) ||
            !int.TryParse(parts[5], out var lineCount) ||
            !int.TryParse(parts[6], out var byteCount))
        {
            return false;
        }

        unit = new ScriptUnit(parts[0].Trim(), scriptIndex, ordinal, startCompiled, startLine, lineCount, byteCount);
        return true;
    }
}
