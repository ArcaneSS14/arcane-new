using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Body;

public sealed partial class HandOrganSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private BodySystem _body = default!; // Arcane

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandOrganComponent, OrganGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<HandOrganComponent, OrganGotRemovedEvent>(OnGotRemoved);
    }

    private void OnGotInserted(Entity<HandOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        _hands.AddHand(args.Target.Owner, ent.Comp.HandID, ent.Comp.Data); // Trauma - use .Owner
        GiveStartingItem(ent, args.Target); // Trauma
    }

    // Arcane: restore existing anatomy without granting the organ's starting item again.
    public void RestoreBodyHands(EntityUid body)
    {
        foreach (var organ in _body.EnumerateOrgans<HandOrganComponent>(body))
            _hands.AddHand(body, organ.Comp2.HandID, organ.Comp2.Data);
    }

    public HashSet<string> GetBodyHandIds(EntityUid body)
    {
        var ids = new HashSet<string>();
        foreach (var organ in _body.EnumerateOrgans<HandOrganComponent>(body))
            ids.Add(organ.Comp2.HandID);
        return ids;
    }

    private void OnGotRemoved(Entity<HandOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        // prevent a recursive double-delete bug
        if (LifeStage(args.Target) >= EntityLifeStage.Terminating)
            return;

        _hands.RemoveHand(args.Target.Owner, ent.Comp.HandID); // Trauma - use .Owner
        _cuffable.TryUncuff(args.Target.Owner, args.Target.Owner); // Trauma
    }
}
