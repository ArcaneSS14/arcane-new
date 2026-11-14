using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Storage;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private EntityLookupSystem _loaderLookup = default!;
    private const float LoaderCargoOffset = 1f;
    private const float LoaderAttachRange = 1.5f;

    private readonly HashSet<Entity<PhysicsComponent>> _loaderCandidates = [];

    private void InitializeLoaderModules()
    {
        SubscribeLocalEvent<ModLoaderMagnetComponent, ModLoaderMagnetActionEvent>(OnLoaderMagnetAction);
        SubscribeLocalEvent<ModLoaderMagnetComponent, ModModuleUsedEvent>(OnLoaderMagnetUsed);
        SubscribeLocalEvent<ModLoaderMagnetizedComponent, StopThrowEvent>(OnLoaderCargoLanded);
        SubscribeLocalEvent<ModCandelaModuleComponent, ModModuleActivatedEvent>(OnCandelaActivated);
    }

    private void OnLoaderMagnetAction(Entity<ModLoaderMagnetComponent> magnet, ref ModLoaderMagnetActionEvent args)
    {
        if (!args.Handled && TryComp(magnet, out ModModuleComponent? module))
            args.Handled = TryUseModule((magnet.Owner, module), args.Performer, args);
    }

    private void OnLoaderMagnetUsed(Entity<ModLoaderMagnetComponent> magnet, ref ModModuleUsedEvent args)
    {
        if (args.Handled || args.Action is not ModLoaderMagnetActionEvent action
            || !action.Target.IsValid(EntityManager) || GetDeployedPart(args.Suit, OuterClothingSlot) == null)
            return;

        var user = args.User;
        var from = _transform.GetMapCoordinates(user);
        var target = _transform.ToMapCoordinates(action.Target);
        if (target.MapId == MapId.Nullspace || target.MapId != from.MapId
            || Vector2.DistanceSquared(target.Position, from.Position) > magnet.Comp.LaunchRange * magnet.Comp.LaunchRange)
            return;

        if (TryComp(user, out PullerComponent? puller) && puller.Pulling is { } cargo
            && _whitelist.IsWhitelistPass(magnet.Comp.Whitelist, cargo)
            && TryComp(cargo, out PullableComponent? pullable))
        {
            var offset = target.Position - _transform.GetMapCoordinates(cargo).Position;
            if (offset.LengthSquared() < 0.0001f || !_transportPulling.TryStopPull(cargo, pullable, user, ignoreGrab: true))
                return;

            if (offset.LengthSquared() > magnet.Comp.LaunchRange * magnet.Comp.LaunchRange)
                offset = Vector2.Normalize(offset) * magnet.Comp.LaunchRange;

            args.Handled = _throwing.TryThrow(cargo, offset, magnet.Comp.LaunchSpeed, user,
                pushbackRatio: 0f, recoil: false, doSpin: false, predicted: false);
            if (args.Handled)
                _audio.PlayPvs(magnet.Comp.Sound, cargo);
            return;
        }

        if (FindLoaderCargo(magnet, user, target) is not { } item)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-loader-magnet-invalid"), user, user);
            return;
        }

        var toCargo = _transform.GetMapCoordinates(item).Position - from.Position;
        if (toCargo.LengthSquared() <= LoaderCargoOffset * LoaderCargoOffset)
        {
            args.Handled = _transportPulling.TryStartPull(user, item);
            return;
        }

        var direction = Vector2.Normalize(toCargo) * LoaderCargoOffset - toCargo;
        args.Handled = _throwing.TryThrow(item, direction, magnet.Comp.PullSpeed, user,
            pushbackRatio: 0f, recoil: false, doSpin: false, predicted: false);
        if (!args.Handled)
            return;

        var marked = EnsureComp<ModLoaderMagnetizedComponent>(item);
        marked.Puller = user;
        marked.Module = magnet;
        _audio.PlayPvs(magnet.Comp.Sound, item);
    }

    private EntityUid? FindLoaderCargo(Entity<ModLoaderMagnetComponent> magnet, EntityUid user, MapCoordinates target)
    {
        EntityUid? best = null;
        var bestDistance = float.MaxValue;
        _loaderCandidates.Clear();
        _loaderLookup.GetEntitiesInRange(target, magnet.Comp.PickRange, _loaderCandidates, LookupFlags.Dynamic | LookupFlags.Approximate);
        foreach (var candidate in _loaderCandidates)
        {
            if (candidate.Owner == user || TerminatingOrDeleted(candidate) || Transform(candidate).Anchored
                || candidate.Comp.BodyType == BodyType.Static || _containers.IsEntityInContainer(candidate)
                || !_whitelist.IsWhitelistPass(magnet.Comp.Whitelist, candidate))
                continue;

            var distance = Vector2.DistanceSquared(_transform.GetMapCoordinates(candidate).Position, target.Position);
            if (distance >= bestDistance)
                continue;

            best = candidate.Owner;
            bestDistance = distance;
        }

        return best;
    }

    private void OnLoaderCargoLanded(Entity<ModLoaderMagnetizedComponent> cargo, ref StopThrowEvent args)
    {
        var wearer = cargo.Comp.Puller;
        if (_net.IsServer && args.User == wearer && TryGetInstalledModuleSuit(cargo.Comp.Module, out var suit)
            && suit.Comp.Wearer == wearer && suit.Comp.State == ModSuitState.Active && suit.Comp.Powered
            && !TerminatingOrDeleted(wearer) && GetDeployedPart(suit, OuterClothingSlot) != null
            && _interaction.InRangeUnobstructed(wearer, cargo.Owner, range: LoaderAttachRange))
            _transportPulling.TryStartPull(wearer, cargo);

        RemCompDeferred(cargo.Owner, cargo.Comp);
    }

    private void OnCandelaActivated(Entity<ModCandelaModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        module.Comp.LastBeacon = null;
        module.Comp.WarnedEmpty = false;
    }

    private void UpdateCandela()
    {
        var query = EntityQueryEnumerator<ModCandelaModuleComponent, ModModuleComponent>();
        while (query.MoveNext(out var uid, out var candela, out var module))
        {
            if (!module.Active || !TryGetInstalledModuleSuit(uid, out var suit) || suit.Comp.Wearer is not { } wearer
                || GetDeployedPart(suit, OuterClothingSlot) == null || GetDeployedPart(suit, "gloves") == null
                || _containers.IsEntityInContainer(wearer))
                continue;

            var xform = Transform(wearer);
            if (xform.GridUid is not { } gridUid || xform.ParentUid != gridUid || !TryComp(gridUid, out MapGridComponent? grid))
                continue;

            var at = _transform.GetMapCoordinates(wearer, xform);
            if (candela.LastBeacon is { } previous && previous.MapId == at.MapId
                && Vector2.DistanceSquared(previous.Position, at.Position) < candela.Spacing * candela.Spacing)
                continue;

            var tile = _map.GetTileRef(gridUid, grid, xform.Coordinates);
            if (tile.Tile.IsEmpty)
                continue;

            if (!TryFindCandela(suit, candela, out var beacon, out var storage))
            {
                if (!candela.WarnedEmpty)
                {
                    candela.WarnedEmpty = true;
                    _popup.PopupEntity(Loc.GetString("modsuit-candela-empty"), wearer, wearer);
                }
                continue;
            }

            if (!_containers.Remove(beacon, storage, destination: xform.Coordinates))
                continue;

            if (!_transform.AnchorEntity(beacon, tile))
            {
                _containers.Insert(beacon, storage);
                continue;
            }

            candela.LastBeacon = at;
            candela.WarnedEmpty = false;
            _itemToggle.TryActivate(beacon, predicted: false, showPopup: false);
            _audio.PlayPvs(candela.Sound, beacon);
        }
    }

    private bool TryFindCandela(Entity<ModSuitComponent> suit, ModCandelaModuleComponent candela,
        out EntityUid beacon, out BaseContainer storage)
    {
        if (TryComp(suit, out StorageComponent? native) && TryFindCandelaIn(native, candela, out beacon))
        {
            storage = native.Container;
            return true;
        }

        if (suit.Comp.ModulesContainer is { } modules)
        {
            foreach (var uid in modules.ContainedEntities)
            {
                if (IsModuleStorageActive(uid) && TryComp(uid, out StorageComponent? extra)
                    && TryFindCandelaIn(extra, candela, out beacon))
                {
                    storage = extra.Container;
                    return true;
                }
            }
        }

        beacon = default;
        storage = default!;
        return false;
    }

    private bool TryFindCandelaIn(StorageComponent storage, ModCandelaModuleComponent candela, out EntityUid beacon)
    {
        foreach (var uid in storage.Container.ContainedEntities)
        {
            if (!TerminatingOrDeleted(uid) && _whitelist.IsWhitelistPass(candela.Whitelist, uid))
            {
                beacon = uid;
                return true;
            }
        }

        beacon = default;
        return false;
    }
}
