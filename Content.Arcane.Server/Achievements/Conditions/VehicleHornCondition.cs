using Content.Goobstation.Shared.Vehicles;
using Content.Goobstation.Server.Vehicles;
using Content.Shared.Whitelist;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class VehicleHornCondition : AchievementCondition
{
    [DataField]
    public EntityWhitelist? Whitelist;
}

public sealed partial class VehicleHornConditionSystem : AchievementConditionSystem<VehicleHornCondition>
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetaDataComponent, HornActionEvent>(OnHorned, after: [typeof(VehicleSystem)]);
    }

    private void OnHorned(EntityUid uid, MetaDataComponent metadata, HornActionEvent args)
    {
        if (!args.Handled || !TryComp<VehicleComponent>(uid, out var component)
            || args.Performer != component.Driver || component.HornSound == null)
            return;

        foreach (var (proto, condition) in Entries)
        {
            if (_whitelist.IsWhitelistPassOrNull(condition.Whitelist, uid))
                Achievements.AddProgress(args.Performer, proto);
        }
    }
}
