using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Emp;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedEmpSystem _emp = default!;

    private void InitializeEmpModules()
    {
        SubscribeLocalEvent<ModSuitComponent, EmpAttemptEvent>(OnSuitEmpAttempt);
        SubscribeLocalEvent<ModSuitComponent, ContainedEmpAttemptEvent>(OnSuitContainedEmpAttempt);
        SubscribeLocalEvent<ModEmpWardComponent, EmpAttemptEvent>(OnWardEmpAttempt);
        SubscribeLocalEvent<ModEmpWardComponent, ContainedEmpAttemptEvent>(OnWardContainedEmpAttempt);
        SubscribeLocalEvent<ModEmpPulseModuleComponent, ModModuleUsedEvent>(OnEmpPulseUsed);
    }

    private void OnSuitEmpAttempt(Entity<ModSuitComponent> suit, ref EmpAttemptEvent args) =>
        args.Cancelled |= IsEmpShielded(suit);

    private void OnSuitContainedEmpAttempt(Entity<ModSuitComponent> suit, ref ContainedEmpAttemptEvent args)
    {
        if (!args.Cancelled && (args.Target == GetCore(suit) || args.Target == GetBattery(suit)?.Owner))
            args.Cancelled = IsEmpShielded(suit);
    }

    private bool IsEmpShielded(Entity<ModSuitComponent> suit)
    {
        if (suit.Comp.ModulesContainer is not { } modules)
            return false;

        foreach (var uid in modules.ContainedEntities)
        {
            if (TryComp(uid, out ModEmpShieldModuleComponent? shield) && TryComp(uid, out ModModuleComponent? module)
                && (!shield.RequireActive || module.Active))
                return true;
        }

        return false;
    }

    private void OnWardEmpAttempt(Entity<ModEmpWardComponent> wearer, ref EmpAttemptEvent args) =>
        args.Cancelled = true;

    private void OnWardContainedEmpAttempt(Entity<ModEmpWardComponent> wearer, ref ContainedEmpAttemptEvent args) =>
        args.Cancelled = true;

    private void OnEmpPulseUsed(Entity<ModEmpPulseModuleComponent> module, ref ModModuleUsedEvent args)
    {
        _emp.EmpPulse(Transform(args.User).Coordinates, module.Comp.Range, module.Comp.EnergyConsumption,
            module.Comp.Duration, args.User, predicted: false);
        args.Handled = true;
    }
}
