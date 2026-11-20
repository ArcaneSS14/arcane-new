using Content.Arcane.Shared.Gambling;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Gambling;

[RegisterComponent]
public sealed partial class ClawMachineComponent : Component
{
    [DataField] public float DoAfterTime = 3.9f;
    [DataField] public SoundSpecifier PlaySound = new SoundPathSpecifier("/Audio/Machines/Keyboard/keyboard1.ogg");
    [DataField(required: true)] public List<ProtoId<PrizePrototype>> Prizes = new();
    [DataField(required: true)] public List<ProtoId<PrizePrototype>> EvilPrizes = new();

    [ViewVariables] public bool Spinning;
    [ViewVariables] public int Operation;
    [ViewVariables] public EntityUid? User;
    [ViewVariables] public EntityUid? Mind;
    [ViewVariables] public NetUserId? UserId;
    [ViewVariables] public DoAfterId? DoAfter;
}
