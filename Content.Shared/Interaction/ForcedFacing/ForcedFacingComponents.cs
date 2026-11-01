// Arcane-Start
using System.Collections.Generic;
using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.ForcedFacing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ForcedFacingSystem))]
public sealed partial class ForcedFacingComponent : Component
{
    [AutoNetworkedField]
    public List<EntityUid> Targets = new();

    public EntityUid? PrimaryTarget => Targets.Count == 0 ? null : Targets[0];
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ForcedFacingSystem))]
public sealed partial class ForcedFacingTargetComponent : Component
{
    [AutoNetworkedField]
    public HashSet<EntityUid> Facing = new();
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ForcedFacingUiComponent : Component;

[ByRefEvent]
public record struct RefreshForcedFacingEvent
{
    public List<EntityUid> Targets;

    public RefreshForcedFacingEvent()
    {
        Targets = new();
    }
}

[ByRefEvent]
public record struct RefreshNoRotateOnMoveEvent(bool Enabled = false);

[ByRefEvent]
public record struct RefreshNoRotateOnInteractEvent(bool Enabled = false);
// Arcane-End
