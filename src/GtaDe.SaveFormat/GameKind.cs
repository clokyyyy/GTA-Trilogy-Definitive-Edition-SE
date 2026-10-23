namespace GtaDe.SaveFormat;

/// <summary>The three games of the Definitive Edition trilogy.</summary>
public enum GameKind
{
    Gta3,
    ViceCity,
    SanAndreas,
}

/// <summary>Identifies the game by validated structure; known revisions are only hints.</summary>
public static class GameDetection
{
    public const uint Gta3Version = 16;
    public const uint ViceCityVersion = 11;
    public const uint SanAndreasVersion = 33;
    public const uint SanAndreasNewVersion = 41;

    public static GameKind? FromVersion(uint version) => version switch
    {
        Gta3Version => GameKind.Gta3,
        ViceCityVersion => GameKind.ViceCity,
        SanAndreasVersion or SanAndreasNewVersion => GameKind.SanAndreas,
        _ => null,
    };

    /// <summary>Validates <paramref name="data"/> and reports its game, or throws if the layout is unverified.</summary>
    public static GameKind Detect(byte[] data)
    {
        return SaveCompatibility.Read(data).Layout.Game;
    }

    public static string DisplayName(this GameKind game) => game switch
    {
        GameKind.Gta3 => "GTA III",
        GameKind.ViceCity => "GTA Vice City",
        GameKind.SanAndreas => "GTA San Andreas",
        _ => game.ToString(),
    };
}
