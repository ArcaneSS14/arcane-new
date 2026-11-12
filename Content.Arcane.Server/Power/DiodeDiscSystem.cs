using Content.Arcane.Shared.Power;
using Content.Goobstation.Shared.Supermatter.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.ActionBlocker;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Projectiles;
using Content.Shared.Singularity.Components;
using Content.Trauma.Shared.Construction;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Arcane.Server.Power;

public sealed partial class DiodeDiscSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DiodeDiscComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<DiodeDiscComponent, DiodeDiscDoAfterEvent>(OnComplete);
        SubscribeLocalEvent<DiodeProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnInteract(Entity<DiodeDiscComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;
        if (!HasComp<EmitterComponent>(target))
            return;
        args.Handled = true;
        if (!CanUpgrade(ent, target, args.User))
            return;
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay,
            new DiodeDiscDoAfterEvent(), ent, target, ent));
    }

    private bool CanUpgrade(Entity<DiodeDiscComponent> ent, EntityUid target, EntityUid user)
        => !TerminatingOrDeleted(ent) && !EntityManager.IsQueuedForDeletion(ent)
           && !TerminatingOrDeleted(target) && !EntityManager.IsQueuedForDeletion(target)
           && !TerminatingOrDeleted(user) && !EntityManager.IsQueuedForDeletion(user)
           && HasComp<EmitterComponent>(target) && !HasComp<UpgradedMachineComponent>(target)
           && ent.Comp.ComponentsToAdd.ContainsKey("UpgradedMachine")
           && ProtoMan.TryIndex(ent.Comp.NewBolt, out var bolt) && !bolt.Abstract
           && _hands.IsHolding(user, ent.Owner) && _blocker.CanInteract(user, target)
           && _interaction.InRangeAndAccessible(user, target);

    private void OnComplete(Entity<DiodeDiscComponent> ent, ref DiodeDiscDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target || args.Used != ent.Owner)
            return;
        args.Handled = true;
        if (!CanUpgrade(ent, target, args.User) || !TryComp<EmitterComponent>(target, out var emitter))
            return;

        EntityManager.AddComponents(target, ent.Comp.ComponentsToAdd, removeExisting: false);
        if (!HasComp<UpgradedMachineComponent>(target))
            return;
        emitter.BoltType = ent.Comp.NewBolt;
        Dirty(target, emitter);
        QueueDel(ent);
    }

    private void OnProjectileHit(Entity<DiodeProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.Applied || TerminatingOrDeleted(args.Target)
            || EntityManager.IsQueuedForDeletion(args.Target)
            || !TryComp<SupermatterComponent>(args.Target, out var supermatter)
            || HasComp<SupermatterImmuneComponent>(ent)
            || _containers.IsEntityInContainer(args.Target)
            || !TryComp<PhysicsComponent>(ent, out var physics) || physics.BodyType == BodyType.Static
            || !TryComp<ProjectileComponent>(ent, out var projectile))
            return;

        ent.Comp.Applied = true;
        if (!supermatter.Activated)
        {
            _adminLog.Add(LogType.Supermatter, LogImpact.High,
                $"{ent.Owner:actor} activated Supermatter {args.Target:subject}");
            _adminLog.Add(LogType.AdminMessage, LogImpact.Extreme,
                $"SUPERMATTER ACTIVATED BY {Name(ent)} AT {Transform(args.Target).Coordinates}");
            supermatter.Activated = true;
        }
        supermatter.Damage += ent.Comp.IntegrityDamage;
        // Source also adds the unmodified projectile damage to its power on collision.
        supermatter.Power += ent.Comp.EnergyDamage + (float) projectile.Damage.GetTotal();
        Dirty(args.Target, supermatter);
    }
}
