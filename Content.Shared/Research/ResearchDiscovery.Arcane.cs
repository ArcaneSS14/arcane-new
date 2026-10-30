// Arcane-Start
using Robust.Shared.Serialization;

namespace Content.Shared.Research;

[Serializable, NetSerializable]
public enum ResearchDiscoveryEventType : byte
{
    ScanEntity,
    MachineInsertion,
    DeconstructEntity,
    ServerTrigger,
}

[Serializable, NetSerializable]
public enum ResearchTechnologyVisibilityState : byte
{
    Hidden,
    RevealedLocked,
    Available,
    Researched,
}
// Arcane-End
