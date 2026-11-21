using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModProjectileDampenerSystem : VirtualController
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ModSuitSystem _suits = default!;

    private const float ExitSlack = 0.5f;
    private readonly HashSet<Entity<ProjectileComponent>> _nearby = new();
    private readonly List<EntityUid> _released = new();

    [SubscribeLocalEvent]
    private void OnActivated(Entity<ModProjectileDampenerModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (_net.IsClient || args.Wearer is not { } wearer)
            return;

        module.Comp.Wearer = wearer;
        var field = EnsureComp<ModDampenerFieldComponent>(wearer);
        field.Module = module;
        field.Radius = module.Comp.Radius;
        field.SpeedMultiplier = module.Comp.SpeedMultiplier;
        field.DamageMultiplier = module.Comp.DamageMultiplier;
        Dirty(wearer, field);
    }

    [SubscribeLocalEvent]
    private void OnDeactivated(Entity<ModProjectileDampenerModuleComponent> module, ref ModModuleDeactivatedEvent args) =>
        RemoveField(module);

    [SubscribeLocalEvent]
    private void OnTerminating(Entity<ModProjectileDampenerModuleComponent> module, ref EntityTerminatingEvent args) =>
        RemoveField(module);

    private void RemoveField(Entity<ModProjectileDampenerModuleComponent> module)
    {
        if (_net.IsClient || module.Comp.Wearer is not { } wearer)
            return;

        module.Comp.Wearer = null;
        if (TryComp<ModDampenerFieldComponent>(wearer, out var field) && field.Module == module.Owner)
            RemComp(wearer, field);
    }

    [SubscribeLocalEvent]
    private void OnHit(Entity<ModDampenedProjectileComponent> projectile, ref BeforeProjectileHitEvent args)
    {
        if (TryComp<ModDampenerFieldComponent>(projectile.Comp.Field, out var field)
            && IsWithinField(projectile, projectile.Comp.Field, ExitSlack))
            args.Damage *= field.DamageMultiplier;
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);
        if (_net.IsClient)
            return;

        var fields = EntityQueryEnumerator<ModDampenerFieldComponent, TransformComponent>();
        while (fields.MoveNext(out var wearer, out var field, out var position))
        {
            if (!_suits.TryGetInstalledModuleSuit(field.Module, out var suit) || suit.Comp.Wearer != wearer
                || !_suits.IsPartDeployed(suit, "outerClothing") || _containers.IsEntityInContainer(wearer))
                continue;

            _nearby.Clear();
            _lookup.GetEntitiesInRange(position.Coordinates, field.Radius, _nearby);
            foreach (var projectile in _nearby)
            {
                if (projectile.Comp.ProjectileSpent || _containers.IsEntityInContainer(projectile)
                    || !TryComp<PhysicsComponent>(projectile, out var body) || body.LinearVelocity.LengthSquared() < 0.0001f
                    || !TransformSystem.InRange(Transform(projectile).Coordinates, position.Coordinates, field.Radius))
                    continue;

                var tracked = EnsureComp<ModDampenedProjectileComponent>(projectile);
                if (tracked.Field == wearer
                    || IsWithinField(projectile, tracked.Field, ExitSlack))
                    continue;

                if (tracked.OriginalSpeed == 0f)
                    tracked.OriginalSpeed = body.LinearVelocity.Length();
                tracked.Field = wearer;
                Dirty(projectile, tracked);
            }
        }

        _released.Clear();
        var dampened = EntityQueryEnumerator<ModDampenedProjectileComponent, PhysicsComponent>();
        while (dampened.MoveNext(out var uid, out var tracked, out var body))
        {
            var speed = body.LinearVelocity.Length();
            var inside = !_containers.IsEntityInContainer(uid) && IsWithinField(uid, tracked.Field, ExitSlack);
            var multiplier = inside ? Comp<ModDampenerFieldComponent>(tracked.Field).SpeedMultiplier : 1f;
            var targetSpeed = tracked.OriginalSpeed * multiplier;
            if (speed > 0.001f && MathF.Abs(speed - targetSpeed) > 0.001f)
                PhysicsSystem.SetLinearVelocity(uid, body.LinearVelocity * (targetSpeed / speed), body: body);
            if (!inside)
                _released.Add(uid);
        }

        foreach (var uid in _released)
            RemComp<ModDampenedProjectileComponent>(uid);
    }

    private bool IsWithinField(EntityUid projectile, EntityUid wearer, float slack)
    {
        return TryComp<ModDampenerFieldComponent>(wearer, out var field)
            && !_containers.IsEntityInContainer(wearer)
            && _suits.TryGetInstalledModuleSuit(field.Module, out var suit) && suit.Comp.Wearer == wearer
            && _suits.IsPartDeployed(suit, "outerClothing")
            && TransformSystem.InRange(Transform(projectile).Coordinates, Transform(wearer).Coordinates, field.Radius + slack);
    }
}
