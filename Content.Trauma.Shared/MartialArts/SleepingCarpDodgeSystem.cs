using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Trauma.Shared.MartialArts;

public sealed partial class SleepingCarpDodgeSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _net = default!;

    private static readonly SoundSpecifier DodgeSound = new SoundCollectionSpecifier("BulletMiss",
        AudioParams.Default.WithVariation(0.125f));

    public override void Initialize()
    {
        SubscribeLocalEvent<SleepingCarpDodgeComponent, ProjectileReflectAttemptEvent>(OnProjectile);
        SubscribeLocalEvent<SleepingCarpDodgeComponent, HitscanDodgeAttemptEvent>(OnHitscan);
    }

    private void OnProjectile(Entity<SleepingCarpDodgeComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled || !CanDodge(args.Target))
            return;

        args.Cancelled = true;
        PlayDodge(args.Target);
    }

    private void OnHitscan(Entity<SleepingCarpDodgeComponent> ent, ref HitscanDodgeAttemptEvent args)
    {
        if (args.Dodged || !CanDodge(args.Target))
            return;

        args.Dodged = true;
        PlayDodge(args.Target);
    }

    private bool CanDodge(EntityUid target)
    {
        return _mob.IsAlive(target) && !_standing.IsDown(target) &&
            !HasComp<KnockedDownComponent>(target) && !HasComp<StunnedComponent>(target);
    }

    private void PlayDodge(EntityUid target)
    {
        if (_net.IsServer)
            _audio.PlayPvs(DodgeSound, target);
    }
}
