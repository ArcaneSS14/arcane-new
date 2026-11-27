using Content.Arcane.Shared.OfferItem;
using Content.Shared.Alert;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Arcane.Server.OfferItem;

public sealed partial class OfferItemSystem : SharedOfferItemSystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private AlertsSystem _alertsSystem = default!;

    private float _offerAcc = 0;
    private const float OfferAccMax = 3f;

    public override void Update(float frameTime)
    {
        _offerAcc += frameTime;

        if (_offerAcc >= OfferAccMax)
            _offerAcc -= OfferAccMax;
        else
            return;

        var query = EntityQueryEnumerator<OfferItemComponent, HandsComponent>();
        while (query.MoveNext(out var uid, out var offerItem, out var hands))
        {
            // TODO implement a normal fix. More info on this:
            // https://github.com/space-syndicate/space-station-14-next/blob/27f6125c828b5ad051f67a5f557bf67bd1d3c2be/Content.Server/_CorvaxNext/OfferItem/OfferItemSystem.cs#L31
            if (offerItem.Hand is not null &&
                (offerItem.Item is null || _hands.GetHeldItem((uid, hands), offerItem.Hand) != offerItem.Item))
            {
                UnOffer(uid, offerItem);
                continue;
            }

            if (!offerItem.IsInReceiveMode)
            {
                _alertsSystem.ClearAlert(uid, OfferAlert);
                continue;
            }

            _alertsSystem.ShowAlert(uid, OfferAlert);
        }
    }
}
