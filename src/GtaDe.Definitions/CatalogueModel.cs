namespace GtaDe.Definitions;

/// <summary>Which island a piece of content belongs to.</summary>
public enum Island
{
    Portland,
    StauntonIsland,
    ShoresideVale,
}

/// <summary>
/// A single story mission.
/// </summary>
/// <param name="Name">The mission's name as the game presents it.</param>
/// <param name="Flag">
/// The script global that records completion. Resolved by name against the symbol table embedded
/// in the save, so it survives game patches that move variables around.
/// </param>
/// <param name="Note">Anything the user should know before toggling this mission.</param>
/// <param name="CountsTowardTotal">
/// Whether the engine's "missions passed" counter includes this entry. The opening 8-Ball
/// sequence does not count: across every sample save, the number of story flags set is exactly
/// one more than the counter in the Stats block.
/// </param>
public sealed record MissionEntry(
    string Name,
    string Flag,
    string? Note = null,
    bool CountsTowardTotal = true);

/// <summary>
/// A group of missions given by one contact.
/// </summary>
/// <param name="Id">Stable identifier, used for navigation and settings.</param>
/// <param name="Name">The contact's name.</param>
/// <param name="Island">Where the strand takes place.</param>
/// <param name="Missions">The missions in story order.</param>
/// <param name="CompletionFlag">
/// Optional global the script sets once every mission in the strand is done. It gates later
/// content, so the editor keeps it in step with the individual flags.
/// </param>
public sealed record MissionStrand(
    string Id,
    string Name,
    Island Island,
    IReadOnlyList<MissionEntry> Missions,
    string? CompletionFlag = null);

/// <summary>One of the twenty Rampages.</summary>
public sealed record RampageEntry(int Number, Island Island, string Location, string Objective)
{
    /// <summary>
    /// The script global holding this Rampage's completion flag.
    /// </summary>
    /// <remarks>
    /// Note the <c>_FLAG</c> suffix. The similarly named <c>RAMPAGE_nn</c> variables are script
    /// object handles, not completion flags: they hold the same non-zero values in every save,
    /// including saves where the Rampage has never been attempted.
    /// </remarks>
    public string Flag => $"RAMPAGE_{Number:00}_FLAG";
}

/// <summary>One of the twenty Unique Stunt Jumps.</summary>
public sealed record UniqueJumpEntry(int Number, Island Island, string Location)
{
    /// <summary>The script global holding this jump's completion flag.</summary>
    public string Flag => $"FLAG_USJ{Number}_PASSED";
}

/// <summary>A vehicle wanted by one of the import/export garages.</summary>
public sealed record ImportExportVehicle(int Slot, string Name);

/// <summary>An import/export garage and the list of vehicles it asks for.</summary>
public sealed record ImportExportGarage(
    string Id,
    string Name,
    Island Island,
    string SlotPrefix,
    string FilledCountVariable,
    IReadOnlyList<ImportExportVehicle> Vehicles);
