using Content.Arcane.Shared.ModSuits.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModClampComponent : Component
{
    [DataField]
    public int MaxContents = 3;

    [DataField]
    public TimeSpan GrabDelay = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan DropDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_hydraulic.ogg");

    public Container Cargo = default!;
}
