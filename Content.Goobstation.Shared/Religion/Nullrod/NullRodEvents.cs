// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Religion.Nullrod.Components;
using Content.Shared.DoAfter;

namespace Content.Goobstation.Shared.Religion.Nullrod;

/// <summary>
/// 	Raised on the nullrod when praying.
/// </summary>
/// <param name="User">The entity praying at the nullrod.</param>
[ByRefEvent]
public record struct AlternatePrayEvent(EntityUid User);

[Serializable, NetSerializable]
public sealed partial class AlternatePrayDoAfterEvent : SimpleDoAfterEvent;

// Arcane-Start
/// <summary>Raised on the original nullrod before an altar replaces it.</summary>
[ByRefEvent]
public record struct NullrodTransformAttemptEvent(EntityUid User)
{
    public bool Cancelled;
}

/// <summary>Raised on the original, still-existing nullrod after its replacement is initialized.</summary>
[ByRefEvent]
public readonly record struct NullrodTransformedEvent(EntityUid User, EntityUid Replacement);
// Arcane-End
