using Content.Shared.DoAfter;
using Content.Shared.Speech.Components;
using Robust.Shared.Audio;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModPatientTransportComponent : Component
{
    [DataField]
    public EntProtoId BagPrototype = "ModsuitHardlightBag";

    [DataField]
    public TimeSpan CaptureDelay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Weapons/egloves.ogg");

    public EntityUid? Bag;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ModHardlightBagComponent : Component
{
    [DataField]
    public bool Capture;

    [DataField]
    public float MetabolicMultiplier = 10f;

    public EntityUid? Projector;
    public EntityUid? Wearer;
    public EntityUid? Occupant;
    public MumbleAccentComponent? AddedAccent;
    public TimeSpan? PullAt;
    public bool Releasing;
}

[RegisterComponent]
public sealed partial class ModTransportStasisComponent : Component
{
    public EntityUid Bag;
    public float Multiplier;
}

[Serializable, NetSerializable]
public sealed partial class ModPatientTransportDoAfterEvent : SimpleDoAfterEvent;
