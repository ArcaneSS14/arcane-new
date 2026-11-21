namespace Content.Shared.Inventory.Events;

/// <summary>Raised on the source before inventory slots are captured for a body transfer.</summary>
[ByRefEvent]
public readonly record struct InventoryTransferStartingEvent(EntityUid Target);
