using Robust.Shared.Audio;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModMagneticHarnessComponent : Component
{
    [DataField]
    public List<string> Slots = new() { "back", "suitstorage" };

    [DataField]
    public float Range = 7f;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan PullTime = TimeSpan.FromSeconds(1);

    [DataField]
    public float PullSpeed = 1.2f;

    [DataField]
    public float LaunchSpeed = 12f;

    [DataField]
    public TimeSpan GiveUpTime = TimeSpan.FromSeconds(4);

    [DataField]
    public float CatchDistance = 0.6f;

    [DataField]
    public SoundSpecifier LaunchSound = new SoundPathSpecifier("/Audio/_Arcane/Items/Modsuit/gravhit.ogg", AudioParams.Default.WithVolume(-4f));

    [DataField]
    public SoundSpecifier CatchSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_end.ogg");
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ModMagnetizedGunComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Wearer;

    [DataField, AutoNetworkedField]
    public string Slot = "back";

    [DataField, AutoNetworkedField]
    public TimeSpan? LooseSince;

    [DataField, AutoNetworkedField]
    public bool Launched;

    [DataField, AutoNetworkedField]
    public bool Driving;

    [ViewVariables]
    public TimeSpan RetryAt;
}
