using Content.Arcane.Shared.Faoli.Components;
using Content.Shared.Alert.Components;

namespace Content.Arcane.Client.Faoli;

public sealed class RevenantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FaoliComponent, GetGenericAlertCounterAmountEvent>(OnGetCounterAmount);
    }

    private void OnGetCounterAmount(Entity<FaoliComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.FaoliAlert != args.Alert)
            return;

        args.Amount = ent.Comp.Faoli.Int();
    }
}
