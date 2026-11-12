using Content.Arcane.Shared.Gambling;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;

namespace Content.Arcane.Server.Gambling;

[RegisterComponent]
public sealed partial class SlotMachineComponent : Component
{
    [DataField] public int SpinCost = 250;
    [DataField(required: true)] public List<ProtoId<PrizePrototype>> Prizes = new();
    [DataField] public float DoAfterTime = 3.8f;
    [DataField] public SoundSpecifier? SpinSound;

    [ViewVariables] public bool Spinning;
    [ViewVariables] public bool EmagSpin;
    [ViewVariables] public EntityUid? User;
    [ViewVariables] public NetUserId? UserId;
    [ViewVariables] public DoAfterId? DoAfter;
    [ViewVariables] public int Operation;
}
