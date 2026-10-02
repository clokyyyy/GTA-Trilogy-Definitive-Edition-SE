namespace GtaDe.SaveFormat;

/// <summary>
/// The PlayerInfo block: money, hidden packages and the pickup bonuses.
/// </summary>
/// <remarks>
/// <para>
/// The field order matches classic GTA III (<c>CPlayerInfo::SavePlayerInfo</c>) and the struct is
/// densely packed, so most fields are not four-byte aligned. Verified against real saves:
/// <see cref="Money"/> equals <see cref="VisibleMoney"/>, and <see cref="TotalHiddenPackages"/>
/// reads 100, which is the correct total for GTA III.
/// </para>
/// <para>
/// Health, armour and weapons are deliberately absent. GTA III does not persist them: the
/// corresponding region of the PedPool block is entirely zero in every save produced by the
/// game, which is why the player always reloads with full health and no weapons.
/// </para>
/// </remarks>
public sealed class PlayerInfoBlock(byte[] data, SaveBlock block) : BlockAccessor(data, block)
{
    /// <summary>Total hidden packages in GTA III.</summary>
    public const int HiddenPackageTotal = 100;

    /// <summary>Spendable money.</summary>
    public int Money
    {
        get => GetInt(0x00);
        set => SetInt(0x00, value);
    }

    /// <summary>Whether the player was wasted or busted when the game was saved.</summary>
    public byte WastedBustedState
    {
        get => GetByte(0x04);
        set => SetByte(0x04, value);
    }

    public int WastedBustedTime
    {
        get => GetInt(0x05);
        set => SetInt(0x05, value);
    }

    /// <summary>Traffic density multiplier the game was running with.</summary>
    public ushort TrafficMultiplier
    {
        get => GetUInt16(0x09);
        set => SetUInt16(0x09, value);
    }

    public float RoadDensity
    {
        get => GetFloat(0x0B);
        set => SetFloat(0x0B, value);
    }

    /// <summary>
    /// The money readout the HUD animates towards. Kept in step with <see cref="Money"/> so the
    /// counter does not visibly roll after loading an edited save.
    /// </summary>
    public int VisibleMoney
    {
        get => GetInt(0x0F);
        set => SetInt(0x0F, value);
    }

    public int HiddenPackagesCollected
    {
        get => GetInt(0x13);
        set => SetInt(0x13, value);
    }

    public int TotalHiddenPackages
    {
        get => GetInt(0x17);
        set => SetInt(0x17, value);
    }

    /// <summary>Hidden-package bonus: the player never runs out of stamina.</summary>
    public bool InfiniteSprint
    {
        get => GetByte(0x1B) != 0;
        set => SetByte(0x1B, value ? (byte)1 : (byte)0);
    }

    /// <summary>Hidden-package bonus: faster weapon reloads.</summary>
    public bool FastReload
    {
        get => GetByte(0x1C) != 0;
        set => SetByte(0x1C, value ? (byte)1 : (byte)0);
    }

    /// <summary>Hidden-package bonus: no bribe is taken when busted.</summary>
    public bool GetOutOfJailFree
    {
        get => GetByte(0x1D) != 0;
        set => SetByte(0x1D, value ? (byte)1 : (byte)0);
    }

    /// <summary>Hidden-package bonus: hospital visits are free.</summary>
    public bool FreeHealthCare
    {
        get => GetByte(0x1E) != 0;
        set => SetByte(0x1E, value ? (byte)1 : (byte)0);
    }

    /// <summary>Sets both money fields at once so the HUD matches the wallet.</summary>
    public void SetMoney(int value)
    {
        Money = value;
        VisibleMoney = value;
    }
}
