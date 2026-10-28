// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;

namespace Content.Arcane.Shared.Power;

public sealed partial class MegaCellSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MegaCellComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
    }

    private void OnInsertAttempt(EntityUid uid, MegaCellComponent comp, ContainerGettingInsertedAttemptEvent args)
    {
        var owner = args.Container.Owner;
        if (TryComp<PowerCellSlotComponent>(owner, out var slot)
            && slot.CellSlotId == args.Container.ID
            && !HasComp<MegaCellSlotComponent>(owner))
            args.Cancel();
    }
}
