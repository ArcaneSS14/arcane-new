using Content.Goobstation.Shared.Xenobiology;

namespace Content.Arcane.Server.Xenobiology;

[RegisterComponent]
public sealed partial class SlimeLivestockFillComponent : Component
{
    [DataField(required: true)]
    public ProtoId<BreedPrototype> Breed;

    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField(required: true)]
    public int Count;

    [DataField(required: true)]
    public string Container = string.Empty;
}
