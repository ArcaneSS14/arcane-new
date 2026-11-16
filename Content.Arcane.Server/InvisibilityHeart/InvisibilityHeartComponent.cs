using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.InvisibilityHeart.Components;

[RegisterComponent]
public sealed partial class InvisibilityHeartComponent : Component
{
    [DataField]
    public EntProtoId OrganProto = "OrganInvisibilityHeart";

    [DataField]
    public string OrganSlot = "heart";
}
