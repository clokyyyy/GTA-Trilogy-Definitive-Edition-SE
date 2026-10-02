namespace GtaDe.Definitions;

/// <summary>
/// The content catalogue for GTA III: The Definitive Edition.
/// </summary>
/// <remarks>
/// Every entry refers to a script global by <em>name</em> rather than by offset. The editor
/// resolves those names through the symbol table that ships inside each save file, so the
/// catalogue stays correct even if a game patch shifts the variable space.
/// </remarks>
public static class Gta3Catalogue
{
    public const int TotalStoryMissions = 73;
    public const int TotalHiddenPackages = 100;
    public const int TotalRampages = 20;
    public const int TotalUniqueJumps = 20;

    /// <summary>The story missions, grouped by the contact who hands them out.</summary>
    public static IReadOnlyList<MissionStrand> Strands { get; } =
    [
        new("eightball", "8-Ball", Island.Portland,
        [
            new("Give Me Liberty / 8-Ball", "FLAG_EIGHTBALL_MISSION_PASSED",
                "The intro. Clearing this flag on an advanced save is not recommended.",
                CountsTowardTotal: false),
        ]),

        new("luigi", "Luigi Goterelli", Island.Portland,
        [
            new("Luigi's Girls", "FLAG_LUIGI_MISSION1_PASSED"),
            new("Don't Spank Ma Bitch Up", "FLAG_LUIGI_MISSION2_PASSED"),
            new("Drive Misty For Me", "FLAG_LUIGI_MISSION3_PASSED"),
            new("Pump-Action Pimp", "FLAG_LUIGI_MISSION4_PASSED"),
            new("The Fuzz Ball", "FLAG_LUIGI_MISSION5_PASSED"),
        ], "FLAG_ALL_LUIGI_MISSIONS_PASSED"),

        new("joey", "Joey Leone", Island.Portland,
        [
            new("Mike Lips Last Lunch", "FLAG_JOEY_MISSION1_PASSED"),
            new("Farewell 'Chunky' Lee Chong", "FLAG_JOEY_MISSION2_PASSED"),
            new("Van Heist", "FLAG_JOEY_MISSION3_PASSED"),
            new("Cipriani's Chauffeur", "FLAG_JOEY_MISSION4_PASSED"),
            new("Dead Skunk in the Trunk", "FLAG_JOEY_MISSION5_PASSED"),
            new("The Getaway", "FLAG_JOEY_MISSION6_PASSED"),
        ], "FLAG_ALL_JOEY_MISSIONS_PASSED"),

        new("toni", "Toni Cipriani", Island.Portland,
        [
            new("Taking Out The Laundry", "FLAG_TONI_MISSION1_PASSED"),
            new("The Pick-Up", "FLAG_TONI_MISSION2_PASSED"),
            new("Salvatore's Called a Meeting", "FLAG_TONI_MISSION3_PASSED"),
            new("Triads and Tribulations", "FLAG_TONI_MISSION4_PASSED"),
            new("Blow Fish", "FLAG_TONI_MISSION5_PASSED"),
        ], "FLAG_ALL_TONI_MISSIONS_PASSED"),

        new("salvatore", "Salvatore Leone", Island.Portland,
        [
            new("Chaperone", "FLAG_FRANKIE_MISSION1_PASSED"),
            new("Cutting the Grass", "FLAG_FRANKIE_MISSION2_PASSED"),
            new("Bomb Da Base: Act I", "FLAG_FRANKIE_MISSION2.1_PASSED"),
            new("Bomb Da Base: Act II", "FLAG_FRANKIE_MISSION3_PASSED"),
            new("Last Requests", "FLAG_FRANKIE_MISSION4_PASSED",
                "Completing this moves the story to Staunton Island."),
        ], "FLAG_ALL_FRANKIE_MISSIONS_PASSED"),

        new("elburro", "El Burro", Island.Portland,
        [
            new("Turismo", "FLAG_DIABLO_MISSION1_PASSED"),
            new("I Scream, You Scream", "FLAG_DIABLO_MISSION2_PASSED"),
            new("Trial by Fire", "FLAG_DIABLO_MISSION3_PASSED"),
            new("Big'n'Veiny", "FLAG_DIABLO_MISSION4_PASSED"),
        ], "FLAG_ALL_DIABLO_MISSIONS_PASSED"),

        new("chonks", "Marty Chonks", Island.Portland,
        [
            new("The Crook", "FLAG_MEAT_MISSION1_PASSED"),
            new("The Thieves", "FLAG_MEAT_MISSION2_PASSED"),
            new("The Wife", "FLAG_MEAT_MISSION3_PASSED"),
            new("Her Lover", "FLAG_MEAT_MISSION4_PASSED"),
        ]),

        new("asuka", "Asuka Kasen", Island.StauntonIsland,
        [
            new("Sayonara Salvatore", "FLAG_ASUKA_MISSION1_PASSED"),
            new("Under Surveillance", "FLAG_ASUKA_MISSION2_PASSED"),
            new("Paparazzi Purge", "FLAG_ASUKA_MISSION3_PASSED"),
            new("Payday for Ray", "FLAG_ASUKA_MISSION4_PASSED"),
            new("Two-Faced Tanner", "FLAG_ASUKA_MISSION5_PASSED"),
        ], "FLAG_ALL_ASUKA_MISSIONS_PASSED"),

        new("ray", "Ray Machowski", Island.StauntonIsland,
        [
            new("Silence the Sneak", "FLAG_RAY_MISSION1_PASSED"),
            new("Arms Shortage", "FLAG_RAY_MISSION2_PASSED"),
            new("Evidence Dash", "FLAG_RAY_MISSION3_PASSED"),
            new("Gone Fishing", "FLAG_RAY_MISSION4_PASSED"),
            new("Plaster Blaster", "FLAG_RAY_MISSION5_PASSED"),
            new("Marked Man", "FLAG_RAY_MISSION6_PASSED"),
        ], "FLAG_ALL_RAY_MISSIONS_PASSED"),

        new("kenji", "Kenji Kasen", Island.StauntonIsland,
        [
            new("Kanbu Bust-out", "FLAG_KENJI_MISSION1_PASSED"),
            new("Grand Theft Auto", "FLAG_KENJI_MISSION2_PASSED"),
            new("Deal Steal", "FLAG_KENJI_MISSION3_PASSED"),
            new("Shima", "FLAG_KENJI_MISSION4_PASSED"),
            new("Smack Down", "FLAG_KENJI_MISSION5_PASSED"),
        ], "FLAG_ALL_KENJI_MISSIONS_PASSED"),

        new("love", "Donald Love", Island.StauntonIsland,
        [
            new("Liberator", "FLAG_LOVE_MISSION1_PASSED"),
            new("Waka-Gashira Wipeout!", "FLAG_LOVE_MISSION2_PASSED"),
            new("A Drop in the Ocean", "FLAG_LOVE_MISSION3_PASSED"),
            new("Grand Theft Aero", "FLAG_LOVE_MISSION4_PASSED"),
            new("Escort Service", "FLAG_LOVE_MISSION5_PASSED"),
            new("Decoy", "FLAG_LOVE_MISSION6_PASSED"),
            new("Love's Disappearance", "FLAG_LOVE_MISSION7_PASSED"),
        ], "FLAG_ALL_LOVE_MISSIONS_PASSED"),

        new("yardie", "King Courtney", Island.StauntonIsland,
        [
            new("Bling-Bling Scramble", "FLAG_YARDIE_MISSION1_PASSED"),
            new("Uzi Rider", "FLAG_YARDIE_MISSION2_PASSED"),
            new("Gangcar Round-up", "FLAG_YARDIE_MISSION3_PASSED"),
            new("Kingdom Come", "FLAG_YARDIE_MISSION4_PASSED"),
        ], "FLAG_ALL_YARDIE_MISSIONS_PASSED"),

        new("asuka-shoreside", "Asuka Kasen (Shoreside Vale)", Island.ShoresideVale,
        [
            new("Bait", "FLAG_ASUKA_SUBURBAN_MISSION1_PASSED"),
            new("Espresso-2-Go", "FLAG_ASUKA_SUBURBAN_MISSION2_PASSED"),
            new("S.A.M.", "FLAG_ASUKA_SUBURBAN_MISSION3_PASSED"),
            new("Ransom", "FLAG_ASUKA_SUBURBAN_MISSION4_PASSED"),
        ], "FLAG_ALL_ASUKA_SUBURBAN_MISSIONS_PASSED"),

        new("dice", "D-Ice", Island.ShoresideVale,
        [
            new("Uzi Money", "FLAG_HOOD_MISSION1_PASSED"),
            new("Toyminator", "FLAG_HOOD_MISSION2_PASSED"),
            new("Rigged to Blow", "FLAG_HOOD_MISSION3_PASSED"),
            new("Bullion Run", "FLAG_HOOD_MISSION4_PASSED"),
            new("Rumble", "FLAG_HOOD_MISSION5_PASSED"),
        ], "FLAG_ALL_HOOD_MISSIONS_PASSED"),

        new("catalina", "Catalina", Island.ShoresideVale,
        [
            new("The Exchange", "FLAG_FINAL_MISSION1_PASSED",
                "The final mission. Setting this marks the story complete."),
        ]),
    ];

    /// <summary>
    /// Two extra flags the main script keeps for Catalina's phone missions. They are exposed
    /// separately because their in-game names could not be confirmed from the save data alone.
    /// </summary>
    public static IReadOnlyList<MissionEntry> UnconfirmedScriptMissions { get; } =
    [
        new("Catalina script mission 1", "FLAG_CAT_MISSION1_PASSED",
            "Tracked by the main script. The in-game name could not be confirmed."),
        new("Catalina script mission 2", "FLAG_CAT_MISSION2_PASSED",
            "Tracked by the main script. The in-game name could not be confirmed."),
    ];

    /// <summary>The island-completion flags the game uses to gate progress and bonuses.</summary>
    public static IReadOnlyList<(string Name, string Flag)> IslandCompletionFlags { get; } =
    [
        ("Portland complete", "FLAG_INDUSTRIAL_PASSED"),
        ("Staunton Island complete", "FLAG_COMMERCIAL_PASSED"),
        ("Shoreside Vale complete", "FLAG_SUBURBAN_PASSED"),
    ];

    /// <summary>The twenty Rampages, in the order the script numbers them.</summary>
    public static IReadOnlyList<RampageEntry> Rampages { get; } =
    [
        new(1, Island.Portland, "Hepburn Heights", "Kill 20 Diablos"),
        new(2, Island.Portland, "Portland Harbour", "Kill 25 Triads"),
        new(3, Island.Portland, "Chinatown", "Kill 20 Triads with a pistol"),
        new(4, Island.Portland, "Trenton", "Destroy 10 vehicles"),
        new(5, Island.Portland, "Saint Mark's", "Kill 15 Mafia with an Uzi"),
        new(6, Island.Portland, "Portland Beach", "Kill 20 gang members"),
        new(7, Island.Portland, "Atlantic Quays", "Destroy 8 vehicles with a rocket launcher"),
        new(8, Island.Portland, "Callahan Point", "Kill 30 Mafia"),
        new(9, Island.StauntonIsland, "Belleville Park", "Kill 25 Yakuza"),
        new(10, Island.StauntonIsland, "Liberty Campus", "Kill 20 Yardies"),
        new(11, Island.StauntonIsland, "Newport", "Destroy 12 vehicles"),
        new(12, Island.StauntonIsland, "Torrington", "Kill 25 gang members with an M16"),
        new(13, Island.StauntonIsland, "Aspatria", "Kill 20 Yakuza with a shotgun"),
        new(14, Island.StauntonIsland, "Rockford", "Kill 30 pedestrians"),
        new(15, Island.StauntonIsland, "Fort Staunton", "Destroy 10 vehicles with grenades"),
        new(16, Island.ShoresideVale, "Wichita Gardens", "Kill 25 Hoods"),
        new(17, Island.ShoresideVale, "Pike Creek", "Kill 20 Colombians"),
        new(18, Island.ShoresideVale, "Cedar Grove", "Destroy 15 vehicles"),
        new(19, Island.ShoresideVale, "Cochrane Dam", "Kill 30 Colombians with an M16"),
        new(20, Island.ShoresideVale, "Francis International", "Kill 25 gang members"),
    ];

    /// <summary>The twenty Unique Stunt Jumps, in script order.</summary>
    public static IReadOnlyList<UniqueJumpEntry> UniqueJumps { get; } =
    [
        new(1, Island.Portland, "Callahan Point, under the bridge"),
        new(2, Island.Portland, "Portland Harbour rooftop"),
        new(3, Island.Portland, "Chinatown multistorey"),
        new(4, Island.Portland, "Trenton factory ramp"),
        new(5, Island.Portland, "Atlantic Quays jetty"),
        new(6, Island.Portland, "Saint Mark's alley"),
        new(7, Island.Portland, "Portland Beach ramp"),
        new(8, Island.Portland, "Hepburn Heights rooftop"),
        new(9, Island.StauntonIsland, "Belleville Park subway"),
        new(10, Island.StauntonIsland, "Newport construction site"),
        new(11, Island.StauntonIsland, "Fort Staunton ramp"),
        new(12, Island.StauntonIsland, "Torrington multistorey"),
        new(13, Island.StauntonIsland, "Rockford rooftop"),
        new(14, Island.StauntonIsland, "Liberty Campus"),
        new(15, Island.StauntonIsland, "Aspatria rooftop"),
        new(16, Island.ShoresideVale, "Cochrane Dam"),
        new(17, Island.ShoresideVale, "Francis International Airport"),
        new(18, Island.ShoresideVale, "Pike Creek"),
        new(19, Island.ShoresideVale, "Wichita Gardens"),
        new(20, Island.ShoresideVale, "Cedar Grove"),
    ];

    /// <summary>The four off-road challenges.</summary>
    public static IReadOnlyList<MissionEntry> OffRoadChallenges { get; } =
    [
        new("Patriot Playground", "FLAG_4X4_MISSION1_PASSED"),
        new("A Ride in the Park", "FLAG_4X4_MISSION2_PASSED"),
        new("Gripped!", "FLAG_4X4_MISSION3_PASSED"),
        new("Multistorey Mayhem", "FLAG_MAYHEM_MISSION1_PASSED"),
    ];

    /// <summary>The four RC Toyz challenges.</summary>
    public static IReadOnlyList<MissionEntry> RemoteControlChallenges { get; } =
    [
        new("Diablo Destruction", "FLAG_RC1_PASSED"),
        new("Mafia Massacre", "FLAG_RC2_PASSED"),
        new("Casino Calamity", "FLAG_RC3_PASSED"),
        new("Rumpo Rampage", "FLAG_RC4_PASSED"),
    ];

    /// <summary>The two import/export garages and the vehicles each one wants.</summary>
    public static IReadOnlyList<ImportExportGarage> ImportExportGarages { get; } =
    [
        new("portland", "Portland Harbour Crane", Island.Portland,
            "INDUSTRIAL_SLOT", "INDUSTRIAL_GARAGE_SLOTS_FILLED",
            [
                new(1, "Securicar"), new(2, "Moonbeam"), new(3, "Coach"), new(4, "Flatbed"),
                new(5, "Linerunner"), new(6, "Trashmaster"), new(7, "Patriot"), new(8, "Mr Whoopee"),
                new(9, "Blista"), new(10, "Mule"), new(11, "Yankee"), new(12, "Bobcat"),
                new(13, "Dodo"), new(14, "Bus"), new(15, "Rumpo"), new(16, "Pony"),
            ]),

        new("shoreside", "Shoreside Vale Crane", Island.ShoresideVale,
            "SUBURBAN_SLOT", "SUBURBAN_GARAGE_SLOTS_FILLED",
            [
                new(1, "Sentinel"), new(2, "Cheetah"), new(3, "Banshee"), new(4, "Idaho"),
                new(5, "Infernus"), new(6, "Taxi"), new(7, "Kuruma"), new(8, "Stretch"),
                new(9, "Perennial"), new(10, "Stinger"), new(11, "Manana"), new(12, "Landstalker"),
                new(13, "Stallion"), new(14, "BF Injection"), new(15, "Cabbie"), new(16, "Esperanto"),
            ]),
    ];
}
