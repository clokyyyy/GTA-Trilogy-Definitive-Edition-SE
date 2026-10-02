namespace GtaDe.SaveFormat;

/// <summary>
/// An open GTA San Andreas: The Definitive Edition save slot.
/// </summary>
public sealed class SanAndreasSaveFile : DeSaveFile
{
    private SanAndreasSaveFile(SaveParts parts)
        : base(parts)
    {
        SimpleVars = new SanAndreasSimpleVars(Data, Layout[SaveBlockKind.SimpleVariables]);
        PlayerInfo = new SanAndreasPlayerInfoBlock(Data, Layout[SaveBlockKind.PlayerInfo]);
        Stats = new SanAndreasStatsBlock(Data, Layout[SaveBlockKind.Stats]);
        PlayerPed = new SanAndreasPlayerPed(Data, Layout[SaveBlockKind.PedPool]);
        Tags = new SanAndreasTagsBlock(Data, Layout[SaveBlockKind.Tags]);
    }

    /// <summary>World clock, weather and camera.</summary>
    public SanAndreasSimpleVars SimpleVars { get; }

    /// <summary>Money, maximum health/armour and the reward perks.</summary>
    public SanAndreasPlayerInfoBlock PlayerInfo { get; }

    /// <summary>The stats menu: body, skills, respect, collectibles and more.</summary>
    public SanAndreasStatsBlock Stats { get; }

    /// <summary>CJ's health, armour and weapons.</summary>
    public SanAndreasPlayerPed PlayerPed { get; }

    /// <summary>The hundred gang tags in Los Santos.</summary>
    public SanAndreasTagsBlock Tags { get; }

    public static new SanAndreasSaveFile Load(string path)
    {
        var save = Parse(File.ReadAllBytes(path));
        save.Path = path;
        return save;
    }

    public static new SanAndreasSaveFile Parse(byte[] data) => new(ParseParts(data, GameKind.SanAndreas));
}
