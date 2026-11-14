using Content.Arcane.Shared.ModSuits.Systems;
using Robust.Shared.Audio;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModLoudspeakerModuleComponent : Component
{
    public EntityUid? RegisteredWearer;
}

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModSoundModuleComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;
}

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModSpawnModuleComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Prototypes = [];

    [DataField]
    public bool ToHand;
}

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModInstrumentModuleComponent : Component;
