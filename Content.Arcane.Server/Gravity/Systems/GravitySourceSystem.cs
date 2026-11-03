using Content.Arcane.Common.Gravity;
using Content.Arcane.Server.Gravity.Components;
using Content.Server.Gravity;
using Content.Server.Power.Components;
using Content.Shared.Gravity;

namespace Content.Arcane.Server.Gravity.Systems;

/// <summary>
///     System that... uhh... provides gravity by every ent with component without useless shit
/// </summary>
public sealed partial class GravitySourceSystem : EntitySystem
{
    [Dependency] private GravitySystem _gravity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravitySourceComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<GravitySourceComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<GravityProviderQueryEvent>(OnProviderQuery);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GravitySourceComponent, ApcComponent, PowerNetworkBatteryComponent, TransformComponent>();
        while (query.MoveNext(out _, out var gravitySource, out var apc, out var battery, out var xform))
        {
            var shouldBeActive = ShouldProvideGravity(apc, battery);

            if (gravitySource.Active == shouldBeActive)
                continue;

            gravitySource.Active = shouldBeActive;
            RefreshParentGravity(xform.ParentUid, shouldBeActive);
        }
    }

    private static bool ShouldProvideGravity(ApcComponent apc, PowerNetworkBatteryComponent battery)
    {
        if (!apc.MainBreakerEnabled)
            return false;

        var networkBattery = battery.NetworkBattery;
        return networkBattery.CurrentStorage > 0f || networkBattery.CurrentReceiving > 0f;
    }

    private void OnParentChanged(Entity<GravitySourceComponent> ent, ref EntParentChangedMessage args)
    {
        if (!ent.Comp.Active)
            return;

        if (args.OldParent is { } oldParent)
            RefreshParentGravity(oldParent, false);

        RefreshParentGravity(args.Transform.ParentUid, true);
    }

    private void OnShutdown(Entity<GravitySourceComponent> ent, ref ComponentShutdown args)
    {
        if (!ent.Comp.Active)
            return;

        // Gravity is recalculated through the provider query, so this source must no longer answer it.
        ent.Comp.Active = false;
        var xform = Transform(ent);
        RefreshParentGravity(xform.ParentUid, false);
    }

    private void OnProviderQuery(ref GravityProviderQueryEvent args)
    {
        if (args.Enabled)
            return;

        var query = EntityQueryEnumerator<GravitySourceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var source, out var xform))
        {
            if (!source.Active || xform.ParentUid != args.Grid || TerminatingOrDeleted(uid))
                continue;

            args.Enabled = true;
            return;
        }
    }

    private void RefreshParentGravity(EntityUid parentUid, bool enable)
    {
        if (!HasComp<GravityComponent>(parentUid))
            return;

        if (enable)
            _gravity.EnableGravity(parentUid);
        else
            _gravity.RefreshGravity(parentUid);
    }
}
