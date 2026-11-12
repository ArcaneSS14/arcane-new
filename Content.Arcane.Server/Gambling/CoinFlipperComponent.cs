using Content.Shared.DoAfter;
using Robust.Shared.Audio;

namespace Content.Arcane.Server.Gambling;

[RegisterComponent]
public sealed partial class CoinFlipperComponent : Component
{
    [DataField] public float DoAfterTime = 3.8f;
    [DataField] public SoundSpecifier SpinSound = new SoundPathSpecifier("/Audio/_Arcane/Machines/SlotMachine/slotmachine_spin.ogg");
    [DataField] public SoundSpecifier LoseSound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");
    [DataField] public SoundSpecifier WinSound = new SoundPathSpecifier("/Audio/Effects/Arcade/win.ogg");

    public bool Spinning;
    public int Stake;
    public int Operation;
    public DoAfterId? DoAfter;
}
