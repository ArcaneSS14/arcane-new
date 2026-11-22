using Content.Arcane.Shared.ModSuits.Systems;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent]
public sealed partial class ModJetpackModuleComponent : Component;

[RegisterComponent]
public sealed partial class ModMouthholeModuleComponent : Component;

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModMagbootsModuleComponent : Component
{
    public bool AddedMagboots;
    public EntityUid? Boots;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ModMouthholePartComponent : Component;

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModSpeedModuleComponent : Component
{
    [DataField]
    public float WalkModifier = 1f;

    [DataField]
    public float SprintModifier = 1f;
}
