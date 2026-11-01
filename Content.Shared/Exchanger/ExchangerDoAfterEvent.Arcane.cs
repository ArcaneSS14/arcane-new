// Arcane-Start
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Exchanger;

/// <summary>
/// Raised when a rapid part exchanger finishes swapping parts in a machine.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ExchangerDoAfterEvent : SimpleDoAfterEvent;
// Arcane-End
