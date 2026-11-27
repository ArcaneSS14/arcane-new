using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Store;

/// <summary>
/// Purchase and unused-state snapshot for the uplink's partial refund interaction.
/// Monetary ownership remains in the store's common purchase receipt.
/// </summary>
[RegisterComponent]
public sealed partial class UplinkRefundableComponent : Component
{
    /// <summary>The authoritative store that supplied this physical product.</summary>
    [DataField]
    public EntityUid OriginalUplink;

    /// <summary>Integer-floor 80% of the actual telecrystal price paid.</summary>
    [DataField]
    public FixedPoint2 RefundAmount;

    /// <summary>Whether a source-defined use has permanently invalidated the partial refund.</summary>
    [DataField]
    public bool IsUsed;

    /// <summary>The currency captured at purchase; currently only telecrystals are eligible.</summary>
    [DataField]
    public ProtoId<CurrencyPrototype> Currency = "Telecrystal";

    /// <summary>Direct contained prototype IDs captured from a non-empty purchased container.</summary>
    [DataField]
    public List<string>? OriginalContainerPrototypes;

    /// <summary>Original item entities throughout the purchased container tree.</summary>
    [DataField]
    public HashSet<EntityUid> OriginalContainedItems = new();

    /// <summary>Combined reagent quantities across the product's solutions at purchase.</summary>
    [DataField]
    public Dictionary<string, FixedPoint2>? OriginalSolutionReagents;
}

/// <summary>Connects purchased contents to the product whose refund they affect.</summary>
[RegisterComponent]
public sealed partial class UplinkRefundUsageComponent : Component
{
    [DataField]
    public EntityUid Product;
}
