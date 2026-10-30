using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Arcane.Shared.Species;

/// <summary>
/// What the species window shows for a species (Orion #45). The ID is the species ID.
/// </summary>
[Prototype]
public sealed partial class SpeciesInfoPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public SpeciesCategory Category = SpeciesCategory.Classic;

    /// <summary>
    /// Guidebook-style document shown under the preview. Falls back to the guide entry with the species ID.
    /// </summary>
    [DataField]
    public ResPath? Description;

    [DataField]
    public List<LocId> Pros = new();

    [DataField]
    public List<LocId> Special = new();

    [DataField]
    public List<LocId> Cons = new();
}

public enum SpeciesCategory : byte
{
    Classic,
    Unusual,
    Special,
}
