// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Metabolism;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Arcane.Server.Feroxi;

public sealed partial class FeroxiDehydrateSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SatiationSystem _satiation = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<FeroxiDehydrateComponent, SatiationComponent>();

        while (query.MoveNext(out var uid, out var feroxiDehydrate, out var satiation))
        {
            if (_satiation.GetValueOrNull((uid, satiation), SatiationSystem.Thirst) is not { } currentThirst)
                continue;

            var shouldBeDehydrated = currentThirst <= feroxiDehydrate.DehydrationThreshold;

            if (feroxiDehydrate.Dehydrated != shouldBeDehydrated)
                UpdateDehydrationStatus((uid, feroxiDehydrate), shouldBeDehydrated);
        }
    }

    /// <summary>
    /// Swaps the metabolizer type of every lung when a dehydration threshold is crossed.
    /// </summary>
    private void UpdateDehydrationStatus(Entity<FeroxiDehydrateComponent> ent, bool shouldBeDehydrated)
    {
        if (ent.Comp.Dehydrated == shouldBeDehydrated)
            return;

        ent.Comp.Dehydrated = shouldBeDehydrated;

        foreach (var lung in _body.GetOrgans<LungComponent>(ent.Owner))
        {
            if (!TryComp<MetabolizerComponent>(lung, out var metabolizer) || metabolizer.MetabolizerTypes == null)
                continue;

            var newMetabolizer = shouldBeDehydrated ? ent.Comp.DehydratedMetabolizer : ent.Comp.HydratedMetabolizer;
            metabolizer.MetabolizerTypes.Clear();
            metabolizer.MetabolizerTypes.Add(newMetabolizer);
            Dirty(lung, metabolizer);
        }
    }
}
