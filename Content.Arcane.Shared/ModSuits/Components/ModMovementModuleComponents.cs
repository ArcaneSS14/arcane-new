using Content.Arcane.Shared.ModSuits.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModAntiGravityComponent : Component;

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModJumpJetModuleComponent : Component
{
    [DataField]
    public float Distance = 6f;

    [DataField]
    public float Speed = 12f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/rocket_launcher.ogg");
}

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModJointTorsionModuleComponent : Component
{
    [DataField]
    public float ChargePerTile = 0.4f;

    [DataField]
    public float MaxTilesPerUpdate = 2f;

    public MapCoordinates? LastPosition;
}

[RegisterComponent]
public sealed partial class ModPlasmaStabilizerModuleComponent : Component;
