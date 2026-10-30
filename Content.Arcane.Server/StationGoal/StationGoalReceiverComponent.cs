namespace Content.Arcane.Server.StationGoal;

/// <summary>
///     Fax machine that prints station goals.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalSystem))]
public sealed partial class StationGoalReceiverComponent : Component
{
    /// <summary>
    ///     Receive the goals of every station, not only of the station this fax is on.
    /// </summary>
    [DataField]
    public bool AllStations;
}
