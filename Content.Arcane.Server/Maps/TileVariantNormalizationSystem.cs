// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Arcane.Shared.CCVar;
using Content.Shared.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;

namespace Content.Arcane.Server.Maps;

/// <summary>
/// Checks saved tile variants once when a grid initializes.
/// </summary>
public sealed partial class TileVariantNormalizationSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private TileSystem _tiles = default!;
    [Dependency] private TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapGridComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<MapGridComponent> grid, ref MapInitEvent args)
    {
        var mode = _cfg.GetCVar(ArcaneCVars.InvalidTileVariantMode);
        if (mode == "off")
            return;

        if (mode is not ("log" or "on"))
        {
            Log.Error($"Invalid tiles.invalid_variant_mode '{mode}'; expected off, log, or on.");
            return;
        }

        var mapId = Transform(grid.Owner).MapID;
        var issues = new Dictionary<int, (string Id, byte Variants, int Count, byte MaxVariant)>();
        var changes = new List<(Vector2i, Tile)>();
        var zeroVariants = new HashSet<int>();
        var count = 0;

        foreach (var tileRef in _maps.GetAllTiles(grid.Owner, grid.Comp))
        {
            var tile = tileRef.Tile;
            if (tile.IsEmpty)
                continue;

            var definition = _turf.GetContentTileDefinition(tileRef);
            if (definition.Variants == 0)
            {
                zeroVariants.Add(tile.TypeId);
                continue;
            }

            if (tile.Variant < definition.Variants)
                continue;

            count++;
            if (issues.TryGetValue(tile.TypeId, out var issue))
            {
                issues[tile.TypeId] = (
                    issue.Id, issue.Variants, issue.Count + 1,
                    tile.Variant > issue.MaxVariant ? tile.Variant : issue.MaxVariant);
            }
            else
            {
                issues.Add(tile.TypeId, (definition.ID, definition.Variants, 1, tile.Variant));
            }

            if (mode == "log")
                continue;

            var indices = tileRef.GridIndices;
            var seed = unchecked((indices.X * 397 ^ indices.Y) * 397 ^ tile.TypeId);
            var variant = definition.Variants == 1
                ? (byte) 0
                : _tiles.GetVariantTile(definition, seed).Variant;

            changes.Add((indices, new Tile(tile.TypeId, tile.Flags, variant, tile.RotationMirroring)));
        }

        if (zeroVariants.Count > 0)
            Log.Error($"Grid {grid.Owner} (map {mapId}) has tile definitions with zero variants: {string.Join(", ", zeroVariants)}. These tiles were not changed.");

        if (count == 0)
            return;

        if (mode == "log")
        {
            var details = string.Join("; ", issues.OrderBy(pair => pair.Key)
                .Select(pair => $"{pair.Value.Id}: {pair.Value.Count} cells, Variants={pair.Value.Variants}, max variant={pair.Value.MaxVariant}"));
            Log.Warning($"Found {count} invalid tile variants on grid {grid.Owner} (map {mapId}). {details}");
            return;
        }

        _maps.SetTiles(grid.Owner, grid.Comp, changes);
        Log.Warning($"Normalized {changes.Count} invalid tile variants on grid {grid.Owner} (map {mapId}).");
    }
}
