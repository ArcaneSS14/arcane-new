using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Trauma.Common.Projectiles;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModShootingAssistantSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private ModSuitSystem _suits = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedGunSystem _guns = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    [SubscribeLocalEvent]
    private void OnWorn(Entity<ModShootingAssistantModuleComponent> module, ref ModModuleWornEvent args)
    {
        if (_actions.AddAction(args.Wearer, ref module.Comp.CycleActionEntity, module.Comp.CycleAction, module))
            _actions.SetEntityIcon(module.Comp.CycleActionEntity.Value, module);
    }

    [SubscribeLocalEvent]
    private void OnActivated(Entity<ModShootingAssistantModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (_net.IsClient || args.Wearer is not { } wearer)
            return;

        var assistant = EnsureComp<ModShootingAssistantComponent>(wearer);
        assistant.Module = module;
        Dirty(wearer, assistant);
        RefreshShooter(wearer);
    }

    [SubscribeLocalEvent]
    private void OnDeactivated(Entity<ModShootingAssistantModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        if (_net.IsClient || args.Wearer is not { } wearer || TerminatingOrDeleted(wearer)
            || !TryComp<ModShootingAssistantComponent>(wearer, out var assistant) || assistant.Module != module.Owner)
            return;

        RemComp(wearer, assistant);
        RefreshShooter(wearer);
    }

    [SubscribeLocalEvent]
    private void OnCycle(Entity<ModShootingAssistantModuleComponent> module, ref ModShootingModeActionEvent args)
    {
        if (_net.IsClient || args.Handled || !_suits.TryGetInstalledModuleSuit(module, out var suit)
            || suit.Comp.Wearer != args.Performer)
            return;

        args.Handled = true;
        module.Comp.Mode = module.Comp.Mode switch
        {
            ModShootingMode.Off => ModShootingMode.Stormtrooper,
            ModShootingMode.Stormtrooper => ModShootingMode.Sharpshooter,
            _ => ModShootingMode.Off,
        };
        Dirty(module);
        RefreshShooter(args.Performer);

        var text = module.Comp.Mode switch
        {
            ModShootingMode.Stormtrooper => "modsuit-shooting-mode-stormtrooper",
            ModShootingMode.Sharpshooter => "modsuit-shooting-mode-sharpshooter",
            _ => "modsuit-shooting-mode-off",
        };
        _popup.PopupEntity(Loc.GetString(text), args.Performer, args.Performer);
    }

    private void RefreshShooter(EntityUid wearer)
    {
        _movement.RefreshMovementSpeedModifiers(wearer);
        foreach (var item in _hands.EnumerateHeld(wearer))
        {
            if (TryComp<GunComponent>(item, out var gun))
                _guns.RefreshModifiers((item, gun), wearer);
        }
    }

    [SubscribeLocalEvent]
    private void OnGunPickedUp(Entity<GunComponent> gun, ref GotEquippedHandEvent args)
    {
        if (HasComp<ModShootingAssistantComponent>(args.User))
            _guns.RefreshModifiers(gun.AsNullable(), args.User);
    }

    [SubscribeLocalEvent(after: [typeof(SharedWieldableSystem)])]
    private void OnGunDropped(Entity<GunComponent> gun, ref GotUnequippedHandEvent args)
    {
        if (HasComp<ModShootingAssistantComponent>(args.User))
            _guns.RefreshModifiers(gun.AsNullable());
    }

    [SubscribeLocalEvent(after: [typeof(SharedWieldableSystem)])]
    private void OnGunRefresh(Entity<GunComponent> gun, ref GunRefreshModifiersEvent args)
    {
        var user = args.User ?? Transform(gun).ParentUid;
        if (!_hands.IsHolding(user, gun) || !TryGetAssistant(user, out var module))
            return;

        switch (module.Mode)
        {
            case ModShootingMode.Stormtrooper:
                args.FireRate *= module.StormtrooperFireRate;
                args.MinAngle += Angle.FromDegrees(module.StormtrooperMinSpread);
                args.MaxAngle += Angle.FromDegrees(module.StormtrooperMaxSpread);
                break;
            case ModShootingMode.Sharpshooter:
                args.MinAngle *= module.SharpshooterSpreadMultiplier;
                args.MaxAngle *= module.SharpshooterSpreadMultiplier;
                args.AngleIncrease *= module.SharpshooterSpreadMultiplier;
                args.ProjectileSpeed *= module.SharpshooterProjectileSpeed;
                break;
        }
    }

    [SubscribeLocalEvent]
    private void OnMovement(Entity<ModShootingAssistantComponent> assistant, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (TryGetAssistant(assistant, out var module) && module.Mode == ModShootingMode.Sharpshooter)
            args.ModifySpeed(module.SharpshooterMoveSpeed, module.SharpshooterMoveSpeed);
    }

    [SubscribeLocalEvent]
    private void OnProjectile(ref PlayerShotProjectileEvent args)
    {
        if (!TryGetAssistant(args.User, out var module) || module.Mode != ModShootingMode.Sharpshooter
            || !TryComp<ProjectileComponent>(args.Projectile, out var projectile)
            || !HasComp<GunComponent>(projectile.Weapon) || !_hands.IsHolding(args.User, projectile.Weapon))
            return;

        projectile.Damage *= module.SharpshooterDamage;
        Dirty(args.Projectile, projectile);
    }

    private bool TryGetAssistant(EntityUid wearer, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]
        out ModShootingAssistantModuleComponent? module)
    {
        module = null;
        return TryComp<ModShootingAssistantComponent>(wearer, out var assistant)
            && _suits.TryGetInstalledModuleSuit(assistant.Module, out var suit) && suit.Comp.Wearer == wearer
            && _suits.IsPartDeployed(suit, "gloves")
            && TryComp(assistant.Module, out module);
    }
}
