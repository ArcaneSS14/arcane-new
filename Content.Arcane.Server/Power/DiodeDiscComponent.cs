using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Power;

[RegisterComponent]
public sealed partial class DiodeDiscComponent : Component
{
    [DataField(required: true)] public EntProtoId NewBolt;
    [DataField(required: true)] public ComponentRegistry ComponentsToAdd = new();
    [DataField] public TimeSpan Delay = TimeSpan.FromSeconds(2);
}

[RegisterComponent]
public sealed partial class DiodeProjectileComponent : Component
{
    [DataField] public float IntegrityDamage;
    [DataField] public float EnergyDamage;
    [ViewVariables] public bool Applied;
}
