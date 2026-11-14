// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Item;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedChameleonClothingSystem _modChameleon = default!;

    private void InitializeArmorUtility()
    {
        SubscribeLocalEvent<ModEnergyShieldModuleComponent, ExaminedEvent>(OnShieldExamined);
        SubscribeLocalEvent<ModPlateCompressionModuleComponent, ModModuleInstalledEvent>(OnCompressionInstalled);
        SubscribeLocalEvent<ModPlateCompressionModuleComponent, ModModuleRemovedEvent>(OnCompressionRemoved);
        SubscribeLocalEvent<ModChameleonModuleComponent, ModModuleInstalledEvent>(OnChameleonInstalled);
        SubscribeLocalEvent<ModChameleonModuleComponent, ModModuleRemovedEvent>(OnChameleonRemoved);
        SubscribeLocalEvent<ModChameleonModuleComponent, ModModuleUsedEvent>(OnChameleonUsed);
    }

    private void UpdateEnergyShields()
    {
        var query = EntityQueryEnumerator<ModEnergyShieldModuleComponent>();
        while (query.MoveNext(out var uid, out var shield))
        {
            if (shield.Charges >= shield.MaxCharges || _timing.CurTime < shield.NextCharge)
                continue;

            shield.Charges++;
            shield.NextCharge = _timing.CurTime + shield.ChargeIncrement;
            Dirty(uid, shield);
        }
    }

    private void TryBlockWithShield(Entity<ModSuitComponent> suit, EntityUid module, DamageModifyEvent damage)
    {
        if (_net.IsClient || damage.Origin is not { } origin || origin == damage.Target
            || damage.Damage.GetTotal() <= 0 || !TryComp(module, out ModEnergyShieldModuleComponent? shield)
            || shield.Charges <= 0 || !TryUseModuleCharge(suit, shield.BlockPower))
            return;

        damage.Damage = new DamageSpecifier();
        shield.Charges--;
        shield.NextCharge = _timing.CurTime + shield.RechargeDelay;
        Dirty(module, shield);
        _audio.PlayPvs(shield.BlockSound, damage.Target);
        _popup.PopupEntity(Loc.GetString("modsuit-energy-shield-block"), damage.Target);
    }

    private void OnShieldExamined(EntityUid uid, ModEnergyShieldModuleComponent shield, ExaminedEvent args) =>
        args.PushMarkup(Loc.GetString("modsuit-energy-shield-examine", ("charges", shield.Charges), ("max", shield.MaxCharges)));

    private void OnCompressionInstalled(Entity<ModPlateCompressionModuleComponent> module, ref ModModuleInstalledEvent args)
    {
        if (!TryComp(args.Suit, out ItemComponent? item))
            return;

        module.Comp.OriginalSize = item.Size;
        _modItem.SetSize(args.Suit, module.Comp.Size, item);
    }

    private void OnCompressionRemoved(Entity<ModPlateCompressionModuleComponent> module, ref ModModuleRemovedEvent args)
    {
        if (module.Comp.OriginalSize is not { } size)
            return;

        module.Comp.OriginalSize = null;
        if (TerminatingOrDeleted(args.Suit))
            return;

        _modItem.SetSize(args.Suit, size);
        if (_containers.TryGetContainingContainer(args.Suit, out var container) && HasComp<StorageComponent>(container.Owner))
            _transform.DropNextTo(args.Suit, container.Owner);
    }

    private void OnChameleonInstalled(Entity<ModChameleonModuleComponent> module, ref ModModuleInstalledEvent args)
    {
        if (_net.IsClient || module.Comp.BuiltIn || HasComp<ChameleonClothingComponent>(args.Suit)
            || MetaData(args.Suit).EntityPrototype is not { } original)
            return;

        var clothing = AddComp<ChameleonClothingComponent>(args.Suit);
        _modChameleon.InitializeModChameleon(args.Suit, clothing, original.ID, Comp<ModSuitComponent>(args.Suit).Wearer);
        module.Comp.Granted = clothing;
    }

    private void OnChameleonRemoved(Entity<ModChameleonModuleComponent> module, ref ModModuleRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Suit) || module.Comp.Granted is not { } clothing
            || CompOrNull<ChameleonClothingComponent>(args.Suit) != clothing)
            return;

        module.Comp.Granted = null;
        _ui.CloseUi(args.Suit, ChameleonUiKey.Key);
        if (MetaData(args.Suit).EntityPrototype is { } original)
            _modChameleon.SetSelectedPrototype(args.Suit, original.ID, forceUpdate: true, validate: false, component: clothing);
        RemComp(args.Suit, clothing);
    }

    private void OnChameleonUsed(Entity<ModChameleonModuleComponent> module, ref ModModuleUsedEvent args)
    {
        if (args.Handled || !HasComp<ChameleonClothingComponent>(args.Suit))
            return;

        args.Handled = _ui.TryToggleUi(args.Suit, ChameleonUiKey.Key, args.User);
    }
}
