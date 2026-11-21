using Content.Shared.Emp;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Ninja;

public sealed partial class NinjaEnergyStarSystem : EntitySystem
{
    private static readonly TimeSpan PulseInterval = TimeSpan.FromSeconds(6);

    [Dependency] private SharedEmpSystem _emp = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NinjaEnergyStarComponent, ThrowDoHitEvent>(OnHit, after: [typeof(SharedProjectileSystem)]);
        SubscribeLocalEvent<NinjaEnergyStarComponent, StopThrowEvent>(OnStopThrow);
        SubscribeLocalEvent<NinjaEnergyStarComponent, GotUnequippedHandEvent>(OnUnequipped);
    }

    private void OnHit(Entity<NinjaEnergyStarComponent> star, ref ThrowDoHitEvent args)
    {
        if (star.Comp.Hit || TerminatingOrDeleted(args.Target))
            return;

        star.Comp.Hit = true;
        if (Comp<EmbeddableProjectileComponent>(star).EmbeddedIntoUid == args.Target)
        {
            RemComp<TimedDespawnComponent>(star);
            star.Comp.NextPulse = _timing.CurTime + PulseInterval;
        }
        else
        {
            // A second contact during the same physics step must not embed an expended star.
            RemComp<EmbeddableProjectileComponent>(star);
            QueueDel(star);
        }

        Pulse(args.Target, 2500f, TimeSpan.FromSeconds(8), args.Component.Thrower);
    }

    private void OnStopThrow(Entity<NinjaEnergyStarComponent> star, ref StopThrowEvent args)
    {
        if (TryComp<EmbeddableProjectileComponent>(star, out var embed) && embed.EmbeddedIntoUid == null)
            QueueDel(star);
    }

    private void OnUnequipped(Entity<NinjaEnergyStarComponent> star, ref GotUnequippedHandEvent args)
    {
        // Hands remove the star before ThrowingSystem marks it as thrown.
        var user = args.User;
        Timer.Spawn(0, () =>
        {
            if (!TerminatingOrDeleted(star) && !HasComp<ThrownItemComponent>(star)
                && TryComp<EmbeddableProjectileComponent>(star, out var embed) && embed.EmbeddedIntoUid == null
                && !_hands.IsHolding(user, star))
                QueueDel(star);
        });
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<NinjaEnergyStarComponent, EmbeddableProjectileComponent>();
        while (query.MoveNext(out var uid, out var star, out var embedded))
        {
            if (star.NextPulse is not { } next || now < next || EntityManager.IsQueuedForDeletion(uid)
                || embedded.EmbeddedIntoUid is not { } target || TerminatingOrDeleted(target))
                continue;

            star.NextPulse = now + PulseInterval;
            Pulse(target, 750f, TimeSpan.FromSeconds(3));
        }
    }

    private void Pulse(EntityUid target, float energy, TimeSpan duration, EntityUid? user = null)
    {
        _emp.TryEmpEffects(target, energy, duration, user);
        SpawnAttachedTo(SharedEmpSystem.EmpPulseEffectPrototype, Transform(target).Coordinates);
        _audio.PlayPvs(SharedEmpSystem.EmpSound, target);
    }
}
