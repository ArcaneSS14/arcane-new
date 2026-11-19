using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityConditions;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.EntityConditions;

public sealed partial class ForeignBodyPartCondition : EntityConditionBase<ForeignBodyPartCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
}

public sealed partial class ForeignBodyPartConditionSystem : EntityConditionSystem<HumanoidProfileComponent, ForeignBodyPartCondition>
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private MarkingManager _markings = default!;

    protected override void Condition(Entity<HumanoidProfileComponent> entity, ref EntityConditionEvent<ForeignBodyPartCondition> args)
    {
        // Nubody stores the species appearance on organs through their markings group.
        var expected = _markings.GetMarkingData(entity.Comp.Species);
        foreach (var part in _body.GetOrgans<BodyPartComponent>(entity.Owner))
        {
            if (!TryComp<OrganComponent>(part, out var organ)
                || organ.Category is not { } category
                || !TryComp<VisualOrganMarkingsComponent>(part, out var markings)
                || !expected.TryGetValue(category, out var own)
                || own.Group == markings.MarkingData.Group)
                continue;

            args.Result = true;
            return;
        }
    }
}

public sealed partial class NoLimbsCondition : EntityConditionBase<NoLimbsCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
}

public sealed partial class NoLimbsConditionSystem : EntityConditionSystem<BodyComponent, NoLimbsCondition>
{
    [Dependency] private BodySystem _body = default!;

    protected override void Condition(Entity<BodyComponent> entity, ref EntityConditionEvent<NoLimbsCondition> args)
    {
        foreach (var part in _body.GetOrgans<BodyPartComponent>(entity.AsNullable()))
        {
            if (part.Comp.PartType is BodyPartType.Arm or BodyPartType.Leg)
                return;
        }
        args.Result = true;
    }
}
