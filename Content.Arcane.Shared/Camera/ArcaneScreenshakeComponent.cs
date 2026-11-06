using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Arcane.Common.Camera;

namespace Content.Arcane.Shared.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArcaneScreenshakeComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ArcaneScreenshakeCommand> Commands = new();

    public override bool SendOnlyToOwner => true;
}
