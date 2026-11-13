using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Items.Components;

/// <summary>
/// Component that stores produce data for a plant.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BotanySystem))]
public sealed partial class ProduceComponent : Component
{
    /// <summary>
    /// Name of a base plant prototype to spawn when extracting seeds.
    /// </summary>
    [DataField("plantId"), AutoNetworkedField]
    public EntProtoId? PlantProtoId;

    /// <summary>
    /// Hidden entity with cloned plant components.
    /// Used to create a <see cref="SeedComponent"/> when this produce has its seeds extracted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? PlantData;

    /// <summary>
    /// Name of the solution container that holds the produce's contents.
    /// </summary>
    [DataField]
    public string TargetSolution = "food";

    // Arcane-Start: opt in to retaining initial contents for plants without chemical outputs.
    /// <summary>
    /// Keep the product's initial solution when its plant defines no chemicals.
    /// Existing crops retain their normal replacement behavior by default.
    /// </summary>
    [DataField]
    public bool PreserveSolutionWhenNoChemicals;
    // Arcane-End

    /// <summary>
    /// Divider for the nutrient bonus when composting this produce.
    /// </summary>
    [DataField]
    public float NutrientDivider = 2.5f;
}
