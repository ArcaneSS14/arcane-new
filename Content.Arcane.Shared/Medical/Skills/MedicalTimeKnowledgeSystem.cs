using Content.Arcane.Common.Medical.Skills;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Arcane.Shared.Medical.Skills;

/// <summary>
/// Applies the First Aid and Surgery skills to the time of healing items and surgery steps.
/// </summary>
public sealed partial class MedicalTimeKnowledgeSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    private static readonly EntProtoId FirstAidKnowledge = "FirstAidKnowledge";
    private static readonly EntProtoId SurgeryKnowledge = "SurgeryKnowledge";

    [SubscribeLocalEvent]
    private void OnGetHealingTime(Entity<KnowledgeHolderComponent> ent, ref GetHealingTimeMultiplierEvent args)
        => args.Multiplier *= GetMultiplier(ent, FirstAidKnowledge);

    [SubscribeLocalEvent]
    private void OnGetSurgeryTime(Entity<KnowledgeHolderComponent> ent, ref GetSurgeryTimeMultiplierEvent args)
        => args.Multiplier *= GetMultiplier(ent, SurgeryKnowledge);

    private float GetMultiplier(EntityUid user, EntProtoId skill)
    {
        if (!_knowledge.SkillsEnabled
            || _knowledge.GetContainer(user) is not { } brain
            || !ProtoMan.Index(skill).TryGetComponent<MedicalTimeKnowledgeComponent>(out var comp, Factory))
            return 1f;

        // Removing a skill chip deletes the unit at level 0, which has to keep the unskilled penalty.
        var level = _knowledge.GetKnowledge(brain, skill)?.Comp.NetLevel ?? 0;
        return Interpolate(comp.Multipliers, level);
    }

    private static float Interpolate(SortedDictionary<int, float> multipliers, int level)
    {
        (int Level, float Multiplier)? previous = null;
        foreach (var (pointLevel, multiplier) in multipliers)
        {
            if (level <= pointLevel)
            {
                if (previous is not { } start)
                    return multiplier;

                var blend = (float) (level - start.Level) / (pointLevel - start.Level);
                return MathHelper.Lerp(start.Multiplier, multiplier, blend);
            }

            previous = (pointLevel, multiplier);
        }

        return previous?.Multiplier ?? 1f;
    }
}
