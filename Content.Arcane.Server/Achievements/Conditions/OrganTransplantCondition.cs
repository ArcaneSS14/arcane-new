using Content.Medical.Common.Body;
using Content.Medical.Shared.Surgery;
using Content.Shared.Body;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class OrganTransplantCondition : AchievementCondition;

/// <summary>Initial ownership used only to distinguish a transplant from reattaching the patient's own organ.</summary>
[RegisterComponent]
public sealed partial class AchievementOrganOriginComponent : Component
{
    public EntityUid Body;
}

public sealed partial class OrganTransplantConditionSystem : AchievementConditionSystem<OrganTransplantCondition>
{
    [Dependency] private BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, BodyInitEvent>(OnBodyInit);
        SubscribeLocalEvent<BodyComponent, SurgeryOrganAddedEvent>(OnSurgery);
    }

    private void OnBodyInit(Entity<BodyComponent> ent, ref BodyInitEvent args)
    {
        if (Entries.Count == 0)
            return;
        foreach (var organ in _body.GetOrgans(ent.AsNullable()))
        {
            if (!HasComp<AchievementOrganOriginComponent>(organ))
                AddComp(organ, new AchievementOrganOriginComponent { Body = ent });
        }
    }

    private void OnSurgery(Entity<BodyComponent> ent, ref SurgeryOrganAddedEvent args)
    {
        if (_body.GetBody(args.Organ) != ent.Owner
            || TryComp<AchievementOrganOriginComponent>(args.Organ, out var origin) && origin.Body == ent.Owner)
            return;

        ProgressAll(args.User);
    }
}
