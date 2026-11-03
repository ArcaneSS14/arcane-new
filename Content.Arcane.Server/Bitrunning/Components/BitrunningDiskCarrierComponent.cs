namespace Content.Arcane.Server.Bitrunning.Components;

/// <summary>
/// Marks the physical body of a connected bitrunner, so items leaving its containers refresh the avatar's disk abilities.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningDiskCarrierComponent : Component
{
    public EntityUid Avatar;
}
