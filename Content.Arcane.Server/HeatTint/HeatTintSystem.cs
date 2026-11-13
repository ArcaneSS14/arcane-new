using Content.Arcane.Shared.HeatTint;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;

namespace Content.Arcane.Server.HeatTint;

public sealed partial class HeatTintSystem : SharedHeatTintSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeatTintComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HeatTintComponent, TemperatureChangedEvent>(OnTemperatureChanged);
        SubscribeLocalEvent<HeatTintComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnMapInit(Entity<HeatTintComponent> ent, ref MapInitEvent args)
    {
        if (_solutions.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
            SetTemperature(ent, solution.Temperature);
        else if (TryComp<TemperatureComponent>(ent, out var temperature))
            SetTemperature(ent, temperature.Temperature);
    }

    private void OnTemperatureChanged(Entity<HeatTintComponent> ent, ref TemperatureChangedEvent args)
    {
        // Object temperature and the food solution are independent in Trauma.
        if (_solutions.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
            SetTemperature(ent, solution.Temperature);
        else
            SetTemperature(ent, args.CurrentTemperature);
    }

    private void OnSolutionChanged(Entity<HeatTintComponent> ent, ref SolutionChangedEvent args)
    {
        if (_solutions.TryGetSolution(ent.Owner, ent.Comp.Solution, out var selected, out var solution) &&
            selected.Value.Owner == args.Solution.Owner)
            SetTemperature(ent, solution.Temperature);
    }

    private void SetTemperature(EntityUid ent, float temperature)
        => _appearance.SetData(ent, HeatTintVisuals.Temperature, temperature);
}
