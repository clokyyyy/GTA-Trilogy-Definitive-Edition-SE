namespace GtaDe.SaveFormat;

/// <summary>
/// An open GTA Vice City: The Definitive Edition save slot.
/// </summary>
public sealed class ViceCitySaveFile : DeSaveFile
{
    private ViceCitySaveFile(SaveParts parts)
        : base(parts)
    {
        SimpleVars = new ViceCitySimpleVars(Data, Layout.SimpleVarsOffset);
        PlayerInfo = new ViceCityPlayerInfoBlock(Data, Layout[SaveBlockKind.PlayerInfo]);
        Stats = new ViceCityStatsBlock(Data, Layout[SaveBlockKind.Stats]);
        PlayerPed = new ViceCityPlayerPed(Data, Layout[SaveBlockKind.PedPool]);
    }

    /// <summary>World clock, weather and camera.</summary>
    public ViceCitySimpleVars SimpleVars { get; }

    /// <summary>Money, hidden packages, maximum health/armour and the reward perks.</summary>
    public ViceCityPlayerInfoBlock PlayerInfo { get; }

    /// <summary>Everything the in-game stats menu shows.</summary>
    public ViceCityStatsBlock Stats { get; }

    /// <summary>Health, armour and weapons of the player ped.</summary>
    public ViceCityPlayerPed PlayerPed { get; }

    public static new ViceCitySaveFile Load(string path)
    {
        var save = Parse(File.ReadAllBytes(path));
        save.Path = path;
        return save;
    }

    public static new ViceCitySaveFile Parse(byte[] data) => new(ParseParts(data, GameKind.ViceCity));
}
