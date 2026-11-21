using Content.Arcane.Shared.ModSuits.Systems;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ModSuitSystem))]
public sealed partial class ModEmpShieldModuleComponent : Component
{
    [DataField]
    public bool RequireActive = true;
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(ModSuitSystem))]
public sealed partial class ModEmpPulseModuleComponent : Component
{
    [DataField]
    public float Range = 4f;

    [DataField]
    public float EnergyConsumption = 100000f;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ModEmpWardComponent : Component;
