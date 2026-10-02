namespace GtaDe.SaveFormat;

/// <summary>
/// An open GTA III: The Definitive Edition save slot.
/// </summary>
public sealed class Gta3SaveFile : DeSaveFile
{
    private Gta3SaveFile(SaveParts parts)
        : base(parts)
    {
        SimpleVars = new SimpleVars(Data, Layout.SimpleVarsOffset);
        PlayerInfo = new PlayerInfoBlock(Data, Layout[SaveBlockKind.PlayerInfo]);
        Stats = new StatsBlock(Data, Layout[SaveBlockKind.Stats]);
        Garages = new GarageBlock(Data, Layout[SaveBlockKind.Garages]);
    }

    /// <summary>World clock, weather, island and camera.</summary>
    public SimpleVars SimpleVars { get; }

    /// <summary>Money, hidden packages and the pickup bonuses.</summary>
    public PlayerInfoBlock PlayerInfo { get; }

    /// <summary>Everything the in-game stats menu shows.</summary>
    public StatsBlock Stats { get; }

    /// <summary>Garage states, safehouse parking and the free bomb/respray flags.</summary>
    public GarageBlock Garages { get; }

    public static new Gta3SaveFile Load(string path)
    {
        var save = Parse(File.ReadAllBytes(path));
        save.Path = path;
        return save;
    }

    public static new Gta3SaveFile Parse(byte[] data) => new(ParseParts(data, GameKind.Gta3));
}
