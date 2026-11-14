using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Arcane.Shared.ModSuits.Components;

/// <summary>The removable power source of a MOD control unit.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ModCoreComponent : Component;

[RegisterComponent]
public sealed partial class ModCoreRefuelableComponent : Component
{
    [DataField(required: true)]
    public EntityWhitelist Fuel = new();

    [DataField]
    public float ChargePerFuel = 200f;

    [DataField]
    public SoundSpecifier RefuelSound = new SoundPathSpecifier("/Audio/Effects/refill.ogg");
}
