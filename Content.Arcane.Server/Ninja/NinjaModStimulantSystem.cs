using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Ninja;

public sealed partial class NinjaModStimulantSystem : EntitySystem
{
    private static readonly ProtoId<ReagentPrototype> Fuel = "Radium";
    private static readonly ProtoId<ReagentPrototype> Stimulants = "Stimulants";
    private const string FuelSolution = "radium";
    private const int DoseFuel = 20;
    private static readonly EntProtoId Stutter = "StatusEffectStutter";

    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private BloodstreamSystem _blood = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NinjaModStimulantComponent, ModModuleUsedEvent>(OnUse);
        SubscribeLocalEvent<ModSuitComponent, ModSuitUseItemEvent>(OnRefill);
    }

    private void OnUse(Entity<NinjaModStimulantComponent> module, ref ModModuleUsedEvent args)
    {
        var user = args.User;
        if (args.Handled || _mobState.IsDead(user) || !_solutions.TryGetSolution(module.Owner, FuelSolution, out var reservoir, out var fuel))
            return;

        if (fuel.GetReagentQuantity(new ReagentId(Fuel.Id, null)) < DoseFuel)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-ninja-stimulant-empty"), user, user);
            return;
        }

        if (!_blood.TryAddToBloodstream(user, new Solution(Stimulants.Id, 5)))
            return;

        _solutions.RemoveReagent(reservoir.Value, Fuel.Id, DoseFuel);
        _status.TryRemoveStatusEffect(user, SharedStunSystem.StunId);
        _stamina.TakeStaminaDamage(user, -200);
        _stun.SetKnockdownTime(user, TimeSpan.Zero);
        _stun.TryStanding(user);
        _status.TryRemoveStatusEffect(user, Stutter);
        args.Handled = true;

        // The stun status releases its movement blocker at the end of this tick.
        Timer.Spawn(0, () =>
        {
            if (!TerminatingOrDeleted(user))
                _stun.TryStanding(user);
        });

        // Reserve this dose's residue now so refills cannot change the later injection.
        var residue = new Solution(Fuel.Id, 5);
        Timer.Spawn(TimeSpan.FromSeconds(7), () =>
        {
            if (!TerminatingOrDeleted(user) && _blood.TryAddToBloodstream(user, residue))
                _popup.PopupEntity(Loc.GetString("modsuit-ninja-stimulant-crash"), user, user);
        });
    }

    private void OnRefill(Entity<ModSuitComponent> suit, ref ModSuitUseItemEvent args)
    {
        if (args.Handled || suit.Comp.Projected || suit.Comp.ModulesContainer is not { } modules
            || !_solutions.TryGetDrainableSolution(args.Item, out var source, out var mixture))
            return;

        var present = mixture.GetReagentQuantity(new ReagentId(Fuel.Id, null));
        if (present <= 0)
            return;

        foreach (var module in modules.ContainedEntities)
        {
            if (!HasComp<NinjaModStimulantComponent>(module)
                || !_solutions.TryGetSolution(module, FuelSolution, out var reservoir, out var solution))
                continue;

            args.Handled = true;
            var quantity = present < solution.AvailableVolume ? present : solution.AvailableVolume;
            if (quantity <= 0)
                return;

            var drained = _solutions.RemoveReagent(source.Value, Fuel.Id, quantity);
            _solutions.TryAddReagent(reservoir.Value, Fuel.Id, drained);
            _popup.PopupEntity(Loc.GetString("modsuit-ninja-stimulant-refilled"), suit, args.User);
            return;
        }
    }
}
