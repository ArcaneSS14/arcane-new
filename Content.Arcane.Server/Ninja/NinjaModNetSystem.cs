using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;

namespace Content.Arcane.Server.Ninja;

public sealed partial class NinjaModNetSystem : EntitySystem
{
    private const float Range = 9f;
    private const float Speed = 15f;
    private static readonly EntProtoId NetProjectile = "NinjaNetProjectile";
    private static readonly EntProtoId Net = "NinjaEnergyNet";

    [Dependency] private ModSuitSystem _suits = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NinjaModNetComponent, ModModuleUsedEvent>(OnFire);
        SubscribeLocalEvent<NinjaNetProjectileComponent, ProjectileHitEvent>(OnHit);
        SubscribeLocalEvent<NinjaNetComponent, StrapAttemptEvent>(OnStrap);
        SubscribeLocalEvent<NinjaNetComponent, UnstrapAttemptEvent>(OnUnstrap);
    }

    private void OnFire(Entity<NinjaModNetComponent> module, ref ModModuleUsedEvent args)
    {
        if (args.Handled || args.Action is not ModNinjaNetActionEvent action || !CanOperate(module, args.User))
            return;

        var origin = _transform.GetMoverCoordinates(args.User);
        if (!action.Target.IsValid(EntityManager))
            return;

        var source = _transform.ToMapCoordinates(origin);
        var destination = _transform.ToMapCoordinates(action.Target);
        var direction = destination.Position - source.Position;
        if (destination.MapId != source.MapId || direction.LengthSquared() is <= 0.01f or > Range * Range)
            return;

        var projectile = SpawnAttachedTo(NetProjectile, origin);
        Comp<NinjaNetProjectileComponent>(projectile).Module = module;
        _gun.ShootProjectile(projectile, direction, _physics.GetMapLinearVelocity(origin), module, args.User, Speed);
        args.Handled = true;
    }

    private void OnHit(Entity<NinjaNetProjectileComponent> projectile, ref ProjectileHitEvent args)
    {
        var target = args.Target;
        if (args.Shooter is not { } user || !CanOperate(projectile.Comp.Module, user)
            || target == user || TerminatingOrDeleted(target) || !HasComp<MobStateComponent>(target)
            || !TryComp<BuckleComponent>(target, out var buckle) || _containers.IsEntityInContainer(target)
            || HasComp<NinjaNetComponent>(Transform(target).ParentUid))
            return;

        var net = SpawnAttachedTo(Net, _transform.GetMoverCoordinates(target));
        var trap = Comp<NinjaNetComponent>(net);
        trap.Module = projectile.Comp.Module;
        trap.Target = target;
        _buckle.Unbuckle((target, buckle), null);
        if (!_buckle.TryBuckle(target, null, net, buckle, popup: false))
            QueueDel(net);
    }

    private bool CanOperate(EntityUid module, EntityUid wearer) =>
        !TerminatingOrDeleted(wearer) && _suits.TryGetInstalledModuleSuit(module, out var suit)
        && suit.Comp.Wearer == wearer && suit.Comp.State == ModSuitState.Active && suit.Comp.Powered
        && _suits.IsPartDeployed(suit, "gloves");

    private void OnStrap(Entity<NinjaNetComponent> net, ref StrapAttemptEvent args)
    {
        if (args.User != null || args.Buckle.Owner != net.Comp.Target)
            args.Cancelled = true;
    }

    private void OnUnstrap(Entity<NinjaNetComponent> net, ref UnstrapAttemptEvent args) => args.Cancelled = true;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<NinjaNetComponent>();
        while (query.MoveNext(out var uid, out var net))
        {
            if (TerminatingOrDeleted(net.Target) || Transform(net.Target).ParentUid != uid
                || !_suits.TryGetInstalledModuleSuit(net.Module, out var suit) || suit.Comp.Wearer is not { } wearer
                || !CanOperate(net.Module, wearer))
                QueueDel(uid);
        }
    }
}
