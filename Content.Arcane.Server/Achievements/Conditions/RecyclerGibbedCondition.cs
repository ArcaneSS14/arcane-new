using Content.Server.Materials;
using Content.Shared.Body;

namespace Content.Arcane.Server.Achievements.Conditions;

// Retain the source type/achievement ID; Trauma's recycler damages limbs instead of gibbing.
public sealed partial class RecyclerGibbedCondition : AchievementCondition;

public sealed partial class RecyclerGibbedConditionSystem : AchievementConditionSystem<RecyclerGibbedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, RecyclerDamagedEvent>(OnDamaged);
    }

    private void OnDamaged(Entity<BodyComponent> ent, ref RecyclerDamagedEvent args) => ProgressAll(ent);
}
