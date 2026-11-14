using Content.Shared.Whitelist;

namespace Content.Arcane.Shared.ModSuits;

[Prototype]
public sealed partial class ModThemePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId? Name;

    [DataField]
    public int ComplexityMax = 15;

    [DataField]
    public float ChargeDrain = 0.8f;

    [DataField]
    public TimeSpan ActivationStepTime = TimeSpan.FromSeconds(1);

    [DataField]
    public List<EntProtoId> InbuiltModules = [];

    [DataField]
    public EntityWhitelist? AllowedModules;

    [DataField]
    public EntityWhitelist? BlacklistedModules;
}
