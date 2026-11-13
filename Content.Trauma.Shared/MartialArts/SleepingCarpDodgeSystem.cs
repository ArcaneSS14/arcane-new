using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.MartialArts;

public sealed partial class SleepingCarpDodgeSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly SoundSpecifier DodgeSound = new SoundCollectionSpecifier("BulletMiss",
        AudioParams.Default.WithVariation(0.125f));

    public override void Initialize()
    {
        SubscribeLocalEvent<SleepingCarpDodgeComponent, ProjectileDodgeAttemptEvent>(OnProjectile);
        SubscribeLocalEvent<SleepingCarpDodgeComponent, HitscanDodgeAttemptEvent>(OnHitscan);
    }

    private void OnProjectile(Entity<SleepingCarpDodgeComponent> ent, ref ProjectileDodgeAttemptEvent args)
    {
        if (args.Dodged || !CanDodge(args.Target))
            return;

        args.Dodged = true;
        PlayDodge(ent.Comp, args.Target);
    }

    private void OnHitscan(Entity<SleepingCarpDodgeComponent> ent, ref HitscanDodgeAttemptEvent args)
    {
        if (args.Dodged || !CanDodge(args.Target))
            return;

        args.Dodged = true;
        PlayDodge(ent.Comp, args.Target);
    }

    private bool CanDodge(EntityUid target)
    {
        return _mob.IsAlive(target) && !_standing.IsDown(target) &&
            !HasComp<KnockedDownComponent>(target) && !HasComp<StunnedComponent>(target);
    }

    private void PlayDodge(SleepingCarpDodgeComponent dodge, EntityUid target)
    {
        if (!_net.IsServer || !_timing.IsFirstTimePredicted || _timing.CurTime < dodge.NextEffect)
            return;

        dodge.NextEffect = _timing.CurTime + TimeSpan.FromSeconds(0.05);
        RaiseNetworkEvent(new SleepingCarpDodgeEffectEvent(GetNetEntity(target), _random.Prob(0.5f)), Filter.Pvs(target));
        _audio.PlayPvs(DodgeSound, target);
    }
}

[Serializable, NetSerializable]
public sealed class SleepingCarpDodgeEffectEvent(NetEntity entity, bool left) : EntityEventArgs
{
    public NetEntity Entity { get; } = entity;
    public bool Left { get; } = left;
}
