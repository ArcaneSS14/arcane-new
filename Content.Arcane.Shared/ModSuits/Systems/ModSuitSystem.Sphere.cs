using Content.Arcane.Shared.ModSuits.Components;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    internal bool DeactivateSphereModule(EntityUid? uid)
    {
        if (uid is not { } id || !TryGetInstalledModuleSuit(id, out var suit)
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
