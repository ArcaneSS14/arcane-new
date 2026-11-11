// <Trauma>
using Content.Shared.Damage;
// </Trauma>
using System.Numerics;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Reflect;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Shot may be reflected by setting <see cref="Reflected"/> to true
/// and changing <see cref="Direction"/> where shot will go next
/// </summary>
[ByRefEvent]
// Trauma - add Damage and Target
public record struct HitScanReflectAttemptEvent(EntityUid? Shooter, EntityUid SourceItem, ReflectType Reflective, Vector2 Direction, bool Reflected, DamageSpecifier? Damage, EntityUid Target) : IInventoryRelayEvent
{
    // Arcane-Start
    /// <summary>
    /// With <see cref="Reflected"/>, the shot ignores the target and carries on along its path instead of bouncing.
    /// </summary>
    public bool PassThrough;
    // Arcane-End

    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}
