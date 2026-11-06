using Robust.Shared.Prototypes;
using Content.Arcane.Common.Construction.Prototypes;

namespace Content.Shared.Construction.Components;

[RegisterComponent]
public sealed partial class MachinePartComponent : Component
{
    [DataField(required: true)]
    public ProtoId<MachinePartPrototype> Part;

    [DataField]
    public int Tier = 1;
}
