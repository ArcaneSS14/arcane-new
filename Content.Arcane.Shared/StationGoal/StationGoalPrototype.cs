using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.StationGoal;

/// <summary>
///     A secondary objective sent to the station by fax at round start.
/// </summary>
[Prototype]
public sealed partial class StationGoalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Text of the fax, gets the station name as <c>station</c>.
    /// </summary>
    [DataField(required: true)]
    public LocId Text;

    [DataField]
    public int? MinPlayers;

    [DataField]
    public int? MaxPlayers;

    /// <summary>
    ///     Items the goal needs, spawned next to every fax that receives it.
    /// </summary>
    [DataField]
    public List<EntProtoId> Spawns = new();
}
