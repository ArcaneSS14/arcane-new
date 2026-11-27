using Content.Shared.Coordinates.Helpers;
using Content.Shared.Forensics.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Arcane.Shared.WashingMachine;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Arcane.Server.WashingMachine;

public sealed partial class WashingMachineSystem : SharedWashingMachineSystem
{
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void EjectPlayer(Entity<WashingMachineStuckComponent> ent)
    {
        var uid = ent.Owner;

        if (ent.Comp.Machine is { } machine && Exists(machine))
        {
            if (!TryFindEjectTile(machine, out var coords))
                return;

            _transform.SetCoordinates(uid, coords);
        }

        FinishEject(ent);
    }

    protected override void ForceEjectPlayer(Entity<WashingMachineStuckComponent> ent)
    {
        var uid = ent.Owner;

        if (ent.Comp.Machine is { } machine && Exists(machine))
        {
            var coords = Transform(machine).Coordinates;
            if (TryFindEjectTile(machine, out var ejectCoords, maxRadius: 2))
                coords = ejectCoords;
            _transform.SetCoordinates(uid, coords);
        }

        FinishEject(ent);
    }

    private void FinishEject(Entity<WashingMachineStuckComponent> ent)
    {
        var uid = ent.Owner;

        if (ent.Comp.TopVisual is { } visual && CanDeleteEntity(visual))
            Del(visual);

        var machineUid = ent.Comp.Machine ?? uid;
        _popup.PopupPredicted(
            Loc.GetString("washing-machine-escape-self", ("machine", machineUid)),
            Loc.GetString("washing-machine-escape-others", ("user", Identity.Entity(uid, EntityManager)), ("machine", machineUid)),
            uid,
            uid);

        RemComp<WashingMachineStuckComponent>(uid);
    }

    private bool TryFindEjectTile(EntityUid machine, out EntityCoordinates coords, int maxRadius = 1)
    {
        coords = default;
        var xform = Transform(machine);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var gridPos = xform.Coordinates.Position;

        for (var radius = 1; radius <= maxRadius; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius)
                        continue;

                    var candidate = new EntityCoordinates(gridUid, gridPos + new Vector2(dx, dy)).SnapToGrid();

                    if (!_maps.TryGetTileRef(gridUid, grid, candidate, out var tileRef))
                        continue;

                    if (tileRef.Tile.IsEmpty)
                        continue;

                    if (_turf.IsTileBlocked(tileRef, CollisionGroup.MobMask))
                        continue;

                    coords = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    protected override void UpdateForensics(Entity<WashingMachineComponent> ent, HashSet<EntityUid> items)
    {
        if (!TryComp<ForensicsComponent>(ent.Owner, out var forensics))
            return;

        foreach (var item in items)
        {
            if (!TryComp<FiberComponent>(item, out var fiber))
                continue;

            var fiberText = fiber.FiberColor == null
                ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial))
                : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial));

            if (forensics.Fibers.Add(fiberText))
                Dirty(ent.Owner, forensics);
        }
    }
}
