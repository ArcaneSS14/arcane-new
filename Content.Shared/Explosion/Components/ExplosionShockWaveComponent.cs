// Arcane-Start
using Robust.Shared.GameStates;

namespace Content.Shared.Explosion.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExplosionShockWaveComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float FalloffPower = 40f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Sharpness = 10f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Width = 0.8f;
}
// Arcane-End
