using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Station.Systems;

public sealed partial class ServerStationSystem
{
    [Dependency] private TurfSystem _turf = default!;

    public override bool TryFindTileOnGrid(Entity<MapGridComponent> grid,
        out Vector2i tile,
        out EntityCoordinates targetCoords,
        int tries = 10)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;

        var aabb = grid.Comp.LocalAABB;

        for (var i = 0; i < tries; i++)
        {
            var randomX = Random.Next((int) aabb.Left, (int) aabb.Right);
            var randomY = Random.Next((int) aabb.Bottom, (int) aabb.Top);

            tile = new Vector2i(randomX, randomY);

            // Arcane-Edit-Start: Share tile eligibility with the antagonist spawn fallback.
            if (!Map.TryGetTile(grid.Comp, tile, out var selectedTile) ||
                !IsValidSpawnTile(grid, tile, selectedTile))
                continue;
            // Arcane-Edit-End

            targetCoords = Map.GridTileToLocal(grid.Owner, grid.Comp, tile);
            return true;
        }

        return false;
    }

    // Arcane-Start: Exhaust valid station tiles only after antagonist spawn sampling fails.
    internal bool TryFindFallbackSpawnTile(Entity<MapGridComponent> grid, out EntityCoordinates coordinates)
    {
        var candidates = Map.GetAllTiles(grid, grid.Comp);
        var count = 0;
        var chosen = default(Vector2i);
        while (candidates.MoveNext(out var candidate))
        {
            var tile = candidate.Value;
            if (!IsValidSpawnTile(grid, tile.GridIndices, tile.Tile))
                continue;

            count++;
            if (Random.Next(count) == 0)
                chosen = tile.GridIndices;
        }

        coordinates = count > 0
            ? Map.GridTileToLocal(grid.Owner, grid.Comp, chosen)
            : EntityCoordinates.Invalid;
        return count > 0;
    }

    private bool IsValidSpawnTile(Entity<MapGridComponent> grid, Vector2i indices, Tile tile)
        => !tile.IsEmpty && !_turf.IsSpace(tile) &&
           !_atmos.IsTileSpace(grid.Owner, Transform(grid.Owner).MapUid, indices) &&
           !_atmos.IsTileAirBlockedCached(grid.Owner, indices);
    // Arcane-End
}
