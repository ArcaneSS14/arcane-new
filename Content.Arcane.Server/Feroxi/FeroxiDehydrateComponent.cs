// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Feroxi;

/// <summary>
/// Component that allows switching between <see cref="MetabolizerTypePrototype"/>s based on thirst.
/// </summary>
[RegisterComponent, Access(typeof(FeroxiDehydrateSystem))]
public sealed partial class FeroxiDehydrateComponent : Component
{
    /// <summary>
    /// Metabolizer type to use when hydrated (above threshold).
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> HydratedMetabolizer;

    /// <summary>
    /// Metabolizer type to use when dehydrated (below or equal threshold).
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> DehydratedMetabolizer;

    [DataField]
    public bool Dehydrated;

    /// <summary>
    /// Thirst threshold where metabolizer switches.
    /// </summary>
    [DataField(required: true)]
    public float DehydrationThreshold;
}
