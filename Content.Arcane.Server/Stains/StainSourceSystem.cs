using Content.Arcane.Shared.Stains;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Inventory;
using Content.Shared.Slippery;
using Content.Shared.Standing;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Map.Components;

namespace Content.Arcane.Server.Stains;

/// <summary>
/// Finds the moments clothes get stained and relays them to what the mob wears: slipping on a puddle, a solution
/// touching the mob, crawling over a puddle, and stepping into one (shoes only).
/// </summary>
public sealed partial class StainSourceSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private SharedMapSystem _map = default!;

    /// <summary>
    /// How much of a puddle soaks into the shoes per step.
    /// </summary>
    private static readonly FixedPoint2 StepTransfer = FixedPoint2.New(1);

    /// <summary>
    /// How much of a puddle touches a mob crawling over it.
    /// </summary>
    private const float CrawlTransferFraction = 0.15f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StainSourceComponent, SlipEvent>(OnSlip);
        SubscribeLocalEvent<InventoryComponent, ReactionEntityEvent>(OnTouched);
        SubscribeLocalEvent<StandingStateComponent, MoveEvent>(OnCrawl);
        SubscribeLocalEvent<StainSourceComponent, StepTriggeredOnEvent>(OnSteppedOn);
    }

    private void OnSlip(Entity<StainSourceComponent> ent, ref SlipEvent args)
    {
        if (!TryComp<InventoryComponent>(args.Slipped, out var inventory) || !TryGetPuddleSolution(ent.Owner, out var source))
            return;

        _inventory.RelayEvent((args.Slipped, inventory), new StainSlippedEvent(source.Value));
        _solution.UpdateChemicals(source.Value);
    }

    private void OnTouched(Entity<InventoryComponent> ent, ref ReactionEntityEvent args)
    {
        if (args.Method != ReactionMethod.Touch || args.ReagentQuantity.Quantity <= FixedPoint2.Zero)
            return;

        var solution = new Solution();
        solution.AddReagent(args.ReagentQuantity.Reagent, args.ReagentQuantity.Quantity);
        _inventory.RelayEvent(ent, new StainSpilledOnEvent(solution));
    }

    private void OnCrawl(Entity<StandingStateComponent> ent, ref MoveEvent args)
    {
        if (!_standing.IsDown(ent.Owner) || !TryComp<InventoryComponent>(ent, out var inventory))
            return;

        if (!TryGetNewTilePuddle(args, out var puddle) || !TryGetPuddleSolution(puddle, out var source))
            return;

        var touched = _solution.SplitSolution(source.Value, source.Value.Comp.Solution.Volume * CrawlTransferFraction);
        _inventory.RelayEvent((ent.Owner, inventory), new StainSpilledOnEvent(touched));
        if (touched.Volume > FixedPoint2.Zero)
            _solution.TryAddSolution(source.Value, touched);
    }

    private void OnSteppedOn(Entity<StainSourceComponent> ent, ref StepTriggeredOnEvent args)
    {
        if (_standing.IsDown(args.Tripper) || !_inventory.TryGetSlotEntity(args.Tripper, "shoes", out var shoes))
            return;

        if (!TryGetPuddleSolution(ent.Owner, out var source) || source.Value.Comp.Solution.Volume <= FixedPoint2.Zero)
            return;

        var touched = _solution.SplitSolution(source.Value, FixedPoint2.Min(StepTransfer, source.Value.Comp.Solution.Volume));
        var relayed = new InventoryRelayedEvent<StainSpilledOnEvent>(new StainSpilledOnEvent(touched), args.Tripper);
        RaiseLocalEvent(shoes.Value, relayed);
        if (touched.Volume > FixedPoint2.Zero)
            _solution.TryAddSolution(source.Value, touched);
    }

    private bool TryGetPuddleSolution(EntityUid uid, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Entity<SolutionComponent>? solution)
    {
        solution = null;
        return TryComp<PuddleComponent>(uid, out var puddle) && _solution.TryGetSolution(uid, puddle.SolutionName, out solution);
    }

    private bool TryGetNewTilePuddle(MoveEvent args, out EntityUid puddle)
    {
        puddle = default;
        var gridUid = _transform.GetGrid(args.NewPosition);
        if (gridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp) || _transform.GetGrid(args.OldPosition) != grid)
            return false;

        if (_map.CoordinatesToTile(grid, gridComp, args.OldPosition) == _map.CoordinatesToTile(grid, gridComp, args.NewPosition))
            return false;

        return _puddle.TryGetPuddle(_map.GetTileRef(grid, gridComp, args.NewPosition), out puddle);
    }

    [Dependency] private SharedTransformSystem _transform = default!;
}
