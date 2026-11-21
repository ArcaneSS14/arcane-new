namespace Content.Arcane.Common.ModSuits;

[ByRefEvent]
public record struct ResolveEquipmentCopyItemEvent(EntityUid? Item);

[ByRefEvent]
public readonly record struct EquipmentCopyEquippedEvent(
    EntityUid Copy,
    IReadOnlySet<EntityUid> EligibleItems,
    IReadOnlySet<EntityUid> EquippedCopies);
