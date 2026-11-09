using Content.Arcane.Shared.Bitrunning.Components;
using Content.Shared.Access.Systems;
using Content.Trauma.Shared.VendingMachines;

namespace Content.Arcane.Shared.Bitrunning.Systems;

public sealed partial class BitrunningPointsSystem : EntitySystem
{
    [Dependency] private SharedIdCardSystem _idCard = default!;

    private EntityQuery<BitrunningPointsComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<BitrunningPointsComponent>();

        SubscribeLocalEvent<BitrunningPointsVendorComponent, ShopVendorBalanceEvent>(OnVendorBalance);
        SubscribeLocalEvent<BitrunningPointsVendorComponent, ShopVendorPurchaseEvent>(OnVendorPurchase);
    }

    private void OnVendorBalance(Entity<BitrunningPointsVendorComponent> ent, ref ShopVendorBalanceEvent args)
    {
        args.Balance = GetPointComp(args.User)?.Comp?.Points ?? 0;
    }

    private void OnVendorPurchase(Entity<BitrunningPointsVendorComponent> ent, ref ShopVendorPurchaseEvent args)
    {
        if (GetPointComp(args.User) is { } account && RemovePoints(account, args.Cost))
            args.Paid = true;
    }

    public Entity<BitrunningPointsComponent?>? GetPointComp(EntityUid user)
    {
        if (TryComp<BitrunningPointsComponent>(user, out var comp))
            return (user, comp);

        return TryFindIdCard(user);
    }

    public Entity<BitrunningPointsComponent?>? TryFindIdCard(EntityUid user)
    {
        if (!_idCard.TryFindIdCard(user, out var idCard))
            return null;

        // points live on the ID card, it gets the component with the first payout
        _query.TryComp(idCard, out var comp);
        return (idCard, comp);
    }

    public bool AddPoints(Entity<BitrunningPointsComponent?> ent, uint amount)
    {
        ent.Comp ??= EnsureComp<BitrunningPointsComponent>(ent);

        if (amount > uint.MaxValue - ent.Comp.Points)
            ent.Comp.Points = uint.MaxValue;
        else
            ent.Comp.Points += amount;

        Dirty(ent);
        return true;
    }

    public bool RemovePoints(Entity<BitrunningPointsComponent?> ent, uint amount)
    {
        if (!_query.Resolve(ent, ref ent.Comp) || amount > ent.Comp.Points)
            return false;

        ent.Comp.Points -= amount;
        Dirty(ent);
        return true;
    }
}
