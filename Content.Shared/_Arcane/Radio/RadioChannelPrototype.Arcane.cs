// Arcane-Start
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio;

public sealed partial class RadioChannelPrototype
{
    /// <summary>
    /// Job icon used to identify the channel in UIs.
    /// </summary>
    [DataField]
    public ProtoId<JobIconPrototype> Icon { get; private set; } = "JobIconUnknown";
}
// Arcane-End
