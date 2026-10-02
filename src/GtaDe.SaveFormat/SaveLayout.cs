namespace GtaDe.SaveFormat;

/// <summary>Identifies a block in a Definitive Edition save block chain.</summary>
public enum SaveBlockKind
{
    Scripts,
    PedPool,
    Garages,
    Vehicles,
    Objects,
    PathFind,
    Cranes,
    Pickups,
    PhoneInfo,
    RestartPoints,
    RadarBlips,
    Zones,
    GangData,
    CarGenerators,
    Particles,
    AudioScriptObjects,
    PlayerInfo,
    Stats,
    Streaming,
    PedTypeInfo,

    /// <summary>Vice City: CGameLogic (shortcut taxi and restart state).</summary>
    GameLogic,

    /// <summary>Vice City: scripted object paths.</summary>
    ScriptPaths,

    /// <summary>Vice City: CSetPieces (scripted police ambushes).</summary>
    SetPieces,

    /// <summary>San Andreas: the SIMPLE_VARIABLES named block.</summary>
    SimpleVariables,

    /// <summary>San Andreas: the METADATA named block (script symbol table).</summary>
    Metadata,

    /// <summary>San Andreas: graffiti tags sprayed by the player.</summary>
    Tags,

    /// <summary>San Andreas: interior placement (IPL) state.</summary>
    Ipls,

    /// <summary>San Andreas: shop and clothing state.</summary>
    Shopping,

    /// <summary>San Andreas: gang war state.</summary>
    GangWars,

    /// <summary>San Andreas: unique stunt jumps.</summary>
    StuntJumps,

    /// <summary>San Andreas: interior entry and exit markers.</summary>
    EntryExits,

    /// <summary>San Andreas: radio station state.</summary>
    Radio,

    /// <summary>San Andreas: script 3D markers.</summary>
    User3DMarkers,

    /// <summary>San Andreas: post effects (night/heat vision).</summary>
    PostEffects,

    /// <summary>San Andreas: the mission brief history.</summary>
    Briefs,
}

/// <summary>
/// A located block within the save buffer. Blocks are never moved or resized by the editor,
/// so these offsets stay valid for the lifetime of the document.
/// </summary>
/// <param name="Kind">Which block this is, derived from its position in the chain.</param>
/// <param name="Tag">The four-character tag, when the block carries one.</param>
/// <param name="Offset">Offset of the first byte of the block, including any size prefix or tag.</param>
/// <param name="DataOffset">Offset of the first payload byte.</param>
/// <param name="DataLength">Payload length in bytes.</param>
/// <param name="TotalLength">Bytes the block occupies in the file, including its header.</param>
public readonly record struct SaveBlock(
    SaveBlockKind Kind,
    string? Tag,
    int Offset,
    int DataOffset,
    int DataLength,
    int TotalLength)
{
    public int EndOffset => Offset + TotalLength;

    public Span<byte> Data(byte[] buffer) => buffer.AsSpan(DataOffset, DataLength);
}

/// <summary>
/// The per-game shape of a Definitive Edition save: how long SimpleVars is and which blocks
/// follow the Scripts block, in order.
/// </summary>
public sealed record SaveLayoutSpec(
    GameKind Game,
    int SimpleVarsLength,
    IReadOnlyList<SaveBlockKind> ChainOrder,
    IReadOnlyDictionary<SaveBlockKind, string> ExpectedTags,
    int MaxGapAfterScripts)
{
    /// <summary>GTA III: 118-byte SimpleVars and the classic 19-block PC chain.</summary>
    public static SaveLayoutSpec Gta3 { get; } = new(
        GameKind.Gta3,
        118,
        [
            SaveBlockKind.PedPool,
            SaveBlockKind.Garages,
            SaveBlockKind.Vehicles,
            SaveBlockKind.Objects,
            SaveBlockKind.PathFind,
            SaveBlockKind.Cranes,
            SaveBlockKind.Pickups,
            SaveBlockKind.PhoneInfo,
            SaveBlockKind.RestartPoints,
            SaveBlockKind.RadarBlips,
            SaveBlockKind.Zones,
            SaveBlockKind.GangData,
            SaveBlockKind.CarGenerators,
            SaveBlockKind.Particles,
            SaveBlockKind.AudioScriptObjects,
            SaveBlockKind.PlayerInfo,
            SaveBlockKind.Stats,
            SaveBlockKind.Streaming,
            SaveBlockKind.PedTypeInfo,
        ],
        new Dictionary<SaveBlockKind, string>
        {
            [SaveBlockKind.Scripts] = "SCR",
            [SaveBlockKind.RestartPoints] = "RST",
            [SaveBlockKind.RadarBlips] = "RDR",
            [SaveBlockKind.Zones] = "ZNS",
            [SaveBlockKind.GangData] = "GNG",
            [SaveBlockKind.CarGenerators] = "CGN",
            [SaveBlockKind.AudioScriptObjects] = "AUD",
            [SaveBlockKind.PedTypeInfo] = "PTP",
        },
        0);

    /// <summary>
    /// Vice City: 156-byte SimpleVars and the 22-block reVC chain. The DE inserts a small region
    /// of unsized data between the Scripts block and the PedPool, which is located by scanning.
    /// </summary>
    public static SaveLayoutSpec ViceCity { get; } = new(
        GameKind.ViceCity,
        156,
        [
            SaveBlockKind.PedPool,
            SaveBlockKind.Garages,
            SaveBlockKind.GameLogic,
            SaveBlockKind.Vehicles,
            SaveBlockKind.Objects,
            SaveBlockKind.PathFind,
            SaveBlockKind.Cranes,
            SaveBlockKind.Pickups,
            SaveBlockKind.PhoneInfo,
            SaveBlockKind.RestartPoints,
            SaveBlockKind.RadarBlips,
            SaveBlockKind.Zones,
            SaveBlockKind.GangData,
            SaveBlockKind.CarGenerators,
            SaveBlockKind.Particles,
            SaveBlockKind.AudioScriptObjects,
            SaveBlockKind.ScriptPaths,
            SaveBlockKind.PlayerInfo,
            SaveBlockKind.Stats,
            SaveBlockKind.SetPieces,
            SaveBlockKind.Streaming,
            SaveBlockKind.PedTypeInfo,
        ],
        new Dictionary<SaveBlockKind, string>
        {
            [SaveBlockKind.Scripts] = "SCR",
            [SaveBlockKind.RestartPoints] = "RST",
            [SaveBlockKind.RadarBlips] = "RDR",
            [SaveBlockKind.Zones] = "ZNS",
            [SaveBlockKind.GangData] = "GNG",
            [SaveBlockKind.CarGenerators] = "CGN",
            [SaveBlockKind.AudioScriptObjects] = "AUD",
            [SaveBlockKind.PedTypeInfo] = "PTP",
        },
        8192);

    /// <summary>
    /// San Andreas: every section is a named block (<c>u32 nameLength, NAME\0</c>) with no size
    /// prefix, so the order below is matched by name rather than by walking sizes.
    /// </summary>
    public static SaveLayoutSpec SanAndreas { get; } = new(
        GameKind.SanAndreas,
        0,
        [],
        new Dictionary<SaveBlockKind, string>(),
        0);

    /// <summary>The San Andreas named blocks, in file order.</summary>
    public static IReadOnlyList<(SaveBlockKind Kind, string Name)> SanAndreasBlocks { get; } =
    [
        (SaveBlockKind.SimpleVariables, "SIMPLE_VARIABLES"),
        (SaveBlockKind.Metadata, "METADATA"),
        (SaveBlockKind.Scripts, "SCRIPTS"),
        (SaveBlockKind.PedPool, "POOLS"),
        (SaveBlockKind.Garages, "GARAGES"),
        (SaveBlockKind.GameLogic, "GAMELOGIC"),
        (SaveBlockKind.PathFind, "PATHS"),
        (SaveBlockKind.Pickups, "PICKUPS"),
        (SaveBlockKind.RestartPoints, "RESTART"),
        (SaveBlockKind.RadarBlips, "RADAR"),
        (SaveBlockKind.Zones, "ZONES"),
        (SaveBlockKind.GangData, "GANGS"),
        (SaveBlockKind.CarGenerators, "CARGENERATORS"),
        (SaveBlockKind.PlayerInfo, "PLAYERINFO"),
        (SaveBlockKind.Stats, "STATS"),
        (SaveBlockKind.SetPieces, "SETPIECES"),
        (SaveBlockKind.Streaming, "STREAMING"),
        (SaveBlockKind.PedTypeInfo, "PEDTYPE"),
        (SaveBlockKind.Tags, "TAGS"),
        (SaveBlockKind.Ipls, "IPLS"),
        (SaveBlockKind.Shopping, "SHOPPING"),
        (SaveBlockKind.GangWars, "GANGWARS"),
        (SaveBlockKind.StuntJumps, "STUNTJUMPS"),
        (SaveBlockKind.EntryExits, "ENTRYEXIT"),
        (SaveBlockKind.Radio, "RADIO"),
        (SaveBlockKind.User3DMarkers, "USER_3DMARKERS"),
        (SaveBlockKind.PostEffects, "POSTEFFECTS"),
        (SaveBlockKind.Briefs, "BRIEFS"),
    ];

    public static SaveLayoutSpec For(GameKind game) => game switch
    {
        GameKind.Gta3 => Gta3,
        GameKind.ViceCity => ViceCity,
        GameKind.SanAndreas => SanAndreas,
        _ => throw new SaveFormatException($"{game.DisplayName()} Definitive Edition saves are not supported yet."),
    };
}

/// <summary>
/// Walks a Definitive Edition save file and records where everything lives.
/// </summary>
/// <remarks>
/// <para>File layout:</para>
/// <code>
/// header        see SaveHeader
/// SimpleVars    118 bytes (GTA III) or 156 bytes (Vice City), not size-prefixed
/// u32           byte length of the METADATA container
/// METADATA      Unreal compressed container holding the script symbol table
/// SCR block     "SCR\0" + u32 size + data   (no outer size prefix)
/// [gap]         Vice City only: a short unsized DE region, kept verbatim
/// N blocks      u32 size + data             (tagged blocks repeat tag + size inside the data)
/// trailer       BRIEF block and any other DE additions, kept verbatim
/// </code>
/// <para>
/// Blocks appear in the classic PC order for each game, which is what lets the untagged ones be
/// identified by position. The tagged ones are cross-checked against their expected tag.
/// </para>
/// </remarks>
public sealed class SaveLayout
{
    private readonly Dictionary<SaveBlockKind, SaveBlock> _blocks;

    private SaveLayout(
        SaveLayoutSpec spec,
        SaveHeader header,
        int simpleVarsOffset,
        int simpleVarsLength,
        int metadataLengthOffset,
        CompressedContainer metadata,
        Dictionary<SaveBlockKind, SaveBlock> blocks,
        IReadOnlyList<SaveBlock> ordered,
        int gapOffset,
        int gapLength,
        int trailerOffset,
        int trailerLength)
    {
        Spec = spec;
        Header = header;
        SimpleVarsOffset = simpleVarsOffset;
        SimpleVarsLength = simpleVarsLength;
        MetadataLengthOffset = metadataLengthOffset;
        Metadata = metadata;
        _blocks = blocks;
        Blocks = ordered;
        GapOffset = gapOffset;
        GapLength = gapLength;
        TrailerOffset = trailerOffset;
        TrailerLength = trailerLength;
    }

    public SaveLayoutSpec Spec { get; }

    public GameKind Game => Spec.Game;

    public SaveHeader Header { get; }

    public int SimpleVarsOffset { get; }

    public int SimpleVarsLength { get; }

    public int MetadataLengthOffset { get; }

    /// <summary>The compressed METADATA container holding the script symbol table.</summary>
    public CompressedContainer Metadata { get; }

    /// <summary>Every located block, in file order.</summary>
    public IReadOnlyList<SaveBlock> Blocks { get; }

    /// <summary>Offset of the unsized region after the Scripts block (Vice City), or the PedPool offset.</summary>
    public int GapOffset { get; }

    /// <summary>Length of the unsized region after the Scripts block; zero for GTA III.</summary>
    public int GapLength { get; }

    /// <summary>Offset of the trailing region (BRIEF and friends) that the editor preserves verbatim.</summary>
    public int TrailerOffset { get; }

    public int TrailerLength { get; }

    public SaveBlock this[SaveBlockKind kind] => _blocks.TryGetValue(kind, out var block)
        ? block
        : throw new SaveFormatException($"The save does not contain a {kind} block.");

    public bool TryGetBlock(SaveBlockKind kind, out SaveBlock block) => _blocks.TryGetValue(kind, out block);

    /// <summary>Reads the layout, choosing the game from the header version.</summary>
    public static SaveLayout Read(byte[] data) => Read(data, GameDetection.Detect(data));

    public static SaveLayout Read(byte[] data, GameKind game)
    {
        var spec = SaveLayoutSpec.For(game);
        var header = SaveHeader.Read(data);

        if (game == GameKind.SanAndreas)
        {
            return ReadNamedBlocks(data, spec, header);
        }

        var simpleVarsOffset = header.Length;
        var metadataLengthOffset = simpleVarsOffset + spec.SimpleVarsLength;
        if (metadataLengthOffset + 4 > data.Length)
        {
            throw new SaveFormatException("The file ends before the metadata length field.");
        }

        var metadataLength = (int)ByteOps.ReadUInt32(data, metadataLengthOffset);
        var metadataOffset = metadataLengthOffset + 4;
        var metadata = CompressedContainer.Read(data, metadataOffset);

        if (metadata.Length != metadataLength)
        {
            throw new SaveFormatException(
                $"The metadata container is {metadata.Length} bytes but the save declares {metadataLength}.");
        }

        var scripts = ReadTaggedBlock(data, metadata.EndOffset, SaveBlockKind.Scripts, spec);
        var gapOffset = scripts.EndOffset;

        List<SaveBlock>? chain = null;
        var gap = 0;
        SaveFormatException? firstError = null;

        for (; gap <= spec.MaxGapAfterScripts; gap++)
        {
            try
            {
                chain = ReadChain(data, gapOffset + gap, spec);
                break;
            }
            catch (SaveFormatException ex)
            {
                firstError ??= ex;
            }
        }

        if (chain is null)
        {
            throw firstError ?? new SaveFormatException("The block chain after the Scripts block could not be read.");
        }

        var blocks = new Dictionary<SaveBlockKind, SaveBlock> { [scripts.Kind] = scripts };
        var ordered = new List<SaveBlock> { scripts };
        foreach (var block in chain)
        {
            blocks[block.Kind] = block;
            ordered.Add(block);
        }

        var cursor = chain[^1].EndOffset;

        return new SaveLayout(
            spec,
            header,
            simpleVarsOffset,
            spec.SimpleVarsLength,
            metadataLengthOffset,
            metadata,
            blocks,
            ordered,
            gapOffset,
            gap,
            cursor,
            data.Length - cursor);
    }

    /// <summary>
    /// Reads a San Andreas save, where each section is introduced by <c>u32 nameLength</c> and a
    /// NUL-terminated name and runs until the next section. Names are searched for in their known
    /// order, so a stray copy of a name inside a payload cannot be mistaken for a section.
    /// </summary>
    private static SaveLayout ReadNamedBlocks(byte[] data, SaveLayoutSpec spec, SaveHeader header)
    {
        var located = new List<(SaveBlockKind Kind, string Name, int Offset, int DataOffset)>();
        var cursor = header.Length;

        foreach (var (kind, name) in SaveLayoutSpec.SanAndreasBlocks)
        {
            var offset = FindNamedBlock(data, cursor, name)
                ?? throw new SaveFormatException($"The {name} section could not be found in this San Andreas save.");

            // Two unknown u32s sit between the header and the first section; nothing else may.
            if (located.Count == 0 && offset > header.Length + 16)
            {
                throw new SaveFormatException($"Expected the {name} section near 0x{header.Length:X} but found it at 0x{offset:X}.");
            }

            var dataOffset = offset + 4 + name.Length + 1;
            located.Add((kind, name, offset, dataOffset));
            cursor = dataOffset;
        }

        var blocks = new Dictionary<SaveBlockKind, SaveBlock>();
        var ordered = new List<SaveBlock>();
        for (var i = 0; i < located.Count; i++)
        {
            var (kind, name, offset, dataOffset) = located[i];
            var end = i + 1 < located.Count ? located[i + 1].Offset : data.Length;
            var block = new SaveBlock(kind, name, offset, dataOffset, end - dataOffset, end - offset);
            blocks[kind] = block;
            ordered.Add(block);
        }

        var simple = blocks[SaveBlockKind.SimpleVariables];
        var meta = blocks[SaveBlockKind.Metadata];
        if (meta.DataLength < 4)
        {
            throw new SaveFormatException("The METADATA section is too short.");
        }

        var metadataLength = (int)ByteOps.ReadUInt32(data, meta.DataOffset);
        var metadata = CompressedContainer.Read(data, meta.DataOffset + 4);
        if (metadata.Length != metadataLength || metadata.EndOffset > meta.EndOffset)
        {
            throw new SaveFormatException(
                $"The metadata container is {metadata.Length} bytes but the save declares {metadataLength}.");
        }

        return new SaveLayout(
            spec,
            header,
            simple.Offset,
            meta.Offset - simple.Offset,
            meta.DataOffset,
            metadata,
            blocks,
            ordered,
            meta.EndOffset,
            0,
            data.Length,
            0);
    }

    private static int? FindNamedBlock(byte[] data, int start, string name)
    {
        var pattern = new byte[4 + name.Length + 1];
        ByteOps.WriteUInt32(pattern, 0, (uint)(name.Length + 1));
        System.Text.Encoding.ASCII.GetBytes(name, 0, name.Length, pattern, 4);

        var index = data.AsSpan(start).IndexOf(pattern);
        return index < 0 ? null : start + index;
    }

    private static List<SaveBlock> ReadChain(byte[] data, int offset, SaveLayoutSpec spec)
    {
        var list = new List<SaveBlock>(spec.ChainOrder.Count);
        foreach (var kind in spec.ChainOrder)
        {
            var block = ReadSizePrefixedBlock(data, offset, kind, spec);
            list.Add(block);
            offset = block.EndOffset;
        }

        return list;
    }

    /// <summary>Reads a block that starts with its own four-character tag and size.</summary>
    private static SaveBlock ReadTaggedBlock(byte[] data, int offset, SaveBlockKind kind, SaveLayoutSpec spec)
    {
        if (offset + 8 > data.Length)
        {
            throw new SaveFormatException($"The file ends before the {kind} block header.");
        }

        var tag = ReadTag(data, offset)
            ?? throw new SaveFormatException($"Expected a {kind} tag at 0x{offset:X}.");
        ValidateTag(kind, tag, offset, spec);

        var size = (int)ByteOps.ReadUInt32(data, offset + 4);
        if (size < 0 || offset + 8 + size > data.Length)
        {
            throw new SaveFormatException($"The {kind} block at 0x{offset:X} declares an impossible size {size}.");
        }

        return new SaveBlock(kind, tag, offset, offset + 8, size, 8 + size);
    }

    /// <summary>
    /// Reads a block introduced by a uint32 size. Tagged blocks repeat their tag and an inner
    /// size at the start of the payload; those eight bytes are skipped so that
    /// <see cref="SaveBlock.DataOffset"/> always points at real content.
    /// </summary>
    private static SaveBlock ReadSizePrefixedBlock(byte[] data, int offset, SaveBlockKind kind, SaveLayoutSpec spec)
    {
        if (offset + 4 > data.Length)
        {
            throw new SaveFormatException($"The file ends before the {kind} block size.");
        }

        var outerSize = (int)ByteOps.ReadUInt32(data, offset);
        var payloadOffset = offset + 4;
        if (outerSize < 0 || payloadOffset + outerSize > data.Length)
        {
            throw new SaveFormatException(
                $"The {kind} block at 0x{offset:X} declares an impossible size {outerSize}.");
        }

        var dataOffset = payloadOffset;
        var dataLength = outerSize;
        string? tag = null;

        if (spec.ExpectedTags.ContainsKey(kind))
        {
            tag = outerSize >= 8 ? ReadTag(data, payloadOffset) : null;
            ValidateTag(kind, tag, offset, spec);

            // The inner size is advisory and does not always equal outerSize - 8 (RDR, for one,
            // reports four bytes fewer), so it is bounds-checked rather than enforced.
            var innerSize = (int)ByteOps.ReadUInt32(data, payloadOffset + 4);
            if (innerSize < 0 || innerSize > outerSize - 8)
            {
                throw new SaveFormatException(
                    $"The {kind} block at 0x{offset:X} has inner size {innerSize}, " +
                    $"which does not fit in its {outerSize} byte payload.");
            }

            dataOffset += 8;
            dataLength -= 8;
        }

        return new SaveBlock(kind, tag, offset, dataOffset, dataLength, 4 + outerSize);
    }

    private static string? ReadTag(byte[] data, int offset)
    {
        if (offset + 4 > data.Length || data[offset + 3] != 0)
        {
            return null;
        }

        for (var i = 0; i < 3; i++)
        {
            var c = data[offset + i];
            if (c is < (byte)'A' or > (byte)'Z')
            {
                return null;
            }
        }

        return System.Text.Encoding.ASCII.GetString(data, offset, 3);
    }

    private static void ValidateTag(SaveBlockKind kind, string? tag, int offset, SaveLayoutSpec spec)
    {
        if (!spec.ExpectedTags.TryGetValue(kind, out var expected))
        {
            return;
        }

        if (tag != expected)
        {
            throw new SaveFormatException(
                $"Expected the {expected} tag for the {kind} block at 0x{offset:X} but found " +
                $"{(tag is null ? "no tag" : tag)}. The block chain is out of sync.");
        }
    }
}
