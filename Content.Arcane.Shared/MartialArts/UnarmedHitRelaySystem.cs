using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.Weapons.Classes;

namespace Content.Arcane.Shared.MartialArts;

/// <summary>
/// Gloves are weapons, so their hits never reach the wearer's knowledge the way bare hands do.
/// They still count as fighting unarmed, so this gives the hit to the active martial art.
/// </summary>
public sealed partial class UnarmedHitRelaySystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private WeaponClassSystem _weaponClass = default!;
    [Dependency] private EntityQuery<KnowledgeHolderComponent> _holderQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(MeleeHitEvent args)
    {
        if (args.Weapon == args.User
            || !_weaponClass.IsUnarmed(args.User, args.Weapon)
            || !_holderQuery.TryComp(args.User, out var holder))
            return;

        _knowledge.RelayActiveEvent((args.User, holder), ref args);
    }
}
