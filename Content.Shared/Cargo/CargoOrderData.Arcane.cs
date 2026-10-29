// Arcane-Start
namespace Content.Shared.Cargo;

/// <summary>
/// Arcane - delivery destination and secured department crate delivery (Orion #377).
/// <see cref="Reason"/> is shown as the order note.
/// </summary>
public sealed partial class CargoOrderData
{
    /// <summary>
    /// Where on the station the order should be delivered, free text.
    /// </summary>
    [DataField]
    public string? DeliveryDestination;

    /// <summary>
    /// Ship the product inside the ordering account's secure crate.
    /// </summary>
    [DataField]
    public bool SecuredDelivery;

    /// <summary>
    /// Flat price added on top of the products, e.g. for secured delivery.
    /// </summary>
    [DataField]
    public int ExtraCost;
}
// Arcane-End
