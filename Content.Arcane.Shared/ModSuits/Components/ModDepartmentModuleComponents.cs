using Content.Arcane.Shared.ModSuits.Systems;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ModSuitSystem))]
public sealed partial class ModQuickCarryComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float Multiplier;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ModSuitSystem))]
public sealed partial class ModQuickCuffComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float Multiplier;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ModShoveBlockerComponent : Component;

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModTrayScannerModuleComponent : Component
{
    public EntityUid? Wearer;
}
