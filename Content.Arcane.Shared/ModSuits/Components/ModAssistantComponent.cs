using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Physics;

namespace Content.Arcane.Shared.ModSuits.Components;

/// <summary>
/// The original AI brain or pAI device installed in a MOD, never a second mind or body.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModAssistantComponent : Component
{
    [AutoNetworkedField]
    public EntityUid Suit;

    public EntityUid? InterfaceAction;
    public EntityUid? ActivateAction;
    public EntityUid? DeployAction;
    [AutoNetworkedField]
    public HashSet<EntityUid> PinnedModules = [];
    public readonly Dictionary<EntityUid, EntityUid> ModuleActions = [];
    public readonly Dictionary<EntityUid, EntityUid> SelectorActions = [];
    public EntityUid? MovementTarget;
    public InputMoverComponent? CreatedMover;
    public MovementSpeedModifierComponent? CreatedSpeedModifier;
    public BodyType? PreviousBodyType;
    public bool AddedInput;
    public bool? PreviousDrawFov;
    public MoveButtons HeldButtons;
}

[RegisterComponent]
public sealed partial class ModAssistantMoverComponent : Component
{
    public EntityUid Assistant;
    public MoveButtons OwnerButtons;
}
