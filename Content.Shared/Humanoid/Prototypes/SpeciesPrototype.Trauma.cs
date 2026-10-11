// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Prototypes;

/// <summary>
/// Trauma - store knowledge profile for a species
/// </summary>
public sealed partial class SpeciesPrototype
{
    // Arcane-Start: Character scale range
    [DataField]
    public float MinScale = 0.75f;

    [DataField]
    public float DefaultScale = 1f;

    [DataField]
    public float MaxScale = 1.25f;

    [DataField]
    public float AverageWeight = 71f;
    // Arcane-End

    [DataField]
    public ProtoId<KnowledgeProfilePrototype> Knowledge = "Human";
}
