using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Stealth.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private BloodstreamSystem _modBloodstream = default!;
    [Dependency] private SharedStunSystem _adrenalineStun = default!;
    [Dependency] private DamageableSystem _kickDamage = default!;

    private void InitializeAntagonistModules()
    {
        SubscribeLocalEvent<ModCloakModuleComponent, ModModuleActivatedEvent>(OnModCloakActivated);
        SubscribeLocalEvent<ModCloakModuleComponent, ModModuleActivateAttemptEvent>(OnModCloakAttempt);
        SubscribeLocalEvent<ModCloakModuleComponent, ModModuleDeactivatedEvent>(OnModCloakDeactivated);
        SubscribeLocalEvent<ModCloakedComponent, MeleeAttackEvent>(OnCloakedAttack);
        SubscribeLocalEvent<ModCloakedComponent, SelfBeforeGunShotEvent>(OnCloakedShot);
        SubscribeLocalEvent<ModCloakedComponent, DamageChangedEvent>(OnCloakedDamaged);
        SubscribeLocalEvent<ModAdrenalineModuleComponent, ModModuleUsedEvent>(OnAdrenalineUsed);
        SubscribeLocalEvent<ModPowerKickModuleComponent, ModPowerKickActionEvent>(OnPowerKickAction);
        SubscribeLocalEvent<ModPowerKickModuleComponent, ModModuleUsedEvent>(OnPowerKickUsed);
        SubscribeLocalEvent<ModPowerKickModuleComponent, ModPowerKickDoAfterEvent>(OnPowerKickCharged);
        SubscribeLocalEvent<ModPowerKickingComponent, ThrowDoHitEvent>(OnPowerKickHit);
        SubscribeLocalEvent<ModPowerKickingComponent, StopThrowEvent>(OnPowerKickStopped);
    }

    private void OnModCloakAttempt(Entity<ModCloakModuleComponent> module, ref ModModuleActivateAttemptEvent args)
    {
        if (HasComp<StealthComponent>(args.User) || HasComp<StealthOnMoveComponent>(args.User))
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("modsuit-cloak-already-active"), args.Suit, args.User);
        }
    }

    private void OnModCloakActivated(Entity<ModCloakModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (args.Wearer is { } wearer && !TerminatingOrDeleted(wearer))
            EnsureComp<ModCloakedComponent>(wearer).Module = module;
    }

    private void OnModCloakDeactivated(Entity<ModCloakModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        if (args.Wearer is { } wearer && TryComp(wearer, out ModCloakedComponent? cloak) && cloak.Module == module.Owner)
            RemComp(wearer, cloak);
    }

    private void OnCloakedAttack(Entity<ModCloakedComponent> wearer, ref MeleeAttackEvent args) =>
        BreakModCloak(wearer, wearer.Comp);

    private void OnCloakedShot(Entity<ModCloakedComponent> wearer, ref SelfBeforeGunShotEvent args)
    {
        if (!args.Cancelled)
            BreakModCloak(wearer, wearer.Comp);
    }

    private void OnCloakedDamaged(EntityUid uid, ModCloakedComponent cloak, DamageChangedEvent args)
    {
        if (args.DamageIncreased && args.Origin is { } origin && origin != uid)
            BreakModCloak(uid, cloak);
    }

    private void BreakModCloak(EntityUid wearer, ModCloakedComponent cloak)
    {
        if (_net.IsClient || !TryGetInstalledModuleSuit(cloak.Module, out var suit)
            || !TryComp(cloak.Module, out ModModuleComponent? module) || !module.Active || suit.Comp.Wearer != wearer)
            return;

        SetModuleActive(suit, (cloak.Module, module), false);
        _popup.PopupEntity(Loc.GetString("modsuit-cloak-broken"), wearer, wearer);
    }

    private void OnAdrenalineUsed(Entity<ModAdrenalineModuleComponent> module, ref ModModuleUsedEvent args)
    {
        if (args.Handled || !_modBloodstream.TryAddToBloodstream(args.User, module.Comp.Solution.Clone()))
            return;

        _adrenalineStun.TryUnstun(args.User);
        _adrenalineStun.TryStanding(args.User);
        _audio.PlayPvs(module.Comp.Sound, args.User);
        args.Handled = true;
    }

    private void OnPowerKickAction(Entity<ModPowerKickModuleComponent> module, ref ModPowerKickActionEvent args)
    {
        if (!args.Handled && TryComp(module, out ModModuleComponent? installed))
            args.Handled = TryUseModule((module.Owner, installed), args.Performer, args);
    }

    private void OnPowerKickUsed(Entity<ModPowerKickModuleComponent> module, ref ModModuleUsedEvent args)
    {
        if (args.Handled || args.Action is not ModPowerKickActionEvent action
            || !CanFinishPowerKick(module, args.User) || HasComp<ModPowerKickingComponent>(args.User))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, module.Comp.ChargeTime,
            new ModPowerKickDoAfterEvent(GetNetCoordinates(action.Target), GetNetEntity(action.Entity)), module)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
        };
        if (!_clampDoAfter.TryStartDoAfter(doAfter))
            return;

        _audio.PlayPvs(module.Comp.ChargeSound, args.User);
        _popup.PopupEntity(Loc.GetString(module.Comp.ChargePopup), args.User, args.User);
        args.Handled = true;
    }

    private void OnPowerKickCharged(Entity<ModPowerKickModuleComponent> module, ref ModPowerKickDoAfterEvent args)
    {
        var user = args.User;
        if (_net.IsClient || args.Cancelled || args.Handled || !CanFinishPowerKick(module, user)
            || HasComp<ModPowerKickingComponent>(user))
            return;

        var from = _transform.GetMapCoordinates(user);
        var destination = GetCoordinates(args.Destination);
        if (GetEntity(args.KickTarget) is { } target && !TerminatingOrDeleted(target))
            destination = Transform(target).Coordinates;

        if (!destination.IsValid(EntityManager))
            return;

        var to = _transform.ToMapCoordinates(destination);
        var direction = to.Position - from.Position;
        if (from.MapId == MapId.Nullspace || from.MapId != to.MapId || direction.LengthSquared() < 0.01f)
            return;

        if (direction.LengthSquared() > module.Comp.Range * module.Comp.Range)
            direction = Vector2.Normalize(direction) * module.Comp.Range;

        var kicking = AddComp<ModPowerKickingComponent>(user);
        kicking.Module = module;
        if (!_throwing.TryThrow(user, direction, module.Comp.Speed, user, pushbackRatio: 0f,
                recoil: false, animated: false, playSound: false, doSpin: false, predicted: false))
        {
            RemComp(user, kicking);
            return;
        }

        args.Handled = true;
        _audio.PlayPvs(module.Comp.LaunchSound, user);
    }

    private bool CanFinishPowerKick(EntityUid module, EntityUid user)
    {
        if (!TryGetInstalledModuleSuit(module, out var suit) || suit.Comp.Wearer != user
            || !suit.Comp.Powered || suit.Comp.State != ModSuitState.Active || _gravity.IsWeightless(user)
            || !_actionBlocker.CanInteract(user, suit) || _containers.IsEntityInContainer(user))
            return false;

        foreach (var slot in Comp<ModModuleComponent>(module).RequiredParts)
        {
            if (!IsPartDeployed(suit, slot))
                return false;
        }

        return true;
    }

    private void OnPowerKickHit(Entity<ModPowerKickingComponent> wearer, ref ThrowDoHitEvent args)
    {
        if (!TryComp(wearer.Comp.Module, out ModPowerKickModuleComponent? kick)
            || !CanFinishPowerKick(wearer.Comp.Module, wearer))
            return;

        RemCompDeferred(wearer.Owner, wearer.Comp);
        wearer.Comp.Module = EntityUid.Invalid;
        _kickDamage.TryChangeDamage(args.Target, kick.Damage, origin: wearer);
        if (HasComp<MobStateComponent>(args.Target))
            _adrenalineStun.TryKnockdown(args.Target, kick.KnockdownTime);
        _audio.PlayPvs(kick.HitSound, args.Target);
    }

    private void OnPowerKickStopped(Entity<ModPowerKickingComponent> wearer, ref StopThrowEvent args) =>
        RemCompDeferred(wearer.Owner, wearer.Comp);
}
