namespace GtaDe.Definitions;

/// <summary>A single toggle backed by one script global.</summary>
/// <param name="Name">What the game calls it.</param>
/// <param name="Flag">The script global that records it.</param>
/// <param name="Note">Anything the user should know before toggling it.</param>
public sealed record FlagEntry(string Name, string Flag, string? Note = null);

/// <summary>A named group of flags, such as one contact's missions.</summary>
public sealed record FlagSection(string Id, string Name, string? Subtitle, IReadOnlyList<FlagEntry> Entries);

/// <summary>A script counter the editor exposes as a number.</summary>
public sealed record CounterEntry(string Name, string Variable, string? Hint = null);

/// <summary>
/// Everything the Vice City editor knows about, keyed by the script global names embedded in the
/// save's symbol table. Mission scripts were matched against the mission list in the save's
/// metadata (<c>LAWYER1.SC</c>, <c>SERG1.SC</c> …).
/// </summary>
public static class ViceCityCatalogue
{
    private static FlagEntry M(string name, string strand, int n, string? note = null) =>
        new(name, $"FLAG_{strand}_MISSION{n}_PASSED", note);

    public static IReadOnlyList<FlagSection> Strands { get; } =
    [
        new("intro", "Prologue", "Ocean Beach",
        [
            new("In the Beginning...", "FLAG_INTRO_PASSED"),
            new("An Old Friend", "FLAG_HOTEL_MISSION1_PASSED"),
        ]),
        new("lawyer", "Ken Rosenberg", "Ocean Beach",
        [
            M("The Party", "LAWYER", 1),
            M("Back Alley Brawl", "LAWYER", 2),
            M("Jury Fury", "LAWYER", 3),
            M("Riot", "LAWYER", 4),
        ]),
        new("general", "Colonel Cortez", "Viceport",
        [
            M("Treacherous Swine", "GENERAL", 1),
            M("Mall Shootout", "GENERAL", 2),
            M("Guardian Angels", "GENERAL", 3),
            M("Sir, Yes Sir!", "GENERAL", 4),
            M("All Hands On Deck!", "GENERAL", 5),
        ]),
        new("kent", "Lance Vance", "Ocean Beach",
        [
            M("Death Row", "KENT", 1),
        ]),
        new("baron", "Ricardo Diaz", "Starfish Island",
        [
            M("The Chase", "BARON", 1),
            M("Phnom Penh '86", "BARON", 2),
            M("The Fastest Boat", "BARON", 3),
            M("Supply & Demand", "BARON", 4),
            M("Rub Out", "BARON", 5),
        ]),
        new("sergio", "Avery Carrington", "Washington Beach",
        [
            M("Four Iron", "SERGIO", 1),
            M("Demolition Man", "SERGIO", 2),
            M("Two Bit Hit", "SERGIO", 3),
        ]),
        new("bikers", "Mitch Baker", "Downtown",
        [
            M("Alloy Wheels of Steel", "BIKERS", 1),
            M("Messing with the Man", "BIKERS", 2),
            M("Hog Tied", "BIKERS", 3),
        ]),
        new("cuban", "Umberto Robina", "Little Havana",
        [
            M("Stunt Boat Challenge", "CUBAN", 1),
            M("Cannon Fodder", "CUBAN", 2),
            M("Naval Engagement", "CUBAN", 3),
            M("Trojan Voodoo", "CUBAN", 4),
        ]),
        new("haitian", "Auntie Poulet", "Little Haiti",
        [
            M("Juju Scramble", "HAITIAN", 1),
            M("Bombs Away!", "HAITIAN", 2),
            M("Dirty Lickin's", "HAITIAN", 3),
        ]),
        new("rock", "Love Fist", "Downtown",
        [
            M("Love Juice", "ROCK", 1),
            M("Psycho Killer", "ROCK", 2),
            M("Publicity Tour", "ROCK", 3),
        ]),
        new("phil", "Phil Cassidy", "Little Haiti",
        [
            M("Gun Runner", "PHIL", 1),
            M("Boomshine Saigon", "PHIL", 2),
        ]),
        new("assin", "Pay phone assassinations", "Various",
        [
            M("Road Kill", "ASSIN", 1),
            M("Waste the Wife", "ASSIN", 2),
            M("Autocide", "ASSIN", 3),
            M("Check Out at the Check In", "ASSIN", 4),
            M("Loose Ends", "ASSIN", 5),
        ]),
        new("bankjob", "The Malibu Club", "Vice Point",
        [
            M("No Escape?", "BANKJOB", 1),
            M("The Shootist", "BANKJOB", 2),
            M("The Driver", "BANKJOB", 3),
            M("The Job", "BANKJOB", 4),
        ]),
        new("porn", "InterGlobal Films", "Prawn Island",
        [
            M("Recruitment Drive", "PORN", 1),
            M("Dildo Dodo", "PORN", 2),
            M("Martha's Mug Shot", "PORN", 3),
            M("G-spotlight", "PORN", 4),
        ]),
        new("protect", "Vercetti Estate (protection)", "Starfish Island",
        [
            M("Shakedown", "PROTECT", 1),
            M("Bar Brawl", "PROTECT", 2),
            M("Cop Land", "PROTECT", 3),
        ]),
        new("taxiwar", "Kaufman Cabs", "Little Haiti",
        [
            M("V.I.P.", "TAXIWAR", 1),
            M("Friendly Rivalry", "TAXIWAR", 2),
            M("Cabmaggedon", "TAXIWAR", 3),
        ]),
        new("counter", "Print Works", "Little Haiti",
        [
            M("Spilling the Beans", "COUNTER", 1),
            M("Hit the Courier", "COUNTER", 2),
        ]),
        new("finale", "Finale", "Starfish Island",
        [
            M("Keep Your Friends Close...", "FINALE", 1),
            M("Ending", "FINALE", 2, "Set by the script once the finale cut-scene has played."),
        ]),
    ];

    public const int RampageCount = 35;

    public const int UniqueJumpCount = 36;

    public const int PackageCount = 100;

    public static IReadOnlyList<FlagEntry> Rampages { get; } =
        Enumerable.Range(1, RampageCount)
            .Select(n => new FlagEntry($"Rampage {n}", $"RAMPAGE_{n:00}_FLAG"))
            .ToList();

    public static IReadOnlyList<FlagEntry> UniqueJumps { get; } =
        Enumerable.Range(1, UniqueJumpCount)
            .Select(n => new FlagEntry($"Unique jump {n}", $"FLAG_USJ{n}_PASSED"))
            .ToList();

    public static IReadOnlyList<FlagEntry> VehicleJobs { get; } =
    [
        new("Paramedic (level 12)", "DONE_AMBULANCE_PROGRESS", "Completion marker the script sets at level 12; rewards infinite sprint."),
        new("Vigilante (level 12)", "DONE_COPCAR_PROGRESS", "Completion marker set at level 12; rewards +50 armour."),
        new("Firefighter (level 12)", "DONE_FIRETRUCK_PROGRESS", "Completion marker set at level 12; rewards fireproof."),
        new("Pizza Boy", "FLAG_PIZZA_MISSION_PASSED", "Level 10 delivers the health boost."),
    ];

    public static IReadOnlyList<FlagEntry> Races { get; } =
    [
        new("Terminal Velocity", "DONE_RACE1_PROGRESS", "Sunshine Autos street race 1."),
        new("Ocean Drive", "DONE_RACE2_PROGRESS", "Sunshine Autos street race 2."),
        new("Border Run", "DONE_RACE3_PROGRESS", "Sunshine Autos street race 3."),
        new("Capital Cruise", "DONE_RACE4_PROGRESS", "Sunshine Autos street race 4."),
        new("Tour!", "DONE_RACE5_PROGRESS", "Sunshine Autos street race 5."),
        new("V.C. Endurance", "DONE_RACE6_PROGRESS", "Sunshine Autos street race 6."),
        new("Hotring", "DONE_OVALRING_PROGRESS", "Hyman Memorial Stadium."),
        new("Bloodring", "MM_MISSION_PASSED_ONCE", "Hyman Memorial Stadium."),
        new("Dirtring", "FLAG_KICKSTART_PASSED_1STIME", "Hyman Memorial Stadium."),
    ];

    public static IReadOnlyList<FlagEntry> Challenges { get; } =
    [
        new("Downtown Chopper Checkpoint", "DONE_HELI1_PROGRESS"),
        new("Ocean Beach Chopper Checkpoint", "DONE_HELI2_PROGRESS"),
        new("Vice Point Chopper Checkpoint", "DONE_HELI3_PROGRESS"),
        new("Little Haiti Chopper Checkpoint", "DONE_HELI4_PROGRESS"),
        new("RC Raider Pickup", "PLAYERPASSEDRCHELI", "Top Fun van."),
        new("RC Bandit Race", "PLAYERPASSEDRCRACE", "Top Fun van."),
        new("RC Baron Race", "PLAYERPASSEDRCPLANE1", "Top Fun van."),
        new("Trial by Dirt", "FLAG_BMX_1_PASSED"),
        new("Test Track", "FLAG_BMX_2_PASSED"),
        new("PCJ Playground", "FLAG_4X4_MISSION1_PASSED"),
        new("Cone Crazy", "FLAG_CARPARK1_PASSED"),
        new("Shooting range", "FLAG_PASSED_RANGE", "Ammu-Nation, Downtown."),
    ];

    public static IReadOnlyList<FlagEntry> Robberies { get; } =
        Enumerable.Range(1, 12)
            .Select(n => new FlagEntry($"Store {n}", $"ROBBED_SHOP_{n}"))
            .Concat(Enumerable.Range(1, 3).Select(n => new FlagEntry($"Ammu-Nation / hardware store {n}", $"ROBBED_HARDSHOP_{n}")))
            .ToList();

    public static IReadOnlyList<FlagEntry> Assets { get; } =
    [
        new("Boatyard", "BOATYARD_ASSET_ACQUIRED", "Viceport. Asset missions: Checkpoint Charlie."),
        new("Cherry Popper Ice Cream", "ICECREAM_ASSET_ACQUIRED", "Little Haiti. Asset mission: Distribution."),
        new("Pole Position Club", "STRIPCLUB_ASSET_ACQUIRED", "Washington Beach. The retail script leaves this flag off even in 100% saves; the club is owned once bought."),
        new("Kaufman Cabs", "TAXIFIRM_ASSET_ACQUIRED", "Little Haiti."),
        new("Sunshine Autos", "SHOWROOM_ASSET_ACQUIRED", "Little Havana."),
        new("InterGlobal Films", "PORN_ASSET_ACQUIRED", "Prawn Island."),
        new("The Malibu Club", "MALIBU_ASSET_ACQUIRED", "Vice Point."),
    ];

    public static IReadOnlyList<FlagEntry> Safehouses { get; } =
    [
        new("Skumole Shack", "SKUMOLE_BOUGHT", "Little Haiti — $1,000."),
        new("3321 Vice Point", "VICE_POINT_3321_BOUGHT", "Vice Point — $2,500."),
        new("1102 Washington Street", "WASHINGTON_1102_BOUGHT", "Washington Beach — $3,000."),
        new("Links View Apartment", "LINKS_VIEW_BOUGHT", "Leaf Links — $6,000."),
        new("Ocean Heights Apartment", "OCEAN_HEIGHTS_BOUGHT", "Ocean Beach — $7,000."),
        new("El Swanko Casa", "ELSWANKO_BOUGHT", "Vice Point — $8,000."),
        new("Hyman Condo", "HYMAN_CONDO_BOUGHT", "Downtown — $14,000."),
    ];

    public static IReadOnlyList<CounterEntry> Counters { get; } =
    [
        new("Paramedic level", "AMBULANCE_LEVEL", "Level the next Paramedic run starts at."),
        new("Patients saved", "TOTAL_SAVED_PEDS"),
        new("Vigilante level", "COPCAR_LEVEL"),
        new("Criminals killed", "TOTAL_CRIMINALS_KILLED"),
        new("Firefighter level", "FIRETRUCK_LEVEL"),
        new("Fires extinguished", "FIRES_EXTINGUISHED"),
        new("Taxi fares", "TAXI_PASSED", "100 unlocks the jumping taxi."),
        new("Pizzas delivered", "PIZZA_DELIVERED"),
        new("Hidden packages (script)", "NUMBER_OF_PACKAGES_COLLECTED", "Kept in step with the player's package count."),
    ];
}
