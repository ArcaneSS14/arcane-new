// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Item;
using Content.Shared.Storage.Components;
using Robust.Shared.Map;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// Arcane - requester names from IDs, delivery destinations and secured department crates (Orion #377).
/// </summary>
public sealed partial class CargoSystem
{
    [Dependency] private EntityStorageSystem _entityStorage = default!;

    private const int MaxOrderTextLength = 64;

    /// <summary>
    /// The orderer's ID name, or the typed one on an emagged console.
    /// </summary>
    private string GetRequester(Entity<CargoOrderConsoleComponent> console, CargoConsoleAddOrderMessage args)
    {
        if (console.Comp.EditableRequesterName && !string.IsNullOrWhiteSpace(args.Requester))
            return Truncate(args.Requester);

        return GenerateRequesterName(args.Actor);
    }

    /// <summary>
    /// Copies the destination and secured delivery choice of a new order, dropping secured delivery when not allowed.
    /// </summary>
    private void SetOrderExtras(CargoOrderData data, Entity<CargoOrderConsoleComponent> console, CargoConsoleAddOrderMessage args, CargoProductPrototype product)
    {
        data.DeliveryDestination = string.IsNullOrWhiteSpace(args.DeliveryDestination) ? null : Truncate(args.DeliveryDestination);
        data.SecuredDelivery = args.SecuredDelivery
            && ProtoMan.Resolve(product.Product, out var productProto)
            && CanBeSecuredDelivery(console, productProto);
        data.ExtraCost = data.SecuredDelivery ? console.Comp.SecureOrderCost : 0;
    }

    /// <summary>
    /// Puts an item, or the contents of a crate, into the account's secure crate.
    /// </summary>
    private EntityUid PackSecureDelivery(CargoOrderData order, CargoAccountPrototype account, EntityUid item, EntityCoordinates spawn)
    {
        if (!order.SecuredDelivery || account.SecureCratePrototype is not { } crateProto)
            return item;

        var crate = Spawn(crateProto, spawn);

        if (TryComp<EntityStorageComponent>(item, out var storage))
        {
            foreach (var content in storage.Contents.ContainedEntities.ToArray())
            {
                if (!_entityStorage.Insert(content, crate))
                    return item; // crate is full, keep the rest in the original
            }

            QueueDel(item);
            return crate;
        }

        if (HasComp<ItemComponent>(item) && _entityStorage.Insert(item, crate))
            return crate;

        QueueDel(crate);
        return item;
    }

    private static string Truncate(string text)
    {
        text = text.Trim();
        return text.Length > MaxOrderTextLength ? text[..MaxOrderTextLength] : text;
    }
}
