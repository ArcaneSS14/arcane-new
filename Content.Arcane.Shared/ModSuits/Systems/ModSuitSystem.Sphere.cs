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
}
