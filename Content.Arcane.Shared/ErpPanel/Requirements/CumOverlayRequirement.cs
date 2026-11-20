using Content.Arcane.Shared.ERP;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.ErpPanel.Requirements;

[Serializable, NetSerializable]
public sealed partial class CumOverlayRequirement : InvertableErpRequirement
{
    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var hasOverlay = entityManager.HasComponent<CumOverlayComponent>(uid);

        return Inverted ? !hasOverlay : hasOverlay;
    }
}
