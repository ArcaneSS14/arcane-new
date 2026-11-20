using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.ErpPanel.Requirements;

/// <summary>
/// Requires a tail, i.e. any tail marking. Works for every species, including humans.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TailRequirement : InvertableErpRequirement
{
    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        // Tails are organs; any organ carrying a tail marking counts.
        var hasTail = false;
        foreach (var organ in entityManager.System<BodySystem>().GetOrgans<VisualOrganMarkingsComponent>(uid))
        {
            if (organ.Comp.Markings.TryGetValue(HumanoidVisualLayers.Tail, out var markings) && markings.Count > 0)
            {
                hasTail = true;
                break;
            }
        }

        return Inverted ? !hasTail : hasTail;
    }
}