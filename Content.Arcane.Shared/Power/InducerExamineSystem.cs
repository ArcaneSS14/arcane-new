// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;

namespace Content.Arcane.Shared.Power;

public sealed partial class InducerExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InducerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<InducerComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString("inducer-examine-rate", ("rate", ent.Comp.TransferRate)));
    }
}
