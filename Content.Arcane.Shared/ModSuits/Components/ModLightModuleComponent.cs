using Content.Arcane.Shared.ModSuits.Systems;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModLightModuleComponent : Component
{
    [DataField]
    public string Slot = "head";

    [DataField]
    public float Radius = 5f;

    [DataField]
    public float Energy = 3f;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public EntProtoId LightPrototype = "ModSuitModuleLight";

    public EntityUid? Light;
}
