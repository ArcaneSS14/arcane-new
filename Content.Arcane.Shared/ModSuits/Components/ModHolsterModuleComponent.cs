using Content.Arcane.Shared.ModSuits.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModHolsterModuleComponent : Component
{
    [DataField]
    public SoundSpecifier? HolsterSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/pistol_magin.ogg");

    [DataField]
    public SoundSpecifier? DrawSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/pistol_magout.ogg");

    public ContainerSlot Container = default!;
}
