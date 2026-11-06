using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Arcane.Shared.MartialArts;

/// <summary>
/// Handles <see cref="CapoeiraParryComponent"/>: each strike that lands gives one attempt to deflect an attack.
/// </summary>
public sealed partial class CapoeiraParrySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    private static readonly SoundSpecifier ParrySound = new SoundPathSpecifier("/Audio/Weapons/genhit2.ogg");

    [SubscribeLocalEvent]
    private void OnMeleeHit(Entity<CapoeiraParryComponent> ent, ref MeleeHitEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);
        if (level < ent.Comp.MinLevel || !args.IsHit || !HitLivingTarget(args))
            return;

        var range = Math.Max(ent.Comp.MaxLevel - ent.Comp.MinLevel, 1);
        var progress = Math.Clamp((float) (level - ent.Comp.MinLevel) / range, 0f, 1f);

        var now = _timing.CurTime;
        var window = EnsureComp<CapoeiraParryWindowComponent>(args.User);
        window.Attempts.RemoveAll(attempt => now >= attempt.End);
        window.Attempts.Add(new CapoeiraParryAttempt
        {
            End = now + ent.Comp.WindowTime,
            Chance = MathHelper.Lerp(ent.Comp.MinChance, ent.Comp.MaxChance, progress),
        });
        Dirty(args.User, window);
    }

    [SubscribeLocalEvent]
    private void OnBeforeHarmfulAction(Entity<CapoeiraParryWindowComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (args.Cancelled
            || !args.CanRiposte
            || TryComp<MeleeWeaponComponent>(args.Used, out var melee) && !melee.CanBeParried)
            return;

        if (TryParry(ent, args.User))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnProjectile(Entity<CapoeiraParryWindowComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        // the projectile carries on past the user
        if (!args.Cancelled && TryParry(ent, args.Component.Shooter))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnHitscan(Entity<CapoeiraParryWindowComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected || !TryParry(ent, args.Shooter))
            return;

        args.Reflected = true;
        args.PassThrough = true;
    }

    private bool TryParry(Entity<CapoeiraParryWindowComponent> ent, EntityUid? attacker)
    {
        if (attacker == ent.Owner || _standing.IsDown(ent.Owner))
            return false;

        var now = _timing.CurTime;
        var attempts = ent.Comp.Attempts;
        attempts.RemoveAll(attempt => now >= attempt.End);
        if (attempts.Count == 0)
            return false;

        // one strike is one attempt, whether it works or not
        var chance = attempts[0].Chance;
        attempts.RemoveAt(0);
        Dirty(ent);

        if (!SharedRandomExtensions.PredictedProb(_timing, chance, GetNetEntity(ent)))
            return false;

        _popup.PopupPredicted(Loc.GetString("capoeira-parry-popup"), ent, attacker);
        _audio.PlayPredicted(ParrySound, ent, attacker);
        return true;
    }

    private bool HitLivingTarget(MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (target != args.User && _mobState.IsAlive(target))
                return true;
        }

        return false;
    }
}
