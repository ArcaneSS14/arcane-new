using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Clothing;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Standing;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedJetpackSystem _jetpacks = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedMagbootsSystem _magboots = default!;

    private void InitializeMobilityModules()
    {
        SubscribeLocalEvent<ModJetpackModuleComponent, ModModuleActivateAttemptEvent>(OnJetpackAttempt);
        SubscribeLocalEvent<ModJetpackModuleComponent, ModModuleActivatedEvent>(OnJetpackActivated);
        SubscribeLocalEvent<ModJetpackModuleComponent, ModModuleDeactivatedEvent>(OnJetpackDeactivated);
        SubscribeLocalEvent<ModMouthholeModuleComponent, ModModuleActivatedEvent>(OnMouthholeActivated);
        SubscribeLocalEvent<ModMouthholeModuleComponent, ModModuleDeactivatedEvent>(OnMouthholeDeactivated);
        SubscribeLocalEvent<ModMouthholePartComponent, BypassIngestionBlockerEvent>(OnMouthholeIngestion);
        SubscribeLocalEvent<ModSpeedModuleComponent, ModModuleActivatedEvent>(OnSpeedActivated);
        SubscribeLocalEvent<ModSpeedModuleComponent, ModModuleDeactivatedEvent>(OnSpeedDeactivated);
        SubscribeLocalEvent<ModMagbootsModuleComponent, ModModuleActivatedEvent>(OnMagbootsActivated);
        SubscribeLocalEvent<ModMagbootsModuleComponent, ModModuleDeactivatedEvent>(OnMagbootsDeactivated);
    }

    private void OnJetpackAttempt(Entity<ModJetpackModuleComponent> module, ref ModModuleActivateAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (Transform(args.User).GridUid is { } grid && HasComp<GravityComponent>(grid))
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("jetpack-no-station"), args.Suit, args.User);
        }
        else if (_standing.IsDown(args.User))
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("jetpack-is-down"), args.Suit, args.User);
        }
    }

    private void OnJetpackActivated(Entity<ModJetpackModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (args.Wearer is { } wearer && TryComp(module, out JetpackComponent? jetpack))
            _jetpacks.SetEnabled(module, jetpack, true, wearer);
    }

    private void OnJetpackDeactivated(Entity<ModJetpackModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        if (TryComp(module, out JetpackComponent? jetpack) && jetpack.JetpackUser is { } wearer && !TerminatingOrDeleted(wearer))
            _jetpacks.SetEnabled(module, jetpack, false, wearer);
    }

    private void UpdateModJetpacks()
    {
        var query = EntityQueryEnumerator<ModJetpackModuleComponent, ModModuleComponent>();
        while (query.MoveNext(out var uid, out _, out var module))
        {
            if (module.Active && !HasComp<ActiveJetpackComponent>(uid) && module.Suit is { } control
                && TryComp(control, out ModSuitComponent? suit))
                SetModuleActive((control, suit), (uid, module), false);
        }
    }

    private void OnMouthholeActivated(Entity<ModMouthholeModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (GetDeployedPart(args.Suit, "head") is { } helmet)
            EnsureComp<ModMouthholePartComponent>(helmet);
    }

    private void OnMouthholeDeactivated(Entity<ModMouthholeModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        if (TryComp(args.Suit, out ModSuitComponent? suit) && suit.PartEntities.TryGetValue("head", out var helmet))
            RemComp<ModMouthholePartComponent>(helmet);
    }

    private void OnMouthholeIngestion(Entity<ModMouthholePartComponent> part, ref BypassIngestionBlockerEvent args) =>
        args.Bypass = true;

    private void OnMagbootsActivated(Entity<ModMagbootsModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (GetDeployedPart(args.Suit, "shoes") is not { } shoes)
            return;

        module.Comp.Boots = shoes;
        module.Comp.AddedMagboots = !HasComp<MagbootsComponent>(shoes);
        _magboots.SetModulePower((shoes, EnsureComp<MagbootsComponent>(shoes)), args.Wearer, true);
    }

    private void OnMagbootsDeactivated(Entity<ModMagbootsModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        if (module.Comp.Boots is not { } shoes || TerminatingOrDeleted(shoes) || !TryComp(shoes, out MagbootsComponent? boots))
            return;

        module.Comp.Boots = null;
        _magboots.SetModulePower((shoes, boots), args.Wearer, false, removeAfter: module.Comp.AddedMagboots);
    }

    private void OnSpeedActivated(Entity<ModSpeedModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (args.Wearer is { } wearer)
            _movement.RefreshMovementSpeedModifiers(wearer);
    }

    private void OnSpeedDeactivated(Entity<ModSpeedModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        if (args.Wearer is { } wearer && !TerminatingOrDeleted(wearer))
            _movement.RefreshMovementSpeedModifiers(wearer);
    }

    private void ApplyModuleSpeedModifiers(Entity<ModSuitComponent> suit, RefreshMovementSpeedModifiersEvent args)
    {
        if (suit.Comp.ModulesContainer is not { } modules)
            return;

        foreach (var uid in modules.ContainedEntities)
        {
            if (TryComp(uid, out ModModuleComponent? module) && module.Active && TryComp(uid, out ModSpeedModuleComponent? speed))
                args.ModifySpeed(speed.WalkModifier, speed.SprintModifier);
        }
    }
}
