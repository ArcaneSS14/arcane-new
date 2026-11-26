namespace Content.Arcane.Server.Store;

/// <summary>
/// Allows unused physical purchases to be returned to this uplink for a partial refund.
/// Separate from the full-refund UI used by other stores.
/// </summary>
[RegisterComponent]
public sealed partial class UplinkRefundStoreComponent : Component
{
}
