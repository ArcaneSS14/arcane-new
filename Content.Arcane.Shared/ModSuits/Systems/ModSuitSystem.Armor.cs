using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    private void InitializeArmorModules()
    {
        SubscribeLocalEvent<ModPartComponent, ArmorModifiersQueryEvent>(OnPartArmorModifiers);
        SubscribeLocalEvent<ModSuitComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnSuitDamageModify);
    }

    private void OnPartArmorModifiers(Entity<ModPartComponent> part, ref ArmorModifiersQueryEvent args)
    {
        if (part.Comp.Suit is not { } control
            || GetDeployedPart(control, part.Comp.Slot) != part.Owner
            || !TryComp(control, out ModSuitComponent? suit)
            || suit.ModulesContainer is not { } modules)
            return;

        DamageModifierSet? effective = null;
        foreach (var uid in modules.ContainedEntities)
        {
            if (!TryComp(uid, out ModArmorModuleComponent? armor) || !armor.Slots.Contains(part.Comp.Slot)
                || !TryComp(uid, out ModModuleComponent? module) || !module.Active)
                continue;

            effective ??= new DamageModifierSet
            {
                Coefficients = new(args.Modifiers.Coefficients),
                FlatReductions = new(args.Modifiers.FlatReductions),
                IgnoreArmorPierceFlags = args.Modifiers.IgnoreArmorPierceFlags,
            };

            foreach (var (type, coefficient) in armor.Modifiers.Coefficients)
                effective.Coefficients[type] = effective.Coefficients.GetValueOrDefault(type, 1f) * coefficient;

            foreach (var (type, reduction) in armor.Modifiers.FlatReductions)
                effective.FlatReductions[type] = effective.FlatReductions.GetValueOrDefault(type) + reduction;
        }

        if (effective != null)
            args.Modifiers = effective;
    }

    private void OnSuitDamageModify(Entity<ModSuitComponent> suit, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (suit.Comp.Wearer != args.Owner || suit.Comp.ModulesContainer is not { } modules)
            return;

        foreach (var uid in modules.ContainedEntities)
        {
            if (TryComp(uid, out ModArmorModuleComponent? armor) && armor.Slots.Count == 0
                && TryComp(uid, out ModModuleComponent? module) && module.Active)
                args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, armor.Modifiers);
        }
    }

    public bool IsPartPressureProtectionSuppressed(Entity<ModPartComponent> part)
    {
        if (part.Comp.Suit is not { } control || GetDeployedPart(control, part.Comp.Slot) != part.Owner
            || !TryComp(control, out ModSuitComponent? suit) || suit.ModulesContainer is not { } modules)
            return false;

        foreach (var uid in modules.ContainedEntities)
        {
            if (HasComp<ModArmorBoosterModuleComponent>(uid) && TryComp(uid, out ModModuleComponent? module) && module.Active)
                return true;
        }

        return false;
    }
}
