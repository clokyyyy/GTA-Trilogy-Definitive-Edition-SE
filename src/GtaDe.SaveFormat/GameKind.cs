namespace GtaDe.SaveFormat;

/// <summary>The three games of the Definitive Edition trilogy.</summary>
public enum GameKind
{
    Gta3,
    ViceCity,
    SanAndreas,
}

/// <summary>Identifies which game wrote a save from the version number in its header.</summary>
public static class GameDetection
{
    public const uint Gta3Version = 16;
    public const uint ViceCityVersion = 11;
    public const uint SanAndreasVersion = 33;

    public static GameKind? FromVersion(uint version) => version switch
    {
        Gta3Version => GameKind.Gta3,
        ViceCityVersion => GameKind.ViceCity,
        SanAndreasVersion => GameKind.SanAndreas,
        _ => null,
    };

    /// <summary>Reads the header of <paramref name="data"/> and reports the game, or throws if unknown.</summary>
    public static GameKind Detect(byte[] data)
    {
        var header = SaveHeader.Read(data);
        return FromVersion(header.Version)
            ?? throw new SaveFormatException(
                $"Save format version {header.Version} is not a GTA III, Vice City or San Andreas Definitive Edition save.");
    }

    public static string DisplayName(this GameKind game) => game switch
    {
        GameKind.Gta3 => "GTA III",
        GameKind.ViceCity => "GTA Vice City",
        GameKind.SanAndreas => "GTA San Andreas",
        _ => game.ToString(),
    };
}
