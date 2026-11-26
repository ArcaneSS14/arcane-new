using Content.Shared.Roles;

namespace Content.Arcane.Server.Station;

/// <summary>
/// Additional job defaults applied to a station that offers the required job.
/// </summary>
[RegisterComponent]
public sealed partial class ArcaneStationJobsComponent : Component
{
    /// <summary>
    /// Job whose presence in the station's setup enables the additional defaults.
    /// A configured zero-slot job still satisfies this condition.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<JobPrototype> RequiredJob;

    /// <summary>
    /// Round-start and midround slot pairs used only for jobs absent from the station's setup.
    /// Explicit pairs, including zero or unlimited slots, take precedence.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<JobPrototype>, int[]> AdditionalJobs = new();
}
