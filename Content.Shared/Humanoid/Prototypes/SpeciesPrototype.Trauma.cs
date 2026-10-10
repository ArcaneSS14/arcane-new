// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Prototypes;

/// <summary>
/// Trauma - store knowledge profile for a species
/// </summary>
public sealed partial class SpeciesPrototype
{
    // Arcane-Start: Character growth range
    [DataField]
    public float MinHeight = 0.9f;

    [DataField]
    public float DefaultHeight = 1f;

    [DataField]
    public float MaxHeight = 1.2f;

    [DataField]
    public float MinWidth = 0.9f;

    [DataField]
    public float DefaultWidth = 1f;

    [DataField]
    public float MaxWidth = 1.1f;

    [DataField]
    public float AverageHeight = 170f;

    [DataField]
    public float AverageWidth = 45f;

    [DataField]
    public float AverageWeight = 71f;

    [DataField]
    public float SizeRatio = 1.2f;
    // Arcane-End

    [DataField]
    public ProtoId<KnowledgeProfilePrototype> Knowledge = "Human";
}
