using Content.Shared.Atmos;

namespace Content.Arcane.Shared.Power;

[Prototype]
public sealed partial class SupermatterEventPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public Gas? GasToSpawn;
    [DataField] public EntProtoId? ProtoToSpawn;
    [DataField] public bool Surge;
    [DataField(required: true)] public LocId Announcement;
}
