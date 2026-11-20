using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.WashingMachine;

[Serializable, NetSerializable]
public enum WashingMachineState : byte
{
    Idle,
    Washing,
    Broken
}
