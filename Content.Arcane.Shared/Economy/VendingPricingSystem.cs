using Content.Arcane.Common.Economy;
using Content.Shared.Access.Systems;
using Content.Shared.Station.Systems;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Economy;

/// <summary>
///     Prices of vending machine items and personal balances, shared so clients show what the server charges.
/// </summary>
public sealed partial class VendingPricingSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VendingMachineComponent, VendingItemPriceEvent>(OnGetPrice);
        SubscribeLocalEvent<BankBalanceQueryEvent>(OnBalanceQuery);
    }

    private void OnGetPrice(Entity<VendingMachineComponent> ent, ref VendingItemPriceEvent args)
    {
        args.Price = GetPrice(ent, args.User, args.ItemId, (InventoryType) args.InventoryType);
    }

    private void OnBalanceQuery(ref BankBalanceQueryEvent args)
    {
        if (TryGetCard(args.User, out var card))
            args.Balance = card.Comp.Balance;
    }

    /// <summary>
    ///     Gets the price of an item, machines off the station sell for free.
    /// </summary>
    public int GetPrice(Entity<VendingMachineComponent> ent, EntityUid? user, string itemId, InventoryType type)
    {
        if (!_proto.TryIndex<VendingPricesPrototype>(ent.Comp.PackPrototypeId.Id, out var prices)
            || _station.GetOwningStation(ent) == null)
            return 0;

        var price = type switch
        {
            InventoryType.Contraband => prices.ContrabandPrices.GetValueOrDefault(itemId, prices.ExtraPrice),
            InventoryType.Emagged => prices.EmaggedPrices.GetValueOrDefault(itemId, prices.ExtraPrice),
            _ => prices.Prices.GetValueOrDefault(itemId, prices.DefaultPrice),
        };

        if (price <= 0 || type != InventoryType.Regular || prices.DiscountDepartment is not { } department
            || user == null || !TryGetCard(user.Value, out var card) || card.Comp.Department != department)
            return Math.Max(price, 0);

        return Math.Max((int) Math.Round(price * prices.DepartmentDiscount, MidpointRounding.AwayFromZero), 1);
    }

    /// <summary>
    ///     Finds the ID card of a user that is linked to a bank account.
    /// </summary>
    public bool TryGetCard(EntityUid user, out Entity<BankCardComponent> card)
    {
        card = default;
        if (!_idCard.TryFindIdCard(user, out var idCard)
            || !TryComp<BankCardComponent>(idCard, out var bankCard)
            || bankCard.AccountId == null)
            return false;

        card = (idCard, bankCard);
        return true;
    }
}
