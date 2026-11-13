using Content.Shared.Body;
using Content.Shared.Hands.Components;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    [Dependency] private HandOrganSystem _zombieHandOrgans = default!;
    [Dependency] private ExtraHandsEquipmentSystem _zombieEquipmentHands = default!;

    public void RestoreAfterZombification(EntityUid uid, HandsComponent saved,
        List<(string Name, Hand Definition)> definitions, string? activeHand)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
        {
            saved.Hands.Clear();
            saved.SortedHands.Clear();
            saved.ActiveHandId = null;
            AddComp(uid, saved);
            hands = saved;
        }

        foreach (var (name, definition) in definitions)
            AddHand((uid, hands), name, definition);
        _zombieHandOrgans.RestoreBodyHands(uid);
        _zombieEquipmentHands.RestoreEquipmentHands((uid, hands));
        TrySetActiveHand((uid, hands), activeHand);
    }
}
