using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Medical.Respiration;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CPRRespirationComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool HasBreathReserve = true;

    [DataField, AutoNetworkedField]
    public bool BreathingImmune;
}
