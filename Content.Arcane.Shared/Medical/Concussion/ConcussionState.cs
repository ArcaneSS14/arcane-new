using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Medical.Concussion;

[Serializable, NetSerializable]
public enum ConcussionState : byte
{
    Invalid,
    Sane,
    Minor,
    Hard,
    Overwhelmed,
}

[ByRefEvent]
public readonly record struct ConcussionStateChangedEvent(ConcussionState OldState, ConcussionState NewState);

[ByRefEvent]
public record struct BeforeConcussionDamageEvent(FixedPoint2 Damage, bool Cancelled = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;
}
