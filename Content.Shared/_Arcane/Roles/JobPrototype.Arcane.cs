using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

public sealed partial class JobPrototype
{
    /// <summary>
    /// Titles a player can pick for this job instead of its name, shown on the ID card and station records.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobAlternateTitlePrototype>> AlternateTitles { get; private set; } = [];
}
