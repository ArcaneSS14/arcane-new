using Content.Medical.Common.Body;

namespace Content.Arcane.Shared.LimbGnawing;

[RegisterComponent, NetworkedComponent]
public sealed partial class LimbGnawerComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(6);

    [DataField]
    public List<BodyPartType> PartTypes = new()
    {
        BodyPartType.Hand,
        BodyPartType.Foot,
        BodyPartType.Arm,
        BodyPartType.Leg,
    };
}
