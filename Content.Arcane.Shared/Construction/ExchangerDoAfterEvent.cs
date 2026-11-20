using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Construction;

/// <summary>
/// Raised when a rapid part exchanger finishes swapping parts in a machine.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ExchangerDoAfterEvent : SimpleDoAfterEvent;
