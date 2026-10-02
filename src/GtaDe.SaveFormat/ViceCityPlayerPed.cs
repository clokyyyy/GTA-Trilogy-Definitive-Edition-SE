namespace GtaDe.SaveFormat;

/// <summary>Vice City weapon types, matching <c>eWeaponType</c>.</summary>
public enum ViceCityWeapon
{
    Unarmed = 0,
    BrassKnuckles = 1,
    Screwdriver = 2,
    GolfClub = 3,
    Nightstick = 4,
    Knife = 5,
    BaseballBat = 6,
    Hammer = 7,
    Cleaver = 8,
    Machete = 9,
    Katana = 10,
    Chainsaw = 11,
    Grenade = 12,
    RemoteGrenade = 13,
    TearGas = 14,
    Molotov = 15,
    Pistol = 17,
    Python = 18,
    Shotgun = 19,
    Spas12 = 20,
    StubbyShotgun = 21,
    Tec9 = 22,
    Uzi = 23,
    SilencedIngram = 24,
    Mp5 = 25,
    M4 = 26,
    Ruger = 27,
    SniperRifle = 28,
    LaserScope = 29,
    RocketLauncher = 30,
    Flamethrower = 31,
    M60 = 32,
    Minigun = 33,
    Camera = 36,
}

/// <summary>One weapon slot as stored in the save: a 24-byte <c>CWeapon</c>.</summary>
public sealed class ViceCityWeaponSlot(byte[] data, int offset, int slot)
{
    public const int Size = 24;

    public int Slot { get; } = slot;

    public int Offset { get; } = offset;

    public ViceCityWeapon Type => (ViceCityWeapon)ByteOps.ReadInt32(data, Offset);

    public int AmmoInClip => ByteOps.ReadInt32(data, Offset + 8);

    public int AmmoTotal => ByteOps.ReadInt32(data, Offset + 12);

    public bool IsEmpty => Type == ViceCityWeapon.Unarmed && AmmoTotal == 0;

    /// <summary>
    /// Puts a weapon in the slot with the given total ammo. Melee weapons always carry one round,
    /// matching what the game writes; firearms get a full clip drawn from the total.
    /// </summary>
    public void Set(ViceCityWeapon weapon, int ammo)
    {
        if (weapon == ViceCityWeapon.Unarmed)
        {
            Clear();
            return;
        }

        var info = ViceCityWeapons.Get(weapon);
        if (info.Slot != Slot)
        {
            throw new ArgumentException($"{info.Name} belongs in slot {info.Slot}, not slot {Slot}.", nameof(weapon));
        }

        var total = info.IsMelee ? 1 : Math.Clamp(ammo, 1, 99_999);
        var clip = info.IsMelee ? 1 : Math.Min(total, info.ClipSize);

        ByteOps.WriteInt32(data, Offset, (int)weapon);
        ByteOps.WriteInt32(data, Offset + 4, 0);
        ByteOps.WriteInt32(data, Offset + 8, clip);
        ByteOps.WriteInt32(data, Offset + 12, total);
        ByteOps.WriteInt32(data, Offset + 16, 0);
    }

    public void Clear() => Array.Clear(data, Offset, 20);
}

/// <summary>Static facts about a Vice City weapon.</summary>
public sealed record ViceCityWeaponInfo(ViceCityWeapon Weapon, string Name, int Slot, int ClipSize, int DefaultAmmo)
{
    public bool IsMelee => Slot is 0 or 1;
}

public static class ViceCityWeapons
{
    public const int SlotCount = 10;

    public static IReadOnlyList<string> SlotNames { get; } =
    [
        "Fists",
        "Melee",
        "Thrown",
        "Pistol",
        "Shotgun",
        "Submachine gun",
        "Assault rifle",
        "Heavy weapon",
        "Sniper rifle",
        "Camera",
    ];

    public static IReadOnlyList<ViceCityWeaponInfo> All { get; } =
    [
        new(ViceCityWeapon.BrassKnuckles, "Brass knuckles", 0, 1, 1),
        new(ViceCityWeapon.Screwdriver, "Screwdriver", 1, 1, 1),
        new(ViceCityWeapon.GolfClub, "Golf club", 1, 1, 1),
        new(ViceCityWeapon.Nightstick, "Nightstick", 1, 1, 1),
        new(ViceCityWeapon.Knife, "Knife", 1, 1, 1),
        new(ViceCityWeapon.BaseballBat, "Baseball bat", 1, 1, 1),
        new(ViceCityWeapon.Hammer, "Hammer", 1, 1, 1),
        new(ViceCityWeapon.Cleaver, "Meat cleaver", 1, 1, 1),
        new(ViceCityWeapon.Machete, "Machete", 1, 1, 1),
        new(ViceCityWeapon.Katana, "Katana", 1, 1, 1),
        new(ViceCityWeapon.Chainsaw, "Chainsaw", 1, 1, 1),
        new(ViceCityWeapon.Grenade, "Grenades", 2, 1, 30),
        new(ViceCityWeapon.RemoteGrenade, "Remote grenades", 2, 1, 30),
        new(ViceCityWeapon.TearGas, "Tear gas", 2, 1, 30),
        new(ViceCityWeapon.Molotov, "Molotov cocktails", 2, 1, 30),
        new(ViceCityWeapon.Pistol, "Pistol", 3, 17, 340),
        new(ViceCityWeapon.Python, "Colt Python", 3, 6, 240),
        new(ViceCityWeapon.Shotgun, "Shotgun", 4, 1, 150),
        new(ViceCityWeapon.Spas12, "SPAS-12", 4, 7, 150),
        new(ViceCityWeapon.StubbyShotgun, "Stubby shotgun", 4, 1, 150),
        new(ViceCityWeapon.Tec9, "Tec-9", 5, 50, 500),
        new(ViceCityWeapon.Uzi, "Uzi", 5, 30, 500),
        new(ViceCityWeapon.SilencedIngram, "Silenced Ingram", 5, 30, 500),
        new(ViceCityWeapon.Mp5, "MP5", 5, 30, 500),
        new(ViceCityWeapon.M4, "M4", 6, 30, 500),
        new(ViceCityWeapon.Ruger, "Ruger", 6, 30, 500),
        new(ViceCityWeapon.SniperRifle, "Sniper rifle", 8, 1, 100),
        new(ViceCityWeapon.LaserScope, "PSG-1 laser scope", 8, 7, 100),
        new(ViceCityWeapon.RocketLauncher, "Rocket launcher", 7, 1, 50),
        new(ViceCityWeapon.Flamethrower, "Flamethrower", 7, 500, 1000),
        new(ViceCityWeapon.M60, "M60", 7, 100, 1000),
        new(ViceCityWeapon.Minigun, "Minigun", 7, 500, 1000),
        new(ViceCityWeapon.Camera, "Camera", 9, 36, 36),
    ];

    public static ViceCityWeaponInfo Get(ViceCityWeapon weapon) =>
        All.FirstOrDefault(w => w.Weapon == weapon)
        ?? throw new ArgumentOutOfRangeException(nameof(weapon), weapon, "Unknown Vice City weapon.");

    public static ViceCityWeaponInfo? TryGet(ViceCityWeapon weapon) => All.FirstOrDefault(w => w.Weapon == weapon);

    public static IEnumerable<ViceCityWeaponInfo> ForSlot(int slot) => All.Where(w => w.Slot == slot);
}

/// <summary>
/// The player ped inside the Vice City PedPool block: health, armour and the ten weapon slots.
/// </summary>
/// <remarks>
/// Unlike GTA III, Vice City persists the player's health, armour and weapons. The DE stores one
/// ped (the player) with health and armour as floats at payload +858/+862 and ten 24-byte
/// <c>CWeapon</c> records from +1038. The accessor only enables itself when the block has the
/// shape this was verified against, so an unexpected layout is left untouched rather than guessed.
/// </remarks>
public sealed class ViceCityPlayerPed : BlockAccessor
{
    public const int ExpectedLength = 1890;
    private const int HealthOffset = 858;
    private const int ArmourOffset = 862;
    private const int WeaponsOffset = 1038;

    public ViceCityPlayerPed(byte[] data, SaveBlock block)
        : base(data, block)
    {
        IsAvailable = Validate();
        Weapons = IsAvailable
            ? Enumerable.Range(0, ViceCityWeapons.SlotCount)
                .Select(i => new ViceCityWeaponSlot(data, block.DataOffset + WeaponsOffset + (i * ViceCityWeaponSlot.Size), i))
                .ToList()
            : [];
    }

    /// <summary>False when the PedPool did not have the verified shape; nothing can be edited then.</summary>
    public bool IsAvailable { get; }

    public IReadOnlyList<ViceCityWeaponSlot> Weapons { get; }

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
        if (Block.DataLength != ExpectedLength || GetInt(0) != 1)
        {
            return false;
        }

        var health = GetFloat(HealthOffset);
        var armour = GetFloat(ArmourOffset);
        if (!float.IsFinite(health) || health is < 0 or > 1000 || !float.IsFinite(armour) || armour is < 0 or > 1000)
        {
            return false;
        }

        for (var i = 0; i < ViceCityWeapons.SlotCount; i++)
        {
            var type = (ViceCityWeapon)GetInt(WeaponsOffset + (i * ViceCityWeaponSlot.Size));
            var ammo = GetInt(WeaponsOffset + (i * ViceCityWeaponSlot.Size) + 12);
            if (type != ViceCityWeapon.Unarmed && ViceCityWeapons.TryGet(type)?.Slot != i)
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
