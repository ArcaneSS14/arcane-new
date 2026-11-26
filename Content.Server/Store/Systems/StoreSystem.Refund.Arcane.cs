// Arcane-Start
using Content.Server.Store.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem
{
    /// <summary>
    /// Credits a bounded partial refund and consumes the same purchase receipt used by the full-refund UI.
    /// The caller owns item eligibility and deletion; this method prevents either path paying twice.
    /// </summary>
    public bool TryRefundPurchase(EntityUid uid,
        EntityUid purchase,
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> refund,
        StoreComponent? store = null)
    {
        if (!Resolve(uid, ref store) || !Exists(purchase) || refund.Count == 0
            || !TryComp<StoreRefundComponent>(purchase, out var receipt)
            || receipt.StoreEntity != uid)
            return false;

        foreach (var (currency, amount) in refund)
        {
            if (amount <= FixedPoint2.Zero || !store.CurrencyWhitelist.Contains(currency)
                || !receipt.BalanceSpent.TryGetValue(currency, out var paid) || amount > paid)
                return false;
        }

        // Consume before the balance/UI update and before the caller queues the item for deletion.
        receipt.StoreEntity = null;
        store.BoughtEntities.Remove(purchase);
        foreach (var (currency, amount) in refund)
        {
            if (store.BalanceSpent.ContainsKey(currency))
                store.BalanceSpent[currency] -= amount;
        }

        // Store and every currency have already been resolved and validated above.
        return TryAddCurrency(refund, uid, store);
    }
}
// Arcane-End
