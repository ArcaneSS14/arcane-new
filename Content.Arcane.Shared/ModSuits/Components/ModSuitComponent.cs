using Content.Arcane.Shared.ModSuits.Systems;
using Robust.Shared.Containers;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(ModSuitSystem))]
public sealed partial class ModSuitComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<ModThemePrototype> Theme;

    [DataField]
    public string CoreSlotId = "mod-core";

    [DataField]
    public float ActivationCharge = 4f;

    [DataField]
    public float UnpoweredSpeedModifier = 0.3f;

    [AutoPausedField]
    public TimeSpan NextPowerAlertUpdate;

    public short? ShownPowerAlert;

    [DataField]
    public Dictionary<string, EntProtoId> Parts = [];

    [AutoNetworkedField]
    public Dictionary<string, EntityUid> PartEntities = [];

    public Container? PartsContainer;

    public Container? ModulesContainer;

    [DataField]
    public List<EntProtoId> InbuiltModules = [];

    [DataField]
    public List<EntProtoId> StartingModules = [];

    [AutoNetworkedField]
    public EntityUid? Wearer;

    [DataField]
    public EntProtoId ActivateAction = "ActionModSuitActivate";

    [DataField]
    public EntProtoId DeployAction = "ActionModSuitDeploy";

    public EntityUid? ActivateActionEntity;
    public EntityUid? DeployActionEntity;
    public readonly Queue<string> ActivationSteps = [];

    [DataField]
    public TimeSpan DeployStepTime = TimeSpan.FromSeconds(0.5);

    public readonly Queue<string> DeploymentSteps = [];

    [AutoNetworkedField]
    public bool Deploying;

    public bool Retracting;

    [AutoPausedField]
    public TimeSpan NextDeploymentStep;

    [AutoPausedField]
    public TimeSpan NextActivationStep;

    [DataField, AutoNetworkedField]
    public ModSuitState State;

    [AutoNetworkedField]
    public bool Powered;

    /// <summary>
    /// A temporary equipment copy that has no accessible core, parts or modules.
    /// </summary>
    [AutoNetworkedField]
    public bool Projected;
}

[Serializable, NetSerializable]
public enum ModSuitState : byte
{
    Inactive,
    Activating,
    Active,
    Deactivating,
}
