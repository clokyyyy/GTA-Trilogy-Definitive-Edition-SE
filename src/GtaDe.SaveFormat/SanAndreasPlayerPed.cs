namespace GtaDe.SaveFormat;

/// <summary>San Andreas weapon types, matching <c>eWeaponType</c>.</summary>
public enum SanAndreasWeapon
{
    Unarmed = 0,
    BrassKnuckles = 1,
    GolfClub = 2,
    Nightstick = 3,
    Knife = 4,
    BaseballBat = 5,
    Shovel = 6,
    PoolCue = 7,
    Katana = 8,
    Chainsaw = 9,
    PurpleDildo = 10,
    WhiteDildo = 11,
    Vibrator = 12,
    SilverVibrator = 13,
    Flowers = 14,
    Cane = 15,
    Grenade = 16,
    TearGas = 17,
    Molotov = 18,
    Pistol = 22,
    SilencedPistol = 23,
    DesertEagle = 24,
    Shotgun = 25,
    SawnOff = 26,
    CombatShotgun = 27,
    MicroUzi = 28,
    Mp5 = 29,
    Ak47 = 30,
    M4 = 31,
    Tec9 = 32,
    CountryRifle = 33,
    SniperRifle = 34,
    RocketLauncher = 35,
    HeatSeeker = 36,
    Flamethrower = 37,
    Minigun = 38,
    SatchelCharge = 39,
    Detonator = 40,
    SprayCan = 41,
    FireExtinguisher = 42,
    Camera = 43,
    NightVision = 44,
    ThermalVision = 45,
    Parachute = 46,
}

/// <summary>Static facts about a San Andreas weapon.</summary>
/// <param name="UsesAmmo">False for melee weapons, gifts, goggles and the parachute, which carry a single unit.</param>
public sealed record SanAndreasWeaponInfo(SanAndreasWeapon Weapon, string Name, int Slot, int ClipSize, int DefaultAmmo, bool UsesAmmo);

public static class SanAndreasWeapons
{
    public const int SlotCount = 13;

    public static IReadOnlyList<string> SlotNames { get; } =
    [
        "Fists",
        "Melee",
        "Handgun",
        "Shotgun",
        "Submachine gun",
        "Assault rifle",
        "Rifle",
        "Heavy weapon",
        "Thrown",
        "Special",
        "Gift",
        "Equipment",
        "Detonator",
    ];

    public static IReadOnlyList<SanAndreasWeaponInfo> All { get; } =
    [
        new(SanAndreasWeapon.BrassKnuckles, "Brass knuckles", 0, 0, 1, false),
        new(SanAndreasWeapon.GolfClub, "Golf club", 1, 0, 1, false),
        new(SanAndreasWeapon.Nightstick, "Nightstick", 1, 0, 1, false),
        new(SanAndreasWeapon.Knife, "Knife", 1, 0, 1, false),
        new(SanAndreasWeapon.BaseballBat, "Baseball bat", 1, 0, 1, false),
        new(SanAndreasWeapon.Shovel, "Shovel", 1, 0, 1, false),
        new(SanAndreasWeapon.PoolCue, "Pool cue", 1, 0, 1, false),
        new(SanAndreasWeapon.Katana, "Katana", 1, 0, 1, false),
        new(SanAndreasWeapon.Chainsaw, "Chainsaw", 1, 0, 1, false),
        new(SanAndreasWeapon.Pistol, "9mm pistol", 2, 17, 340, true),
        new(SanAndreasWeapon.SilencedPistol, "Silenced 9mm", 2, 17, 340, true),
        new(SanAndreasWeapon.DesertEagle, "Desert Eagle", 2, 7, 140, true),
        new(SanAndreasWeapon.Shotgun, "Shotgun", 3, 1, 100, true),
        new(SanAndreasWeapon.SawnOff, "Sawn-off shotgun", 3, 2, 100, true),
        new(SanAndreasWeapon.CombatShotgun, "Combat shotgun", 3, 7, 140, true),
        new(SanAndreasWeapon.MicroUzi, "Micro SMG / Uzi", 4, 50, 500, true),
        new(SanAndreasWeapon.Mp5, "MP5", 4, 30, 500, true),
        new(SanAndreasWeapon.Tec9, "Tec-9", 4, 50, 500, true),
        new(SanAndreasWeapon.Ak47, "AK-47", 5, 30, 500, true),
        new(SanAndreasWeapon.M4, "M4", 5, 50, 500, true),
        new(SanAndreasWeapon.CountryRifle, "Country rifle", 6, 1, 100, true),
        new(SanAndreasWeapon.SniperRifle, "Sniper rifle", 6, 1, 100, true),
        new(SanAndreasWeapon.RocketLauncher, "Rocket launcher", 7, 1, 50, true),
        new(SanAndreasWeapon.HeatSeeker, "Heat-seeking RPG", 7, 1, 50, true),
        new(SanAndreasWeapon.Flamethrower, "Flamethrower", 7, 500, 1000, true),
        new(SanAndreasWeapon.Minigun, "Minigun", 7, 500, 1000, true),
        new(SanAndreasWeapon.Grenade, "Grenades", 8, 1, 30, true),
        new(SanAndreasWeapon.TearGas, "Tear gas", 8, 1, 30, true),
        new(SanAndreasWeapon.Molotov, "Molotov cocktails", 8, 1, 30, true),
        new(SanAndreasWeapon.SatchelCharge, "Satchel charges", 8, 1, 30, true),
        new(SanAndreasWeapon.SprayCan, "Spray can", 9, 500, 1500, true),
        new(SanAndreasWeapon.FireExtinguisher, "Fire extinguisher", 9, 500, 1500, true),
        new(SanAndreasWeapon.Camera, "Camera", 9, 36, 72, true),
        new(SanAndreasWeapon.PurpleDildo, "Purple dildo", 10, 0, 1, false),
        new(SanAndreasWeapon.WhiteDildo, "Dildo", 10, 0, 1, false),
        new(SanAndreasWeapon.Vibrator, "Vibrator", 10, 0, 1, false),
        new(SanAndreasWeapon.SilverVibrator, "Silver vibrator", 10, 0, 1, false),
        new(SanAndreasWeapon.Flowers, "Flowers", 10, 0, 1, false),
        new(SanAndreasWeapon.Cane, "Cane", 10, 0, 1, false),
        new(SanAndreasWeapon.NightVision, "Night-vision goggles", 11, 0, 1, false),
        new(SanAndreasWeapon.ThermalVision, "Thermal goggles", 11, 0, 1, false),
        new(SanAndreasWeapon.Parachute, "Parachute", 11, 0, 1, false),
        new(SanAndreasWeapon.Detonator, "Detonator", 12, 0, 1, false),
    ];

    public static SanAndreasWeaponInfo Get(SanAndreasWeapon weapon) =>
        All.FirstOrDefault(w => w.Weapon == weapon)
        ?? throw new ArgumentOutOfRangeException(nameof(weapon), weapon, "Unknown San Andreas weapon.");

    public static SanAndreasWeaponInfo? TryGet(SanAndreasWeapon weapon) => All.FirstOrDefault(w => w.Weapon == weapon);

    public static IEnumerable<SanAndreasWeaponInfo> ForSlot(int slot) => All.Where(w => w.Slot == slot);
}

/// <summary>One weapon slot as stored in the save: a 28-byte <c>CWeapon</c>.</summary>
public sealed class SanAndreasWeaponSlot(byte[] data, int offset, int slot)
{
    public const int Size = 28;

    public int Slot { get; } = slot;

    public int Offset { get; } = offset;

    public SanAndreasWeapon Type => (SanAndreasWeapon)ByteOps.ReadInt32(data, Offset);

    public int AmmoInClip => ByteOps.ReadInt32(data, Offset + 8);

    public int AmmoTotal => ByteOps.ReadInt32(data, Offset + 12);

    public bool IsEmpty => Type == SanAndreasWeapon.Unarmed;

    /// <summary>
    /// Puts a weapon in the slot. Weapons without ammunition carry a single unit, matching what
    /// the game writes; firearms get a full clip drawn from the total.
    /// </summary>
    public void Set(SanAndreasWeapon weapon, int ammo)
    {
        if (weapon == SanAndreasWeapon.Unarmed)
        {
            Clear();
            return;
        }

        var info = SanAndreasWeapons.Get(weapon);
        if (info.Slot != Slot)
        {
            throw new ArgumentException($"{info.Name} belongs in slot {info.Slot}, not slot {Slot}.", nameof(weapon));
        }

        var total = info.UsesAmmo ? Math.Clamp(ammo, 1, 99_999) : 1;
        var clip = info.UsesAmmo ? Math.Min(total, info.ClipSize) : 0;

        ByteOps.WriteInt32(data, Offset, (int)weapon);
        ByteOps.WriteInt32(data, Offset + 4, 0);
        ByteOps.WriteInt32(data, Offset + 8, clip);
        ByteOps.WriteInt32(data, Offset + 12, total);
        ByteOps.WriteInt32(data, Offset + 16, 0);
    }

    public void Clear() => Array.Clear(data, Offset, 20);
}

/// <summary>
/// The player ped inside the San Andreas POOLS section: position, health, armour and the thirteen
/// weapon slots.
/// </summary>
/// <remarks>
/// The DE saves one ped (CJ). Its record begins with the ped count and handle, then position
/// and heading, health and armour as floats at +0x24/+0x28, and thirteen 28-byte <c>CWeapon</c>
/// records from +0x2C. The accessor only enables itself when that shape checks out.
/// </remarks>
public sealed class SanAndreasPlayerPed : BlockAccessor
{
    private const int HealthOffset = 0x24;
    private const int ArmourOffset = 0x28;
    private const int WeaponsOffset = 0x2C;

    public SanAndreasPlayerPed(byte[] data, SaveBlock block)
        : base(data, block)
    {
        IsAvailable = Validate();
        Weapons = IsAvailable
            ? Enumerable.Range(0, SanAndreasWeapons.SlotCount)
                .Select(i => new SanAndreasWeaponSlot(data, block.DataOffset + WeaponsOffset + (i * SanAndreasWeaponSlot.Size), i))
                .ToList()
            : [];
    }

    public bool IsAvailable { get; }

    public IReadOnlyList<SanAndreasWeaponSlot> Weapons { get; }

    public float PositionX => GetFloat(0x14);

    public float PositionY => GetFloat(0x18);

    public float PositionZ => GetFloat(0x1C);

    public float Health
    {
        get => GetFloat(HealthOffset);
        set => SetFloat(HealthOffset, Math.Clamp(value, 1f, 1000f));
    }

    public float Armour
    {
        get => GetFloat(ArmourOffset);
        set => SetFloat(ArmourOffset, Math.Clamp(value, 0f, 1000f));
    }

    private bool Validate()
    {
        if (Block.DataLength < WeaponsOffset + (SanAndreasWeapons.SlotCount * SanAndreasWeaponSlot.Size) || GetInt(0) != 1)
        {
            return false;
        }

        var health = GetFloat(HealthOffset);
        var armour = GetFloat(ArmourOffset);
        if (!float.IsFinite(health) || health is < 0 or > 1000 || !float.IsFinite(armour) || armour is < 0 or > 1000)
        {
            return false;
        }

        for (var i = 0; i < SanAndreasWeapons.SlotCount; i++)
        {
            var type = (SanAndreasWeapon)GetInt(WeaponsOffset + (i * SanAndreasWeaponSlot.Size));
            var ammo = GetInt(WeaponsOffset + (i * SanAndreasWeaponSlot.Size) + 12);
            if (type != SanAndreasWeapon.Unarmed && SanAndreasWeapons.TryGet(type)?.Slot != i)
            {
                return false;
            }

            if (ammo < 0)
            {
                return false;
            }
        }

        return true;
    }
}
