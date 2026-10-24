using Content.Server.Clothing.Systems;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Station.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;

namespace Content.Arcane.Server.CloningAppearance;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed partial class CloningAppearanceSystem : EntitySystem
{
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private StationSystem _stations = default!;
    [Dependency] private StationSpawningSystem _spawning = default!;
    [Dependency] private ServerGameTicker _ticker = default!;
    [Dependency] private MindSystem _mindSystem = default!;
    [Dependency] private EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private OutfitSystem _outfit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CloningAppearanceComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CloningAppearanceEvent>(OnPlayerSpawn);
    }

    private void OnPlayerSpawn(CloningAppearanceEvent ev)
    {
        var profile = _ticker.GetPlayerProfile(ev.Player);
        var mobUid = _spawning.SpawnPlayerMob(ev.Coords, null, profile, ev.StationUid);
        var targetMind = _mindSystem.GetOrCreateMind(ev.Player.UserId);

        foreach (var entry in ev.Component.Components.Values)
        {
            var comp = (Component) _serialization.CreateCopy(entry.Component, notNullableOverride: true);
            AddComp(mobUid, comp, true);
        }

        if (ev.Component.Gear is { } gear)
        {
            _outfit.SetOutfit(mobUid, gear, doSpecial: false);
        }

        foreach (var nearbyEntity in _entityLookupSystem.GetEntitiesInRange(mobUid, 1f))
        {
            if (!TryComp<CryostorageComponent>(nearbyEntity, out var cryostorageComponent))
                continue;

            if (!_container.TryGetContainer(nearbyEntity, cryostorageComponent.ContainerId, out var container))
                continue;

            if (!_container.CanInsert(mobUid, container, true))
                continue;

            _container.Insert(mobUid, container);
            break;
        }

        _mindSystem.TransferTo(targetMind, mobUid);
    }

    private void OnPlayerAttached(Entity<CloningAppearanceComponent> ent, ref PlayerAttachedEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        QueueLocalEvent(new CloningAppearanceEvent
        {
            Player = args.Player,
            Component = ent.Comp,
            StationUid = _stations.GetOwningStation(ent),
            Coords = Transform(ent).Coordinates
        });
        Del(ent);
    }
}
