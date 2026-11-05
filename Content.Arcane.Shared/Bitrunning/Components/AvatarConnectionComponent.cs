using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Bitrunning.Components;

[RegisterComponent]
public sealed partial class AvatarConnectionComponent : Component
{
    [DataField]
    public EntityUid? OriginalBody;

    [DataField]
    public EntityUid? Server;

    [DataField]
    public EntityUid? Netpod;

    [DataField]
    public EntityUid? RunnerMind;

    [DataField]
    public bool NoHit = true;

    [DataField]
    public bool DeleteOnDisconnect;

    [DataField]
    public EntProtoId DisconnectActionPrototype = "ActionBitrunningDisconnectAvatar";

    [DataField]
    public EntityUid? DisconnectActionEntity;

    [DataField]
    public TimeSpan DisconnectBlockedUntil = TimeSpan.Zero;

    /// <summary>
    /// Set while the avatar is being disconnected, so the death and ejection it causes do not disconnect it again.
    /// </summary>
    [ViewVariables]
    public bool Disconnecting;
}
