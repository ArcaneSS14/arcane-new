// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Arcane - order slip fields from Orion #377.
/// </summary>
public sealed partial class CargoSlipComponent
{
    [DataField]
    public string? DeliveryDestination;

    [DataField]
    public bool SecuredDelivery;

    [DataField]
    public int ExtraCost;
}
