namespace GtaDe.SaveFormat;

/// <summary>
/// Typed read/write access to the mission script's global variable space.
/// </summary>
/// <remarks>
/// <para>Layout of the Scripts block payload:</para>
/// <code>
/// +0   u32 varSpaceSize   total byte size of the global variable space
/// +4   byte[8]            opaque compiler header, observed 02 00 01 8C 44 00 00 6C
/// +12  globals            variable space, index 0 starts here
/// </code>
/// <para>
/// Because <see cref="SaveBlock.DataOffset"/> already points past the <c>SCR</c> tag and size,
/// the absolute file offset of a global is <c>DataOffset + 12 + Index</c>, which is
/// <c>scrTagOffset + 20 + Index</c>.
/// </para>
/// <para>
/// Getting this base wrong is the single most dangerous mistake an editor can make: an eight
/// byte shift silently writes into neighbouring variables and corrupts the save. It is therefore
/// verified on load against three constants the compiler always emits first.
/// </para>
/// </remarks>
public sealed class GlobalVariableSpace
{
    /// <summary>Bytes between the start of the Scripts payload and global index 0.</summary>
    public const int GlobalsRelativeOffset = 12;

    /// <summary>Byte length of the opaque compiler header between the size field and the globals.</summary>
    public const int OpaqueHeaderLength = 8;

    /// <summary>
    /// Constants the compiler emits at the start of the GTA III and Vice City main scripts. Their
    /// presence at the expected offsets proves the global base is correct. GTA III files them under
    /// the MAIN scope; Vice City leaves every global unscoped, so the bare name is tried as well.
    /// </summary>
    private static readonly (string Name, float Value)[] Anchors =
    [
        ("ONE_SIXTEENTH", 0.0625f),
        ("ONE_THIRTYSECOND", 0.03125f),
        ("ONE_SIXTYFOURTH", 0.015625f),
    ];

    private readonly byte[] _data;

    private GlobalVariableSpace(byte[] data, ScriptSymbolTable symbols, int baseOffset, int size)
    {
        _data = data;
        Symbols = symbols;
        BaseOffset = baseOffset;
        Size = size;
    }

    public ScriptSymbolTable Symbols { get; }

    /// <summary>Absolute file offset of global index 0.</summary>
    public int BaseOffset { get; }

    /// <summary>Declared byte size of the global variable space.</summary>
    public int Size { get; }

    public static GlobalVariableSpace Create(byte[] data, SaveBlock scripts, ScriptSymbolTable symbols)
    {
        if (scripts.Kind != SaveBlockKind.Scripts)
        {
            throw new ArgumentException("A Scripts block is required.", nameof(scripts));
        }

        var declaredSize = (int)ByteOps.ReadUInt32(data, scripts.DataOffset);
        var baseOffset = scripts.DataOffset + GlobalsRelativeOffset;

        if (declaredSize < 0 || baseOffset + declaredSize > data.Length)
        {
            throw new SaveFormatException(
                $"The Scripts block declares a {declaredSize} byte variable space that does not fit in the file.");
        }

        var space = new GlobalVariableSpace(data, symbols, baseOffset, declaredSize);
        space.VerifyAnchors();
        return space;
    }

    /// <summary>
    /// Confirms that the computed global base lines up with the symbol table. Throws if it does
    /// not, rather than risk writing to the wrong offsets.
    /// </summary>
    private void VerifyAnchors()
    {
        var checkedAny = false;

        foreach (var (name, expected) in Anchors)
        {
            if (!Symbols.TryGet("MAIN." + name, out var global))
            {
                if (Symbols.FindGlobal(name) is not { } bare)
                {
                    continue;
                }

                global = bare;
            }

            checkedAny = true;
            var actual = ByteOps.ReadSingle(_data, BaseOffset + global.Index);
            if (Math.Abs(actual - expected) > 1e-9f)
            {
                throw new SaveFormatException(
                    $"Script global base check failed: expected {name} to be {expected} but read {actual}. " +
                    "Refusing to edit this save because variable offsets cannot be trusted.");
            }
        }

        if (!checkedAny)
        {
            throw new SaveFormatException(
                "The script symbol table is missing the compiler constants used to validate variable offsets.");
        }

        // The compiler reserves the size field plus the opaque header ahead of the globals.
        var expectedSize = Symbols.MaxIndex + 4 + OpaqueHeaderLength;
        if (expectedSize > Size)
        {
            throw new SaveFormatException(
                $"The symbol table references offsets up to {Symbols.MaxIndex} but the variable space is only " +
                $"{Size} bytes. The save and its metadata do not match.");
        }
    }

    private int OffsetOf(ScriptGlobal global, int element)
    {
        if (element < 0 || element >= global.ArrayCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(element),
                element,
                $"{global.QualifiedName} has {global.ArrayCount} element(s).");
        }

        var offset = BaseOffset + global.Index + (element * 4);
        if (offset + 4 > _data.Length)
        {
            throw new SaveFormatException($"{global.QualifiedName} lies outside the save file.");
        }

        return offset;
    }

    public int GetInt(ScriptGlobal global, int element = 0) => ByteOps.ReadInt32(_data, OffsetOf(global, element));

    public float GetFloat(ScriptGlobal global, int element = 0) => ByteOps.ReadSingle(_data, OffsetOf(global, element));

    public void SetInt(ScriptGlobal global, int value, int element = 0) =>
        ByteOps.WriteInt32(_data, OffsetOf(global, element), value);

    public void SetFloat(ScriptGlobal global, float value, int element = 0) =>
        ByteOps.WriteSingle(_data, OffsetOf(global, element), value);

    public int GetInt(string name, int element = 0) => GetInt(Symbols.RequireGlobal(name), element);

    public float GetFloat(string name, int element = 0) => GetFloat(Symbols.RequireGlobal(name), element);

    public void SetInt(string name, int value, int element = 0) =>
        SetInt(Symbols.RequireGlobal(name), value, element);

    public void SetFloat(string name, float value, int element = 0) =>
        SetFloat(Symbols.RequireGlobal(name), value, element);

    /// <summary>True when the save's symbol table declares this variable unambiguously.</summary>
    public bool Has(string name) => Symbols.FindGlobal(name) is not null;

    /// <summary>Reads a global without caring about its declared type, for the raw globals page.</summary>
    public (int AsInt, float AsFloat) GetRaw(ScriptGlobal global, int element = 0)
    {
        var offset = OffsetOf(global, element);
        return (ByteOps.ReadInt32(_data, offset), ByteOps.ReadSingle(_data, offset));
    }

    public bool GetFlag(string name, int element = 0) => GetInt(name, element) != 0;

    public void SetFlag(string name, bool value, int element = 0) =>
        SetInt(name, value ? 1 : 0, element);
}
