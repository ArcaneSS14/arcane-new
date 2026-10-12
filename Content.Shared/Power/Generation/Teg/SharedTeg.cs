using Robust.Shared.Serialization;
using Content.Shared.DoAfter; // Arcane

namespace Content.Shared.Power.Generation.Teg;

/// <summary>
/// Appearance keys for the TEG &amp; its circulators.
/// </summary>
[Serializable, NetSerializable]
public enum TegVisuals
{
    PowerOutput,
    CirculatorSpeed,
    CirculatorPower,
    // Arcane-Start: Yogstation TEG appearance
    CirculatorState,
    MachineState,
    Flipped,
    // Arcane-End
}

/// <summary>
/// Visual sprite layers for the TEG &amp; its circulators.
/// </summary>
[Serializable, NetSerializable]
public enum TegVisualLayers
{
    PowerOutput,
    CirculatorBase,
    CirculatorLight
}

/// <summary>
/// Visual speed levels for the TEG circulators.
/// </summary>
[Serializable, NetSerializable]
public enum TegCirculatorSpeed
{
    SpeedStill,
    SpeedSlow,
    SpeedFast
}

// Arcane-Start: Yogstation TEG appearance
[Serializable, NetSerializable]
public enum TegCirculatorState
{
    Off,
    Idle,
    HotSlow,
    HotFast,
    ColdSlow,
    ColdFast,
}

[Serializable, NetSerializable]
public enum TegMachineState
{
    Unassembled,
    UnassembledFlipped,
    Assembled,
    AssembledFlipped,
    Broken,
}

[Serializable, NetSerializable]
public enum TegCirculatorMode
{
    Hot,
    Cold,
}

[Serializable, NetSerializable]
public sealed partial class TegCirculatorDoAfterEvent : SimpleDoAfterEvent
{
    [DataField] public bool ChangeMode;
    [DataField] public TegCirculatorMode? Mode;
    [DataField] public bool Flipped;
}
// Arcane-End
