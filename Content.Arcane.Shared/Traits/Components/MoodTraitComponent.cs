using Content.Arcane.Shared.Mood;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Traits.Components;

[RegisterComponent]
public sealed partial class MoodTraitComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<MoodEffectPrototype>> MoodEffects = new();
}
