// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Lathe;

namespace Content.Trauma.Shared.Lathe;

/// <summary>
/// Applies <see cref="LatheUpgradeComponent"/> modifiers when added to a lathe and removes it.
/// </summary>
public sealed class LatheUpgradeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheUpgradeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<LatheUpgradeComponent> ent, ref MapInitEvent args)
    {
        RemCompDeferred<LatheUpgradeComponent>(ent);

        if (!TryComp<LatheComponent>(ent, out var lathe))
            return;

        // Arcane-Edit-Start: keep the machine part multipliers in step with the base ones
        if (ent.Comp.MaterialUseMultiplier is {} matMul)
        {
            lathe.FinalMaterialMultiplier *= lathe.MaterialUseMultiplier > 0f ? matMul / lathe.MaterialUseMultiplier : 1f;
            lathe.MaterialUseMultiplier = matMul;
        }
        if (ent.Comp.TimeMultiplier is {} timeMul)
        {
            lathe.FinalTimeMultiplier *= lathe.TimeMultiplier > 0f ? timeMul / lathe.TimeMultiplier : 1f;
            lathe.TimeMultiplier = timeMul;
        }
        // Arcane-Edit-End

        Dirty(ent, lathe);
    }
}
