using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Bitrunning.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AvatarNavRelayComponent : Component
{
    /// <summary>
    /// The paired avatar or netpod, kept up to date on connect, reconnect, polymorph and disconnect.
    /// Nothing reads it yet; camera relaying between them is not implemented.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? RelayEntity;
}
