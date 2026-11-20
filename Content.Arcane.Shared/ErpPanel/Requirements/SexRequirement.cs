using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.ErpPanel.Requirements;

[Serializable, NetSerializable]
public sealed partial class SexRequirement : InvertableErpRequirement
{
    [DataField(required: true)]
    public HashSet<Sex> Sexes = new();

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var hasSex = entityManager.TryGetComponent<HumanoidProfileComponent>(uid, out var humanoid)
            && Sexes.Contains(humanoid.Sex);

        return Inverted ? !hasSex : hasSex;
    }
}
