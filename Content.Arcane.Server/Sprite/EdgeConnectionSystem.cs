using Content.Arcane.Shared.Sprite;
using Robust.Shared.Map.Components;

namespace Content.Arcane.Server.Sprite;

/// <summary>
/// Calculates edge-connection masks for anchored entities on grids.
/// </summary>
public sealed partial class EdgeConnectionSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedMapSystem _map = default!;

    private static readonly (Vector2i Offset, EdgeConnectionDirections Direction, EdgeConnectionDirections Opposite)[] CardinalOffsets =
    [
        (new Vector2i(1, 0), EdgeConnectionDirections.East, EdgeConnectionDirections.West),
        (new Vector2i(-1, 0), EdgeConnectionDirections.West, EdgeConnectionDirections.East),
        (new Vector2i(0, 1), EdgeConnectionDirections.North, EdgeConnectionDirections.South),
        (new Vector2i(0, -1), EdgeConnectionDirections.South, EdgeConnectionDirections.North),
    ];

    private EntityQuery<EdgeConnectionComponent> _edgeQuery;

    public override void Initialize()
    {
        base.Initialize();

        _edgeQuery = GetEntityQuery<EdgeConnectionComponent>();

        SubscribeLocalEvent<EdgeConnectionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EdgeConnectionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EdgeConnectionComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<EdgeConnectionComponent, MoveEvent>(OnMoved);
    }

    private void OnInit(Entity<EdgeConnectionComponent> ent, ref ComponentInit args)
    {
        Recalculate(ent);
        RecalculateNeighbors(Transform(ent));
    }

    private void OnShutdown(Entity<EdgeConnectionComponent> ent, ref ComponentShutdown args)
    {
        RecalculateNeighbors(Transform(ent), ent);
    }

    private void OnAnchorChanged(Entity<EdgeConnectionComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Detaching)
        {
            RecalculateNeighbors(args.Transform, ent);
            return;
        }

        Recalculate(ent);
        RecalculateNeighbors(args.Transform);
    }

    private void OnMoved(Entity<EdgeConnectionComponent> ent, ref MoveEvent args)
    {
        var newTile = args.OldPosition.EntityId != args.NewPosition.EntityId
                      || args.OldPosition.Position.Floored() != args.NewPosition.Position.Floored();

        if (!newTile && args.OldRotation.EqualsApprox(args.NewRotation))
            return;

        Recalculate(ent);
        RecalculateNeighbors(args.Component);
    }

    private void Recalculate(Entity<EdgeConnectionComponent> ent)
    {
        var xform = Transform(ent);
        if (!TryGetTile(xform, out var gridUid, out var grid, out var tile))
        {
            _appearance.SetData(ent, EdgeConnectionVisuals.ConnectionMask, EdgeConnectionDirections.None);
            return;
        }

        var worldAllowed = Rotate(ent.Comp.AllowedDirections, xform.LocalRotation, clockwise: true);
        var mask = EdgeConnectionDirections.None;
        foreach (var (offset, direction, opposite) in CardinalOffsets)
        {
            if ((worldAllowed & direction) != 0
                && HasMatchingNeighbor(ent, xform.LocalRotation, gridUid, grid, tile + offset, ent.Comp.ConnectionKey, opposite))
            {
                mask |= direction;
            }
        }

        var localMask = Rotate(mask, xform.LocalRotation, clockwise: false);
        if (QuarterTurns(xform.LocalRotation) % 2 != 0)
            localMask = FlipEastWest(localMask);

        _appearance.SetData(ent, EdgeConnectionVisuals.ConnectionMask, localMask);
    }

    private bool HasMatchingNeighbor(EntityUid self, Angle selfRotation, EntityUid gridUid, MapGridComponent grid, Vector2i tile, string key, EdgeConnectionDirections required)
    {
        var selfTurns = QuarterTurns(selfRotation);
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (anchored.MoveNext(out var other))
        {
            if (other is not { } uid || uid == self || !_edgeQuery.TryComp(uid, out var edge) || edge.ConnectionKey != key)
                continue;

            // Being removed.
            if (edge.LifeStage > ComponentLifeStage.Running)
                continue;

            var otherXform = Transform(uid);
            if (!otherXform.Anchored || QuarterTurns(otherXform.LocalRotation) % 2 != selfTurns % 2)
                continue;

            if ((Rotate(edge.AllowedDirections, otherXform.LocalRotation, clockwise: true) & required) != 0)
                return true;
        }

        return false;
    }

    private void RecalculateNeighbors(TransformComponent xform, EntityUid? ignore = null)
    {
        if (xform.GridUid is not { } gridUid || !TryComp(gridUid, out MapGridComponent? grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        foreach (var (offset, _, _) in CardinalOffsets)
        {
            var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile + offset);
            while (anchored.MoveNext(out var other))
            {
                if (other is { } uid && uid != ignore && _edgeQuery.TryComp(uid, out var edge))
                    Recalculate((uid, edge));
            }
        }
    }

    private bool TryGetTile(TransformComponent xform, out EntityUid gridUid, out MapGridComponent grid, out Vector2i tile)
    {
        gridUid = default;
        grid = default!;
        tile = default;

        if (!xform.Anchored || xform.GridUid is not { } gridId || !TryComp(gridId, out MapGridComponent? gridComp))
            return false;

        gridUid = gridId;
        grid = gridComp;
        tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        return true;
    }

    private static EdgeConnectionDirections Rotate(EdgeConnectionDirections flags, Angle rotation, bool clockwise)
    {
        var turns = QuarterTurns(rotation);
        if (!clockwise)
            turns = (4 - turns) % 4;

        for (var i = 0; i < turns; i++)
        {
            var rotated = EdgeConnectionDirections.None;
            if ((flags & EdgeConnectionDirections.North) != 0)
                rotated |= EdgeConnectionDirections.East;
            if ((flags & EdgeConnectionDirections.East) != 0)
                rotated |= EdgeConnectionDirections.South;
            if ((flags & EdgeConnectionDirections.South) != 0)
                rotated |= EdgeConnectionDirections.West;
            if ((flags & EdgeConnectionDirections.West) != 0)
                rotated |= EdgeConnectionDirections.North;
            flags = rotated;
        }

        return flags;
    }

    private static int QuarterTurns(Angle rotation)
    {
        return ((int) Math.Round(rotation.Degrees / 90.0) % 4 + 4) % 4;
    }

    private static EdgeConnectionDirections FlipEastWest(EdgeConnectionDirections flags)
    {
        var result = flags & ~(EdgeConnectionDirections.East | EdgeConnectionDirections.West);
        if ((flags & EdgeConnectionDirections.East) != 0)
            result |= EdgeConnectionDirections.West;
        if ((flags & EdgeConnectionDirections.West) != 0)
            result |= EdgeConnectionDirections.East;
        return result;
    }
}
