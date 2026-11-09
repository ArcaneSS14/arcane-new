using Content.Arcane.Shared.StationGoal;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.StationGoal;

/// <summary>
///     Added to a station, picks a random goal from the list at round start.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalSystem))]
public sealed partial class StationGoalComponent : Component
{
    [DataField]
    public List<ProtoId<StationGoalPrototype>> Goals = new();
}
