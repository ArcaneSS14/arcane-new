// Arcane-Start: silicon rest state
using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.SiliconStanding;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SiliconStandingComponent : Component
{
    public static readonly EntProtoId ToggleAction = "ActionToggleSiliconResting";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SiliconRestingComponent : Component;

public sealed partial class ToggleSiliconRestingEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum SiliconStandingVisuals : byte
{
    Resting,
}
// Arcane-End
