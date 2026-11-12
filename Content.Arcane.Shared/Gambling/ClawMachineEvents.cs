using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Gambling;

[Serializable, NetSerializable]
public sealed partial class ClawMachineDoAfterEvent : DoAfterEvent
{
    [DataField] public int Operation;
    public override DoAfterEvent Clone() => (ClawMachineDoAfterEvent) MemberwiseClone();
}

[Serializable, NetSerializable]
public enum ClawMachineVisuals : byte
{
    Spinning,
    NormalSprite,
}
