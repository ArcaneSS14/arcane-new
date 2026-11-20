using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Weapons.Aim;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ArcaneAimCameraOffsetComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Offset = 9f;

    [DataField, AutoNetworkedField]
    public EntityUid? Source;
}
