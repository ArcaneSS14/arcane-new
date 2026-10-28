// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Cuffs.Components;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.HealthExaminable;

namespace Content.Arcane.Shared.Examine;

/// <summary>
/// Shows cuffed hands on examine (Orion #221) and eye damage on health examine (Orion #374).
/// </summary>
public sealed partial class ConditionExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CuffableComponent, ExaminedEvent>(OnCuffableExamined);
        SubscribeLocalEvent<BlindableComponent, HealthBeingExaminedEvent>(OnHealthExamined);
    }

    private void OnCuffableExamined(Entity<CuffableComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.CanStillInteract)
            args.PushMarkup(Loc.GetString("examine-handcuffed", ("ent", ent.Owner)));
    }

    private void OnHealthExamined(EntityUid uid, BlindableComponent comp, HealthBeingExaminedEvent args)
    {
        if (comp.EyeDamage <= 0)
            return;

        args.Message.PushNewline();
        args.Message.AddMarkupOrThrow(Loc.GetString("blindable-component-eye-damage", ("target", uid)));
    }
}
