// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Map.Components;

namespace Content.Arcane.Server.Doors;

/// <summary>
/// Keeps the phantom airtight entities of a multi-tile airlock in place and in sync with its air blocking (Orion #466).
/// </summary>
public sealed partial class MultiTileAirlockAirtightSystem : EntitySystem
{
    [Dependency] private AirtightSystem _airtight = default!;
    [Dependency] private SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultiTileAirlockAirtightComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MultiTileAirlockAirtightComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<MultiTileAirlockAirtightComponent, ReAnchorEvent>(OnReAnchor);
        SubscribeLocalEvent<MultiTileAirlockAirtightComponent, AirtightChanged>(OnAirtightChanged);
        SubscribeLocalEvent<MultiTileAirlockAirtightComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<MultiTileAirlockAirtightComponent> ent, ref MapInitEvent args)
    {
        Respawn(ent);
    }

    private void OnAnchorChanged(Entity<MultiTileAirlockAirtightComponent> ent, ref AnchorStateChangedEvent args)
    {
        Respawn(ent);
    }

    private void OnReAnchor(Entity<MultiTileAirlockAirtightComponent> ent, ref ReAnchorEvent args)
    {
        Respawn(ent);
    }

    private void OnAirtightChanged(Entity<MultiTileAirlockAirtightComponent> ent, ref AirtightChanged args)
    {
        foreach (var phantom in ent.Comp.Phantoms)
        {
            if (TryComp<AirtightComponent>(phantom, out var airtight))
                _airtight.SetAirblocked((phantom, airtight), args.Airtight.AirBlocked);
        }
    }

    private void OnShutdown(Entity<MultiTileAirlockAirtightComponent> ent, ref ComponentShutdown args)
    {
        DeletePhantoms(ent);
    }

    private void Respawn(Entity<MultiTileAirlockAirtightComponent> ent)
    {
        DeletePhantoms(ent);

        var xform = Transform(ent);
        if (TerminatingOrDeleted(ent)
            || !xform.Anchored
            || xform.GridUid is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var grid)
            || !TryComp<AirtightComponent>(ent, out var doorAirtight))
            return;

        var anchorTile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        foreach (var offset in ent.Comp.TileOffsets)
        {
            var rotated = xform.LocalRotation.RotateVec(new Vector2(offset.X, offset.Y));
            var tile = anchorTile + new Vector2i((int) MathF.Round(rotated.X), (int) MathF.Round(rotated.Y));

            var phantom = Spawn(ent.Comp.Phantom, _map.GridTileToLocal(gridUid, grid, tile));
            if (TryComp<AirtightComponent>(phantom, out var airtight))
                _airtight.SetAirblocked((phantom, airtight), doorAirtight.AirBlocked);

            ent.Comp.Phantoms.Add(phantom);
        }
    }

    private void DeletePhantoms(Entity<MultiTileAirlockAirtightComponent> ent)
    {
        foreach (var phantom in ent.Comp.Phantoms)
        {
            if (!TerminatingOrDeleted(phantom))
                QueueDel(phantom);
        }

        ent.Comp.Phantoms.Clear();
    }
}
