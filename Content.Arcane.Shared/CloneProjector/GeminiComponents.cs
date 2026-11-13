using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Roles.Components;
using Content.Shared.Whitelist;

namespace Content.Arcane.Shared.CloneProjector;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CloneProjectorComponent : Component
{
    [AutoNetworkedField] public EntityUid? CloneUid;
    [AutoNetworkedField] public EntityUid? CurrentHost;
    [AutoNetworkedField] public EntityUid? ActionEntity;

    [DataField] public TimeSpan DestroyedCooldown = TimeSpan.FromSeconds(90);
    [DataField] public TimeSpan StunDuration = TimeSpan.FromSeconds(8);
    [DataField] public bool DoStun = true;
    [DataField] public DamageSpecifier DamageOnDestroyed = new();
    [DataField] public bool RestrictRangedWeapons = true;
    [DataField, AlwaysPushInheritance] public ComponentRegistry? AddedComponents;
    [DataField, AlwaysPushInheritance] public ComponentRegistry? RemovedComponents;
    [DataField] public EntityWhitelist? ClonedItemBlacklist;
    [DataField] public EntityWhitelist? ClonedItemWhitelist;
    [DataField] public EntityWhitelist? UserBlacklist;
    [DataField] public ProtoId<DamageModifierSetPrototype> CloneDamageModifierSet = "LivingLight";
    [DataField] public LocId NameSuffix = "gemini-projector-clone-name-suffix";
    [DataField] public LocId FlavorText = "gemini-projector-clone-flavor-text";
    [DataField] public LocId GhostRoleName = "ghost-role-information-gemini-clone-name";
    [DataField] public LocId GhostRoleDescription = "ghost-role-information-gemini-clone-description";
    [DataField] public LocId GhostRoleRules = "ghost-role-information-familiar-rules";
    [DataField] public EntProtoId Action = "ActionActivateProjector";

    public TimeSpan ReadyAt;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GeminiHologramComponent : Component
{
    [AutoNetworkedField] public bool RestrictRangedWeapons;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class GeminiRoleComponent : BaseMindRoleComponent;

public sealed partial class CloneProjectorActivatedEvent : InstantActionEvent;
