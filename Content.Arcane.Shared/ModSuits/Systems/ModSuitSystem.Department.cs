using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Cuffs;
using Content.Shared.SubFloor;
using Content.Trauma.Common.Carrying;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedTrayScannerSystem _trayScanner = default!;

    private void InitializeDepartmentModules()
    {
        SubscribeLocalEvent<ModQuickCarryComponent, ModifyCarryDurationEvent>(OnCarryDuration);
        SubscribeLocalEvent<ModQuickCuffComponent, ModifyCuffDurationEvent>(OnCuffDuration);
        SubscribeLocalEvent<ModShoveBlockerComponent, DisarmAttemptEvent>(OnDisarmAttempt);
        SubscribeLocalEvent<ModTrayScannerModuleComponent, ModModuleActivatedEvent>(OnTrayActivated);
        SubscribeLocalEvent<ModTrayScannerModuleComponent, ModModuleDeactivatedEvent>(OnTrayDeactivated);
    }

    private void OnCarryDuration(Entity<ModQuickCarryComponent> carrier, ref ModifyCarryDurationEvent args) =>
        args.Multiplier *= carrier.Comp.Multiplier;

    private void OnCuffDuration(Entity<ModQuickCuffComponent> user, ref ModifyCuffDurationEvent args) =>
        args.Duration *= user.Comp.Multiplier;

    private void OnDisarmAttempt(Entity<ModShoveBlockerComponent> target, ref DisarmAttemptEvent args)
    {
        if (args.DisarmerUid != target.Owner)
            args.Cancelled = true;
    }

    private void OnTrayActivated(Entity<ModTrayScannerModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (args.Wearer is not { } wearer || module.Comp.Wearer != null)
            return;

        module.Comp.Wearer = wearer;
        _trayScanner.RegisterScannerUser(wearer);
    }

    private void OnTrayDeactivated(Entity<ModTrayScannerModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        if (module.Comp.Wearer is not { } wearer)
            return;

        module.Comp.Wearer = null;
        if (!TerminatingOrDeleted(wearer))
            _trayScanner.UnregisterScannerUser(wearer);
    }
}
