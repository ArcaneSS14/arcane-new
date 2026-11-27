// Arcane-Start
namespace Content.Shared.Silicons.Borgs;

public sealed partial class BorgSubtypePrototype
{
    /// <summary>
    /// Sprite state overrides for this subtype.
    /// </summary>
    [DataField]
    public BorgSubtypeVisuals Visuals = new();
}

/// <summary>
/// Sprite state overrides of a borg subtype, for sprites whose states differ from the parent borg type.
/// </summary>
[DataDefinition]
public sealed partial class BorgSubtypeVisuals
{
    /// <summary>
    /// State used while the borg rests, if the subtype has one.
    /// </summary>
    [DataField]
    public string? RestBodyState;

    /// <summary>
    /// State used while the borg moves, instead of the parent type's.
    /// </summary>
    [DataField]
    public string? MovementBodyState;

    /// <summary>
    /// Light state when a mind is in the borg, instead of the parent type's.
    /// </summary>
    [DataField]
    public string? HasMindState;

    /// <summary>
    /// Light state without a mind, instead of the parent type's.
    /// </summary>
    [DataField]
    public string? NoMindState;

    /// <summary>
    /// Toggleable light state, instead of the parent type's.
    /// </summary>
    [DataField]
    public string? ToggleLightState;

    /// <summary>
    /// Do not switch the body state while moving.
    /// </summary>
    [DataField]
    public bool DisableMovementVisuals;
}
// Arcane-End
