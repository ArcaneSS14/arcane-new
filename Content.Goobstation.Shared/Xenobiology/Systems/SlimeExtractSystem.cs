// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Examine;

// Arcane-Start
using Content.Shared.EntityEffects;
using Robust.Shared.Timing;
// Arcane-End

namespace Content.Goobstation.Shared.Xenobiology.Systems;

// This handles slime extracts.
public sealed partial class SlimeExtractSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    // Arcane-Start
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private INetManager _net = default!;
    // Arcane-End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeExtractComponent, ReactiveEffectsApplyingEvent>(OnApplyingEffects); // Arcane-Edit
        SubscribeLocalEvent<SlimeExtractComponent, ExaminedEvent>(OnExamined);
    }

    // Arcane-Edit-Start: Old Arcane #386/#533, reserve the extract before scheduling the reaction.
    private void OnApplyingEffects(Entity<SlimeExtractComponent> ent, ref ReactiveEffectsApplyingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (!_net.IsServer || ent.Comp.Used)
            return;

        ent.Comp.Used = true;
        Dirty(ent);
        if (_solution.TryGetRefillableSolution(ent.Owner, out var soln, out _))
            _solution.RemoveAllSolution((ent.Owner, soln));

        var effects = args.Effects;
        var scale = args.Scale;
        Timer.Spawn(ent.Comp.ReactionDelay, () =>
        {
            if (!TerminatingOrDeleted(ent) && TryComp<SlimeExtractComponent>(ent, out var current) && current == ent.Comp)
                _effects.ApplyEffects(ent, effects, scale);
        });
    }
    // Arcane-Edit-End

    private void OnExamined(Entity<SlimeExtractComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Used || !HasComp<ReactiveComponent>(ent)) // Arcane-Edit: Also check whether the reaction was scheduled
            args.PushMarkup(Loc.GetString("xeno-extract-reaction-unreactive"));
    }
}
