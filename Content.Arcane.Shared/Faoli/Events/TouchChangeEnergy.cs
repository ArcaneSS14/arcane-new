using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Arcane.Shared.Faoli.Events;

public sealed partial class TouchChangeEnergy : EntityTargetActionEvent
{

    [DataField(required: true)]
    public int Amount;

    [DataField]
    public int Cost = 0;

    [DataField]
    public SoundSpecifier? Sound;
}
