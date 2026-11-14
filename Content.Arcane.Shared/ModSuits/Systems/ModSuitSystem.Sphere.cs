using Content.Arcane.Shared.ModSuits.Components;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    internal bool TryGetSphereModuleSuit(EntityUid? uid, out Entity<ModSuitComponent> suit)
    {
        if (uid is { } id && TryComp(id, out ModModuleComponent? module)
            && module.Suit is { } control
            && TryComp(control, out ModSuitComponent? component)
            && component.ModulesContainer?.Contains(id) == true)
        {
            suit = (control, component);
            return true;
        }

        suit = default;
        return false;
    }

    internal bool DeactivateSphereModule(EntityUid? uid)
    {
        if (uid is not { } id || !TryGetSphereModuleSuit(id, out var suit)
            || !TryComp(id, out ModModuleComponent? module) || !module.Active)
            return false;

        SetModuleActive(suit, (id, module), false);
        return true;
    }

    internal bool HasSphereCharge(Entity<ModSuitComponent> suit, float amount) =>
        GetBattery(suit) is { } battery && _battery.GetCharge(battery.AsNullable()) >= amount;

    internal bool TryUseSphereCharge(Entity<ModSuitComponent> suit, float amount) =>
        GetBattery(suit) is { } battery && _battery.TryUseCharge(battery.AsNullable(), amount);
}
