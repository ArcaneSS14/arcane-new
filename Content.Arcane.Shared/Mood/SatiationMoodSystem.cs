using Content.Arcane.Common.CCVar;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Mood;

/// <summary>
///     Applies hunger and thirst moodlets when the satiation of an entity crosses a threshold.
/// </summary>
public sealed partial class SatiationMoodSystem :
    BaseSatiationEffectSystem<SatiationMoodComponent, ProtoId<MoodEffectPrototype>?>
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private INetManager _net = default!;

    protected override Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<ProtoId<MoodEffectPrototype>?>> GetThresholds(
        SatiationMoodComponent comp) => comp.Satiations;

    protected override ProtoId<MoodEffectPrototype>? DefaultValue() => null;

    protected override void AfterSatiationUpdate(Entity<SatiationMoodComponent> entity)
    {
        if (_net.IsClient || !_cfg.GetCVar(ArcaneCVars.MoodEnabled))
            return;

        foreach (var thresholds in entity.Comp.Satiations.Values)
        {
            if (thresholds.Current is { } moodlet)
                RaiseLocalEvent(entity, new MoodEffectEvent(moodlet));
        }
    }
}
