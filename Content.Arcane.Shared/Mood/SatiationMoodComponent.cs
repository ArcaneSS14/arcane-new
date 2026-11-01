using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Mood;

/// <summary>
///     Gives the entity moodlets depending on its current satiations.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SatiationMoodSystem))]
public sealed partial class SatiationMoodComponent : Component
{
    /// <summary>
    ///     Moodlets keyed by the satiation thresholds they are applied at.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<ProtoId<MoodEffectPrototype>?>> Satiations = [];
}
