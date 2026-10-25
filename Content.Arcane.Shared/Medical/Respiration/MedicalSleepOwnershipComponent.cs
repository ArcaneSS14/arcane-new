using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Medical.Respiration;

/// <summary>
/// Records whether medical unconsciousness introduced sleep or joined an existing cause.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MedicalSleepOwnershipComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool OwnsSleep;
}
