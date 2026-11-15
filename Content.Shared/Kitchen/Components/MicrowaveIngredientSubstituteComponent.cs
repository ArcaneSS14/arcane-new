// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen.Components;

/// <summary>
///     Lets a solid ingredient satisfy one additional microwave ingredient ID.
///     Adapted from mithail's ArcaneSS14/arcane #582 (791d830c45885924770d2d5542e798e6d3e70213).
/// </summary>
[RegisterComponent]
public sealed partial class MicrowaveIngredientSubstituteComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Ingredient;
}
