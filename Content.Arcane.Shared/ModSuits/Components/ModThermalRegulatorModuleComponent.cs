namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent]
public sealed partial class ModThermalRegulatorModuleComponent : Component
{
    [DataField]
    public float TargetTemperature = 310.15f;

    [DataField]
    public float Rate = 5f;
}
