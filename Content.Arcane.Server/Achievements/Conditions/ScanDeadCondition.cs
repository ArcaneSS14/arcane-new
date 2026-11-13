using Content.Server.Medical;
using Content.Server.Medical.Components;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Systems;
using Content.Shared.Item;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class ScanDeadCondition : AchievementCondition;

public sealed partial class ScanDeadConditionSystem : AchievementConditionSystem<ScanDeadCondition>
{
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemComponent, HealthAnalyzerDoAfterEvent>(OnScanned, after: [typeof(HealthAnalyzerSystem)]);
    }

    private void OnScanned(Entity<ItemComponent> ent, ref HealthAnalyzerDoAfterEvent args)
    {
        if (!args.Handled || args.Cancelled || !HasComp<HealthAnalyzerComponent>(ent)
            || args.Target is not { } target || !_mobState.IsDead(target))
            return;

        ProgressAll(args.User);
    }
}
