using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Ghost;

/// <summary>
/// An antagonist everyone on the station learns about, listed separately in the ghost warp menu.
/// Matched by entity prototype (including parents), mind role prototype or component, so no mob
/// prototypes have to be edited.
/// </summary>
[Prototype]
public sealed partial class GlobalAntagonistPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// Sorting order in the menu, lower first.
    /// </summary>
    [DataField]
    public int Weight;

    [DataField]
    public List<EntProtoId> Entities = new();

    [DataField]
    public List<EntProtoId> MindRoles = new();

    /// <summary>
    /// Component names, only checked for presence.
    /// </summary>
    [DataField]
    public List<string> Components = new();
}
