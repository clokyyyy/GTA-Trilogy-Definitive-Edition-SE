namespace GtaDe.Definitions;

/// <summary>
/// A San Andreas toggle backed by one script global, or one element of a global array.
/// </summary>
/// <param name="Name">What the game calls it.</param>
/// <param name="Variable">The script global (optionally scope-qualified, e.g. <c>GYMLS.GYM_LS_DEFEATED</c>).</param>
/// <param name="Note">Anything the user should know before toggling it.</param>
/// <param name="Element">Array element, for globals such as <c>ALREADY_BOUGHT_HOUSE</c>.</param>
/// <param name="OnValue">
/// The value the game writes when it is done. The toggle reads as set when the global is at least
/// this value, and turning it on never lowers a larger value the game already wrote.
/// </param>
public sealed record SanAndreasFlag(string Name, string Variable, string? Note = null, int Element = 0, int OnValue = 1);

/// <summary>A story mission complete once its strand counter has moved past <paramref name="Launch"/>.</summary>
/// <param name="Launch">The counter value the strand's launcher script starts this mission at.</param>
public sealed record StrandMission(string Name, int Launch, string? Note = null);

/// <summary>One contact's story strand, driven by a single <c>FLAG_*_MISSION_COUNTER</c>.</summary>
public sealed record StoryStrand(string Id, string Name, string Region, string Counter, IReadOnlyList<StrandMission> Missions)
{
    /// <summary>The counter value once every mission in the strand is passed.</summary>
    public int CompleteValue => Missions.Count == 0 ? 0 : Missions.Max(m => m.Launch) + 1;
}

/// <summary>A San Andreas stat shown as an editable number.</summary>
public sealed record StatEntry(int Id, string Name, double Max, string? Hint = null);

/// <summary>A named group of San Andreas toggles.</summary>
public sealed record SanAndreasSection(string Id, string Name, string Description, IReadOnlyList<SanAndreasFlag> Entries, IReadOnlyList<int>? Stats = null);

/// <summary>
/// Everything the San Andreas editor knows about, keyed by the script global names in the
/// save's symbol table. Launch values come from the decompiled launcher scripts (<c>SWEET</c>,
/// <c>SYND</c>, <c>DESERT</c> …), which start each mission when the strand counter equals it.
/// </summary>
public static class SanAndreasCatalogue
{
    private static IReadOnlyList<StrandMission> Seq(params string[] names) =>
        names.Select((name, i) => new StrandMission(name, i)).ToList();

    public static IReadOnlyList<StoryStrand> Strands { get; } =
    [
        new("intro", "Introduction", "Los Santos", "FLAG_INTRO_MISSION_COUNTER", Seq("Big Smoke", "Ryder")),
        new("sweet", "Sweet", "Los Santos", "FLAG_SWEET_MISSION_COUNTER",
            Seq("Tagging Up Turf", "Cleaning the Hood", "Drive-Thru", "Nines and AK's", "Drive-By",
                "Sweet's Girl", "Cesar Vialpando", "Doberman", "Los Sepulcros")),
        new("ryder", "Ryder", "Los Santos", "FLAG_RYDER_MISSION_COUNTER",
            Seq("Home Invasion", "Catalyst", "Robbing Uncle Sam")),
        new("smoke", "Big Smoke", "Los Santos", "FLAG_SMOKE_MISSION_COUNTER",
            Seq("OG Loc", "Running Dog", "Wrong Side of the Tracks", "Just Business")),
        new("strap", "OG Loc", "Los Santos", "FLAG_STRAP_MISSION_COUNTER",
        [
            new("Life's a Beach", 0),
            new("Madd Dogg's Rhymes", 1),
            new("Management Issues", 2),
            new("House Party", 4, "Two-part mission; complete once both nights are done."),
        ]),
        new("crash", "C.R.A.S.H.", "Los Santos", "FLAG_CRASH_MISSION_COUNTER",
            Seq("Burning Desire", "Gray Imports")),
        new("cesar", "Cesar Vialpando", "Los Santos", "FLAG_CESAR_MISSION_COUNTER", Seq("High Stakes Lowrider")),
        new("la1fin", "Los Santos finale", "Los Santos", "FLAG_LA1FIN1_MISSION_COUNTER",
            Seq("Reuniting the Families", "The Green Sabre")),
        new("bcrash", "Badlands", "Red County", "FLAG_BCRASH_MISSION_COUNTER", Seq("Badlands")),
        new("truth", "The Truth", "Red County", "FLAG_TRUTH_MISSION_COUNTER",
            Seq("Body Harvest", "Are You Going to San Fierro?")),
        new("bcesar", "Cesar (Badlands)", "Red County", "FLAG_BCESAR_MISSION_COUNTER",
        [
            new("Wu Zi Mu", 1, "The first street race in the Badlands."),
            new("Farewell, My Love...", 9, "Setting this completes the Badlands race strand."),
        ]),
        new("garage", "Garage", "San Fierro", "FLAG_GARAGE_MISSION_COUNTER",
            Seq("Wear Flowers in Your Hair", "Deconstruction")),
        new("scrash", "C.R.A.S.H. (San Fierro)", "San Fierro", "FLAG_SCRASH_MISSION_COUNTER",
            Seq("555 We Tip", "Snail Trail")),
        new("wuzi", "Woozie", "San Fierro", "FLAG_WUZI_MISSION_COUNTER",
            Seq("Mountain Cloud Boys", "Ran Fa Li", "Lure", "Amphibious Assault", "The Da Nang Thang")),
        new("steal", "Wang Cars", "San Fierro", "FLAG_STEAL_MISSION_COUNTER",
            Seq("Zeroing In", "Test Drive", "Customs Fast Track", "Puncture Wounds")),
        new("zero", "Zero", "San Fierro", "FLAG_ZERO_MISSION_COUNTER",
            Seq("Air Raid", "Supply Lines...", "New Model Army")),
        new("synd", "Syndicate", "San Fierro", "FLAG_SYND_MISSION_COUNTER",
        [
            new("Photo Opportunity", 0),
            new("Jizzy", 2, "Includes the Jizzy's club cut-scene step."),
            new("T-Bone Mendez", 3),
            new("Mike Toreno", 4),
            new("Outrider", 5),
            new("Ice Cold Killa", 6),
            new("Pier 69", 7),
            new("Toreno's Last Flight", 8),
            new("Yay Ka-Boom-Boom", 9),
        ]),
        new("desert", "Toreno and the airstrip", "Bone County", "FLAG_DESERT_MISSION_COUNTER",
            Seq("Monster", "Highjack", "Interdiction", "Verdant Meadows", "Learning to Fly",
                "N.O.E.", "Stowaway", "Black Project", "Green Goo")),
        new("casino", "Caligula's and the Four Dragons", "Las Venturas", "FLAG_CASINO_MISSION_COUNTER",
            Seq("Fender Ketchup", "Explosive Situation", "You've Had Your Chips", "Don Peyote",
                "Intensive Care", "The Meat Business", "Fish in a Barrel", "Freefall", "Saint Mark's Bistro")),
        new("vcrash", "C.R.A.S.H. (Las Venturas)", "Las Venturas", "FLAG_VCRASH_MISSION_COUNTER",
            Seq("Misappropriation", "High Noon")),
        new("doc", "Madd Dogg", "Las Venturas", "FLAG_DOC_MISSION_COUNTER", Seq("Madd Dogg")),
        new("heist", "Caligula's heist", "Las Venturas", "FLAG_HEIST_MISSION_COUNTER",
            Seq("Architectural Espionage", "Key to Her Heart", "Dam and Blast", "Cop Wheels",
                "Up, Up and Away!", "Breaking the Bank at Caligula's")),
        new("mansion", "Madd Dogg's mansion", "Los Santos", "FLAG_MANSION_MISSION_COUNTER",
            Seq("A Home in the Hills", "Vertical Bird", "Home Coming", "Cut Throat Business")),
        new("grove", "Grove Street", "Los Santos", "FLAG_GROVE_MISSION_COUNTER",
            Seq("Beat Down on B Dup", "Grove 4 Life")),
        new("riot", "Riot", "Los Santos", "FLAG_RIOT_MISSION_COUNTER",
        [
            new("Riot", 0),
            new("Los Desperados", 1),
            new("End of the Line", 4, "The three-part finale; completing it finishes the story."),
        ]),
    ];

    /// <summary>Catalina's four robberies, which raise <c>CAT_COUNTER</c> as each is passed.</summary>
    public static IReadOnlyList<SanAndreasFlag> CatalinaRobberies { get; } =
    [
        new("Local Liquor Store", "FLAG_CAT_MISSION1_PASSED"),
        new("Small Town Bank", "FLAG_CAT_MISSION2_PASSED"),
        new("Tanker Commander", "FLAG_CAT_MISSION3_PASSED"),
        new("Against All Odds", "FLAG_CAT_MISSION4_PASSED"),
    ];

    /// <summary>The King in Exile cut-scene mission: the trailer trigger reaches 2 once it has played.</summary>
    public static SanAndreasFlag KingInExile { get; } =
        new("King in Exile", "FLAG_TRIGGER_TRAILOR_CUT", "Plays once every robbery is done and Body Harvest is passed.", OnValue: 2);

    public const string CatalinaCounter = "CAT_COUNTER";

    public static IReadOnlyList<SanAndreasSection> ActivityTabs { get; } =
    [
        new("jobs", "Vehicle jobs",
            "The vehicle sub-missions. Each flag is the one the 100% tally reads; the job levels live in the stats.",
            [
                new("Taxi Driver", "DONE_TAXIODD_PROGRESS", "Fifty fares; also see the fare counter."),
                new("Paramedic", "DONE_AMBULANCE_PROGRESS", "Level 12 reward: maximum health and infinite sprint."),
                new("Vigilante", "DONE_COPCAR_PROGRESS", "Level 12 reward: maximum armour raised to 150."),
                new("Firefighter", "DONE_FIRETRUCK_PROGRESS", "Level 12 reward: fireproof."),
                new("Burglary", "DONE_BURGLARY_PROGRESS", "Not needed for 100%."),
                new("Pimping", "PIMP_PASSED_ONCE"),
                new("Trucking", "DONE_TRUCK_PROGRESS"),
                new("Quarry", "DONE_QUARRY_PROGRESS"),
                new("Freight train", "FREIGHT.FLAG_FREIGHT_PASSED_1STIME"),
                new("Valet parking", "VALET_MISSION_COMPLETED"),
                new("Courier: Los Santos (Roboi's)", "COURIERLA_PASSED_ONCE"),
                new("Courier: San Fierro (Hippy Shopper)", "COURIERSF_PASSED_ONCE"),
                new("Courier: Las Venturas (Burger Shot)", "COURIERLV_PASSED_ONCE"),
            ],
            [157, 158, 159, 161, 175]),
        new("schools", "Schools",
            "Driving, flight, bike and boat school. Passing each school unlocks its vehicle generators; the medal flags drive the reward cars.",
            [
                new("Driving school passed", "DRIVING_TEST_PASSED"),
                new("Pilot school passed", "PILOT_TEST_PASSED"),
                new("Pilot licence obtained", "D5_PILOT_LICENCE_OBTAINED"),
                new("Pilot school bronze reward", "D5_BRONZE_GENERATOR_UNLOCKED"),
                new("Pilot school silver reward", "D5_SILVER_GENERATOR_UNLOCKED"),
                new("Pilot school gold reward", "D5_GOLD_GENERATOR_UNLOCKED"),
                new("Bike school passed", "BS_PASSED"),
                new("Bike school passed (first time)", "FLAG_BIKESCHOOL_PASSED_1STIME"),
                .. Medals("Bike school", "BS", ["THE360", "THE180", "WHEELIE", "JUMPSTOP", "STOPPIE", "JUMPSTOPPIE"],
                    ["The 360", "The 180", "Wheelie", "Jump & Stop", "Stoppie", "Jump & Stoppie"]),
                new("Boat school passed", "BOAT_PASSED_ONCE"),
                .. Medals("Boat school", "BOAT", ["ACCELERATE", "SIMPLECIRCUIT", "SLALOM", "SKIJUMP", "HOVER"],
                    ["Basic Seamanship", "Plot a Course", "Fat Slalom", "Jump & Speed", "Land, Sea and Air"]),
            ]),
        new("races", "Races and challenges",
            "Street races, stunt challenges and the other one-off challenges that count toward 100%.",
            [
                new("Race tournament complete", "GOT_RACE_MISSION_COMPLETE"),
                new("BMX challenge", "DONE_BMX_STUNT_PROGRESS"),
                new("NRG-500 challenge", "DONE_NRG500_STUNT_PROGRESS"),
                new("Kickstart", "FLAG_KICKSTART_PASSED_1STIME"),
                new("Blood Bowl", "BLOOD_PASSED_ONCE"),
                new("Chiliad Challenge", "FLAG_MTBIKE_PASSED_1STIME"),
                new("Triathlons", "FLAG_TRIATHALON_PASSED_1STIME"),
                new("Dirt-bike / shooting challenge", "FLAG_SHTR_PASSED_1STIME"),
                new("Lowrider challenge unlocked", "LOWRIDER_MINIGAME_UNLOCKED"),
                new("Export list complete", "IMPEXP.IMPEXP_IS_COMPLETE", "The Easter Basin export lists."),
                new("Ammu-Nation challenge: round 1", "SH_RANGE.SR_RANGE_LEVEL", "Shooting range level, 4 when finished.", 0, 4),
                new("Ammu-Nation challenge: round 2", "SH_RANGE.SR_RANGE_LEVEL", null, 1, 4),
                new("Ammu-Nation challenge: round 3", "SH_RANGE.SR_RANGE_LEVEL", null, 2, 4),
            ],
            [144, 145, 168]),
        new("gyms", "Gyms and misc",
            "Gym fighting styles, minigame unlocks and the story keys a few side activities check.",
            [
                new("Ganton gym: boxing learned", "GYMLS.GYM_LS_DEFEATED"),
                new("Cobra gym: kung fu learned", "GYMSF.GYM_SF_DEFEATED"),
                new("Below the Belt gym: kickboxing learned", "GYMLV.GYM_LV_DEFEATED"),
                new("Basketball unlocked", "BBALL_UNLOCKED"),
                new("Valet parking unlocked", "VALET_UNLOCKED"),
                new("Millie's keycard acquired", "KEYCARD_AQUIRED_FROM_MILLIE"),
                new("Zero's RC shop bought", "ZEROS_PROPERTY_BOUGHT"),
                new("Game complete", "GAME_COMPLETE", "Set by End of the Line; unlocks the post-story phone calls."),
            ]),
        new("wardrobe", "Wardrobe and shops",
            "Girlfriend and job outfits plus the clothes-shop visits. The outfits themselves are given by the game the next time it checks these flags.",
            [
                new("Valet uniform", "FLAG_PLAYER_GOT_VALET_UNIFORM"),
                new("Croupier uniform", "FLAG_PLAYER_GOT_CASINO_UNIFORM"),
                new("Gimp suit", "FLAG_PLAYER_GOT_GIMP_SUIT"),
                new("Police uniform (Barbara)", "FLAG_PLAYER_GOT_POLICE_UNIFORM"),
                new("Rural clothes (Katie / Helena)", "FLAG_PLAYER_GOT_COUNTRY_CLOTHES"),
                new("Mechanic overalls (Michelle)", "FLAG_GOT_MECHANIC_CLOTHES"),
                new("Medic uniform (Katie)", "FLAG_GOT_MEDIC_CLOTHES"),
                new("Pimp suit", "FLAG_GOT_PIMP_CLOTHES"),
                new("Shopped at Binco", "FLAG_BOUGHT_FROM_BINCO"),
                new("Shopped at ProLaps", "FLAG_BOUGHT_FROM_PROLAPSE"),
                new("Shopped at SubUrban", "FLAG_BOUGHT_FROM_SUBURBAN"),
                new("Shopped at Zip", "FLAG_BOUGHT_FROM_ZIP"),
                new("Shopped at Victim", "FLAG_BOUGHT_FROM_VICTIM"),
                new("Shopped at Didier Sachs", "FLAG_BOUGHT_FROM_DIDIESSACHS"),
            ]),
    ];

    private static IEnumerable<SanAndreasFlag> Medals(string school, string prefix, string[] keys, string[] names)
    {
        for (var i = 0; i < keys.Length; i++)
        {
            yield return new($"{school}: {names[i]} gold", $"{prefix}_{keys[i]}_GOLDACHIEVED");
        }
    }

    public const string HouseArray = "ALREADY_BOUGHT_HOUSE";

    private static SanAndreasFlag House(int index, string name, string? note = null) => new(name, HouseArray, note, index);

    /// <summary>Buyable safehouses by region, indexed into <c>ALREADY_BOUGHT_HOUSE</c>. Names come from each save point's position.</summary>
    public static IReadOnlyList<SanAndreasSection> SafehouseRegions { get; } =
    [
        new("ls", "Los Santos", "The city where the story starts.",
        [
            House(3, "Santa Maria Beach", "$30,000"),
            House(26, "Jefferson motel", "$10,000"),
            House(12, "Mulholland", "$120,000"),
            House(10, "El Corona", "$10,000"),
            House(15, "Verona Beach", "$10,000"),
            House(30, "Willowfield", "$10,000"),
        ]),
        new("countryside", "Countryside", "Red County, Flint County and Whetstone. These go on sale once the countryside opens up.",
        [
            House(25, "Dillimore", "$40,000"),
            House(8, "Palomino Creek", "$35,000"),
            House(31, "Blueberry", "$10,000"),
            House(22, "Angel Pine", "$20,000"),
            House(19, "Whetstone", "$100,000"),
        ]),
        new("sf", "San Fierro", "Available after the move to San Fierro.",
        [
            House(18, "Chinatown", "$20,000"),
            House(20, "Doherty", "$20,000"),
            House(13, "Paradiso", "$20,000"),
            House(14, "Hashbury", "$40,000"),
            House(11, "Calton Heights", "$100,000"),
            House(21, "Queens", "$50,000"),
        ]),
        new("desert", "Desert", "Bone County and Tierra Robada.",
        [
            House(5, "Fort Carson", "$30,000"),
            House(23, "El Quebrados", "$20,000"),
            House(24, "Las Barrancas", "$20,000"),
        ]),
        new("lv", "Las Venturas", "Houses and the casino hotel suites.",
        [
            House(9, "Redsands West", "$30,000"),
            House(6, "Prickle Pine", "$50,000"),
            House(7, "Whitewood Estates", "$30,000"),
            House(4, "Rockshore West", "$20,000"),
            House(29, "Creek", "$10,000"),
            House(16, "Camel's Toe suite", "$6,000"),
            House(17, "Come-A-Lot suite", "$6,000"),
            House(27, "Old Venturas Strip suite", "$6,000"),
            House(28, "Clown's Pocket suite", "$6,000"),
        ]),
    ];

    public static IReadOnlyList<SanAndreasFlag> Safehouses { get; } = SafehouseRegions.SelectMany(r => r.Entries).ToList();


    /// <summary>Stats that track the 100% collectibles, with their totals.</summary>
    public static IReadOnlyList<(string Name, int Stat, int TotalStat, int Total, string Note)> Collectibles { get; } =
    [
        ("Snapshots", 231, 232, 50, "San Fierro. Reward: weapons at the Doherty safehouse."),
        ("Horseshoes", 241, 242, 50, "Las Venturas. Reward: luck maxed and weapons at the Four Dragons."),
        ("Oysters", 243, 244, 50, "Under water across the state. Reward: lung capacity and sex appeal maxed."),
    ];

    public const int TagStat = 322;

    public static IReadOnlyList<StatEntry> BodyStats { get; } =
    [
        new(21, "Fat", 1000),
        new(22, "Stamina", 1000),
        new(23, "Muscle", 1000),
        new(24, "Max health stat", 1000, "Raises the health bar; 1000 is the cap."),
        new(25, "Sex appeal", 1000),
        new(225, "Lung capacity", 1000),
        new(233, "Luck", 1000),
    ];

    public static IReadOnlyList<StatEntry> DrivingStats { get; } =
    [
        new(160, "Driving skill", 1000),
        new(223, "Flying skill", 1000),
        new(229, "Bike skill", 1000),
        new(230, "Cycling skill", 1000),
    ];

    public static IReadOnlyList<StatEntry> WeaponSkills { get; } =
    [
        new(69, "Pistol", 1000, "Hitman at 999: dual-wield."),
        new(70, "Silenced pistol", 1000),
        new(71, "Desert Eagle", 1000),
        new(72, "Shotgun", 1000),
        new(73, "Sawn-off shotgun", 1000, "Hitman at 999: dual-wield."),
        new(74, "Combat shotgun", 1000),
        new(75, "Micro SMG", 1000, "Hitman at 999: dual-wield."),
        new(76, "SMG", 1000),
        new(77, "AK-47", 1000),
        new(78, "M4", 1000),
        new(79, "Rifle", 1000),
    ];

    public static IReadOnlyList<StatEntry> RespectStats { get; } =
    [
        new(64, "Total respect", 1000),
        new(65, "Girlfriend respect", 1000),
        new(66, "Clothes respect", 1000),
        new(67, "Fitness respect", 1000),
        new(68, "Respect", 1000, "Mission respect; the game adds the other sources on top."),
    ];

    public static IReadOnlyList<StatEntry> GirlfriendStats { get; } =
    [
        new(252, "Denise", 100, "50% gives the pimp suit, 100% unlocks her car."),
        new(253, "Michelle", 100),
        new(254, "Helena", 100),
        new(255, "Barbara", 100),
        new(256, "Katie", 100),
        new(257, "Millie", 100),
    ];

    public static IReadOnlyList<StatEntry> ProgressStats { get; } =
    [
        new(0, "Progress made", 1000, "Out of the total below; this is what the completion percentage reads."),
        new(1, "Total progress", 1000),
        new(147, "Missions passed", 1000),
        new(148, "Total missions", 1000),
        new(181, "Cities unlocked", 4, "0 = Los Santos only, 4 = the whole state."),
        new(134, "Days passed", 100000),
        new(234, "Territories taken over", 1000),
        new(236, "Territories held", 1000),
        new(237, "Most territories held", 1000),
    ];
}
