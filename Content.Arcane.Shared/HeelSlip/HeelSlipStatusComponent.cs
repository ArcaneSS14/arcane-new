using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.HeelSlip;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeelSlipStatusComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField, AutoNetworkedField]
    public float SlowdownModifier = 0.25f;
}
