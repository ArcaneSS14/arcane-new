using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Gambling;

[Serializable, NetSerializable]
public sealed partial class SlotMachineDoAfterEvent : DoAfterEvent
{
    [DataField] public int Operation;
    public override DoAfterEvent Clone() => (SlotMachineDoAfterEvent) MemberwiseClone();
}

[Serializable, NetSerializable]
public sealed partial class CoinFlipperDoAfterEvent : DoAfterEvent
{
    [DataField] public int Operation;
    public override DoAfterEvent Clone() => (CoinFlipperDoAfterEvent) MemberwiseClone();
}

[Serializable, NetSerializable]
public enum SlotMachineVisuals : byte { Spinning }

/// <summary>Raised on the player only after the prize entity was spawned.</summary>
[ByRefEvent]
public readonly record struct PrizeWonEvent(ProtoId<PrizePrototype> Prize, EntityUid Machine, EntityUid PrizeEntity);
