using Content.Arcane.Shared.ModSuits.Systems;
using Content.Shared.Damage;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ModSuitSystem))]
public sealed partial class ModArmorModuleComponent : Component
{
    [DataField(required: true)]
    public DamageModifierSet Modifiers = new();

    [DataField]
    public List<string> Slots = [];
}

[RegisterComponent]
public sealed partial class ModArmorBoosterModuleComponent : Component;
