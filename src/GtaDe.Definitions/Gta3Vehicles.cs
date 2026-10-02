namespace GtaDe.Definitions;

/// <summary>GTA III vehicle model indices, as used by cars stored in safehouse garages.</summary>
public static class Gta3Vehicles
{
    private static readonly Dictionary<int, string> Names = new()
    {
        [90] = "Landstalker",
        [91] = "Idaho",
        [92] = "Stinger",
        [93] = "Linerunner",
        [94] = "Perennial",
        [95] = "Sentinel",
        [96] = "Patriot",
        [97] = "Fire Truck",
        [98] = "Trashmaster",
        [99] = "Stretch",
        [100] = "Manana",
        [101] = "Infernus",
        [102] = "Blista",
        [103] = "Pony",
        [104] = "Mule",
        [105] = "Cheetah",
        [106] = "Ambulance",
        [107] = "FBI Car",
        [108] = "Moonbeam",
        [109] = "Esperanto",
        [110] = "Taxi",
        [111] = "Kuruma",
        [112] = "Bobcat",
        [113] = "Mr. Whoopee",
        [114] = "BF Injection",
        [115] = "Corpse Manana",
        [116] = "Police",
        [117] = "Enforcer",
        [118] = "Securicar",
        [119] = "Banshee",
        [120] = "Predator",
        [121] = "Bus",
        [122] = "Rhino",
        [123] = "Barracks OL",
        [124] = "Train",
        [125] = "Police Helicopter",
        [126] = "Dodo",
        [127] = "Coach",
        [128] = "Cabbie",
        [129] = "Stallion",
        [130] = "Rumpo",
        [131] = "RC Bandit",
        [132] = "Bellyup",
        [133] = "Mr. Wongs",
        [134] = "Mafia Sentinel",
        [135] = "Yardie Lobo",
        [136] = "Yakuza Stinger",
        [137] = "Diablo Stallion",
        [138] = "Cartel Cruiser",
        [139] = "Hoods Rumpo XL",
        [140] = "Air Train",
        [141] = "Dead Dodo",
        [142] = "Speeder",
        [143] = "Reefer",
        [144] = "Panlantic",
        [145] = "Flatbed",
        [146] = "Yankee",
        [147] = "Escape",
        [148] = "Borgnine Taxi",
        [149] = "Toyz Van",
        [150] = "Ghost",
    };

    /// <summary>The vehicle's name, or a placeholder when the index is not a known vehicle.</summary>
    public static string Name(int modelIndex) =>
        Names.TryGetValue(modelIndex, out var name) ? name : $"Model {modelIndex}";

    public static bool IsKnown(int modelIndex) => Names.ContainsKey(modelIndex);
}
