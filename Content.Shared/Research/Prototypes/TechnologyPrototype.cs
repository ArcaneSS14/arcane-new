using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
// Arcane-Start
using Robust.Shared.Serialization;
using Content.Shared.Radio;
// Arcane-End

using Content.Arcane.Common.Research; // Arcane

// Arcane-Start
using Content.Shared.Construction.Prototypes;
using Content.Shared.Research.Prototypes;
using Content.Arcane.Common.Construction.Prototypes;
// Arcane-End

namespace Content.Shared.Research.Prototypes;

/// <summary>
/// This is a prototype for a technology that can be unlocked.
/// </summary>
[Prototype]
public sealed partial class TechnologyPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the technology.
    /// Supports locale strings
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    // Arcane-Start
    /// <summary>
    /// Localized description of the technology.
    /// </summary>
    [DataField]
    public LocId Description = string.Empty;
    // Arcane-End

    /// <summary>
    /// An icon used to visually represent the technology in UI.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// What research discipline this technology belongs to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TechDisciplinePrototype> Discipline;

    /// <summary>
    /// What tier research is this?
    /// The tier governs how much lower-tier technology
    /// needs to be unlocked before this one.
    /// </summary>
    [DataField(required: true)]
    public int Tier;

    /// <summary>
    /// Hidden tech is not ever available at the research console.
    /// </summary>
    [DataField]
    public bool Hidden;

    // Arcane-Start
    /// <summary>
    /// Should this technology start as researched in every compatible database.
    /// </summary>
    [DataField]
    public bool StartingTechnology;
    // Arcane-End

/* // Arcane-Edit: Use PointCosts
    /// <summary>
    /// How much research is needed to unlock.
    /// </summary>
    [DataField]
    public int Cost = 10000;
*/

    // Arcane-Start
    /// <summary>
    /// Multipoint type costs.
    /// </summary>
    [DataField(required: true)]
    public List<ResearchPointAmount> PointCosts = new();
    // Arcane-End

    /// <summary>
    /// A list of <see cref="TechnologyPrototype"/>s that need to be unlocked in order to unlock this technology.
    /// </summary>
    [DataField]
    public List<ProtoId<TechnologyPrototype>> TechnologyPrerequisites = new();

    // Arcane-Start
    /// <summary>
    /// The required experiments that must be completed before this technology can be researched.
    /// </summary>
    [DataField]
    public List<ProtoId<ResearchExperimentPrototype>> RequiredExperiments = new();

    /// <summary>
    /// Experiments that can reduce the research cost for this technology.
    /// </summary>
    [DataField]
    public List<ProtoId<ResearchExperimentPrototype>> DiscountExperiments = new();

    /// <summary>
    /// Per-technology discount values keyed by experiment ID.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ResearchExperimentPrototype>, int> DiscountExperimentCosts = new();

    /// <summary>
    /// Experiments unlocked when this technology is researched.
    /// </summary>
    [DataField]
    public List<ProtoId<ResearchExperimentPrototype>> UnlockedExperiments = new();

    /// <summary>
    /// Should this technology be announced to the station when unlocked.
    /// </summary>
    [DataField]
    public bool AnnounceOnUnlock = true;

    /// <summary>
    /// Radio channels used for unlock announcements when <see cref="AnnounceOnUnlock"/> is enabled.
    /// Falls back to the console announcement channel if empty.
    /// </summary>
    [DataField]
    public List<ProtoId<RadioChannelPrototype>> AnnounceChannels = new();

    /// <summary>
    /// Infrastructure categories unlocked by this technology.
    /// </summary>
    [DataField]
    public List<string> InfrastructureUnlocks = new();

    /// <summary>
    /// Additional reveal/discovery requirements for hidden technologies.
    /// </summary>
    [DataField]
    public List<TechnologyRevealRequirement> RevealRequirements = new();
    // Arcane-End

    /// <summary>
    /// A list of <see cref="LatheRecipePrototype"/>s that are unlocked by this technology
    /// </summary>
    [DataField]
    public List<ProtoId<LatheRecipePrototype>> RecipeUnlocks = new();

    /// <summary>
    /// A list of non-standard effects that are done when this technology is unlocked.
    /// </summary>
    [DataField]
    public IReadOnlyList<GenericUnlock> GenericUnlocks = new List<GenericUnlock>();

    // Arcane-Start
    /// <summary>
    /// Item prototypes that advance this technology's discovery when inserted into a research machine.
    /// </summary>
    [DataField]
    public List<string> ItemUnlocks = new();

    /// <summary>
    /// Prototypes that advance this technology's discovery when deconstructed.
    /// </summary>
    [DataField]
    public List<string> DeconstructionUnlocks = new();

    /// <summary>
    /// Required item prototype paths to reveal or unlock this technology.
    /// </summary>
    [DataField]
    public List<string> RequiredItemsToUnlock = new();
    // Arcane-End

    /// <summary>
    /// Goobstation RnD console rework field
    /// Position of this tech in console menu
    /// </summary>
    [DataField(required: true)]
    public Vector2i Position { get; private set; }

    // Arcane-Start
    public IEnumerable<ProtoId<TechnologyPrototype>> AllRequiredTechnologies => TechnologyPrerequisites;
    // Arcane-End
}

// Arcane-Start
[Serializable, NetSerializable]
public enum TechnologyRevealRequirementKind : byte
{
    RevealedTechnology,
    ResearchedTechnology,
    CompletedExperiment,
    ScanEntity,
    MachineInsertion,
    DeconstructEntity,
    ServerTrigger,
}

[DataDefinition, Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial record TechnologyRevealRequirement
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField]
    public TechnologyRevealRequirementKind Kind;

    [DataField]
    public int Target = 1;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record RevealedTechnologyRevealRequirement : TechnologyRevealRequirement
{
    [DataField(required: true)]
    public ProtoId<TechnologyPrototype> Technology;

    public RevealedTechnologyRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.RevealedTechnology;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ResearchedTechnologyRevealRequirement : TechnologyRevealRequirement
{
    [DataField(required: true)]
    public ProtoId<TechnologyPrototype> Technology;

    public ResearchedTechnologyRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.ResearchedTechnology;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record CompletedExperimentRevealRequirement : TechnologyRevealRequirement
{
    [DataField(required: true)]
    public ProtoId<ResearchExperimentPrototype> Experiment;

    public CompletedExperimentRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.CompletedExperiment;
    }
}

[DataDefinition, Serializable, NetSerializable]
public partial record ScanEntityRevealRequirement : TechnologyRevealRequirement
{
    [DataField]
    public string? RequiredEntityPrototype;

    [DataField]
    public List<string> RequiredTags = new();

    public ScanEntityRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.ScanEntity;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record MachineInsertionRevealRequirement : ScanEntityRevealRequirement
{
    [DataField]
    public string? RequiredMachinePrototype;

    public MachineInsertionRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.MachineInsertion;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record DeconstructEntityRevealRequirement : TechnologyRevealRequirement
{
    [DataField]
    public string? RequiredEntityPrototype;

    [DataField]
    public List<string> RequiredTags = new();

    public DeconstructEntityRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.DeconstructEntity;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ServerTriggerRevealRequirement : TechnologyRevealRequirement
{
    [DataField(required: true)]
    public string TriggerId = string.Empty;

    public ServerTriggerRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.ServerTrigger;
    }
}
// Arcane-End

[DataDefinition]
public partial record struct GenericUnlock()
{
    /// <summary>
    /// What event is raised when this is unlocked?
    /// Used for doing non-standard logic.
    /// </summary>
    [DataField]
    public object? PurchaseEvent = null;

    /// <summary>
    /// A player facing tooltip for what the unlock does.
    /// Supports locale strings.
    /// </summary>
    [DataField]
    public string UnlockDescription = string.Empty;
}

// Arcane-Start
/// <summary>
/// Prototype definition for experiment progression in research techweb.
/// </summary>
[Prototype]
public sealed partial class ResearchExperimentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = string.Empty;

    [DataField]
    public LocId Description = string.Empty;

    [DataField]
    public string Category = "General";

    [DataField]
    public bool Hidden;

    [DataField]
    public bool StartingExperiment;

    /// <summary>
    /// Required technologies for the experiment to become available.
    /// </summary>
    [DataField]
    public List<ProtoId<TechnologyPrototype>> RequiredTechnologies = new();

    /// <summary>
    /// Required experiments for this experiment to become available.
    /// </summary>
    [DataField]
    public List<ProtoId<ResearchExperimentPrototype>> RequiredExperiments = new();

    [DataField]
    public ExperimentSourceFlags SupportedSources = ExperimentSourceFlags.AnyScanner;

    [DataField(required: true)]
    public ExperimentObjective Objective = new ServerTriggerExperimentObjective();

    [DataField]
    public ExperimentReward Reward = new();

    [DataField]
    public Vector2i? Position;
}

[Serializable, NetSerializable]
public enum ExperimentObjectiveKind : byte
{
    ServerTrigger,
    ScanEntity,
    PresentItem,
    ScanDifferentEntities,
    ScanSamples,
    ActionCount,
    DebugManual,
}

[Flags]
public enum ExperimentSourceFlags : byte
{
    None = 0,
    ResearchConsole = 1 << 0,
    MachineScanner = 1 << 1,
    HandheldScanner = 1 << 2,
    AnyScanner = ResearchConsole | MachineScanner | HandheldScanner,
    DefaultScanner = ResearchConsole | MachineScanner,
}

[DataDefinition, Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial record ExperimentObjective
{
    [DataField]
    public ExperimentObjectiveKind Kind = ExperimentObjectiveKind.ServerTrigger;

    [DataField]
    public int Target = 1;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ServerTriggerExperimentObjective : ExperimentObjective
{
    [DataField]
    public string TriggerId = string.Empty;

    public ServerTriggerExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.ServerTrigger;
    }
}

[DataDefinition, Serializable, NetSerializable]
public partial record ScanEntityExperimentObjective : ExperimentObjective
{
    [DataField]
    public List<string> RequiredEntityPrototypes = new();

    [DataField]
    public List<string> RequiredTags = new();

    [DataField]
    public List<string> RequiredComponents = new();

    [DataField]
    public List<ExperimentEntityCondition> RequiredConditions = new();

    [DataField]
    public string? RequiredReagent;

    [DataField]
    public float? MinReagentPurity;

    [DataField]
    public string? RequiredGas;

    [DataField]
    public float? MinGasPurity;

    [DataField]
    public float? MinExplosiveIntensity;

    [DataField]
    public int? RequiredMachinePartTier;

    [DataField]
    public List<ProtoId<MachinePartPrototype>> RequiredMachineParts = new();

    public ScanEntityExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.ScanEntity;
    }
}

[Serializable, NetSerializable]
public enum ExperimentEntityCondition : byte
{
    AnyFish,
    RareFish,
    IpcOrCyborg,
    HasAugmentedOrgans,
    NonBaselineHumanoid,
    Damaged,
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record PresentItemExperimentObjective : ScanEntityExperimentObjective
{
    public PresentItemExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.PresentItem;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ScanDifferentEntitiesExperimentObjective : ScanEntityExperimentObjective
{
    public ScanDifferentEntitiesExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.ScanDifferentEntities;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ScanSamplesExperimentObjective : ScanEntityExperimentObjective
{
    public ScanSamplesExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.ScanSamples;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ActionCountExperimentObjective : ExperimentObjective
{
    [DataField(required: true)]
    public string ActionId = string.Empty;

    public ActionCountExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.ActionCount;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record DebugManualExperimentObjective : ExperimentObjective
{
    public DebugManualExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.DebugManual;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ExperimentReward
{
    [DataField]
    public int ResearchPoints;

    [DataField]
    public List<ResearchPointAmount> PointRewards = new();

    [DataField]
    public int FlatDiscount;

    [DataField]
    public float PercentageDiscount;

    [DataField]
    public List<ProtoId<ResearchExperimentPrototype>> UnlockExperiments = new();

    [DataField]
    public List<ProtoId<TechnologyPrototype>> RevealTechnologies = new();
}
// Arcane-End
