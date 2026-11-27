using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.WashingMachine.Events;

[Serializable, NetSerializable]
public sealed partial class EnterWashingMachineDoAfterEvent : SimpleDoAfterEvent
{
}
