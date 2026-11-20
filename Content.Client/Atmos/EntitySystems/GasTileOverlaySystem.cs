using Content.Client.Atmos.Overlays;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Utility; // Arcane

namespace Content.Client.Atmos.EntitySystems;

[UsedImplicitly]
public sealed partial class GasTileOverlaySystem : SharedGasTileOverlaySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GasOverlayUpdateEvent>(HandleGasOverlayUpdate);
        SubscribeLocalEvent<GasTileOverlayComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid gridUid, GasTileOverlayComponent comp, ref ComponentHandleState args)
    {
        Dictionary<Vector2i, GasOverlayChunk> modifiedChunks;

        switch (args.Current)
        {
            // is this a delta or full state?
            case GasTileOverlayDeltaState delta:
            {
                modifiedChunks = delta.ModifiedChunks;
                foreach (var index in comp.Chunks.Keys)
                {
                    if (!delta.AllChunks.Contains(index))
                        comp.Chunks.Remove(index);
                }

                break;
            }
            case GasTileOverlayState state:
            {
                modifiedChunks = state.Chunks;
                foreach (var index in comp.Chunks.Keys)
                {
                    if (!state.Chunks.ContainsKey(index))
                        comp.Chunks.Remove(index);
                }

                break;
            }
            default:
                return;
        }

        foreach (var (index, data) in modifiedChunks)
        {
            UpdateTileMasks(data); // Arcane
            comp.Chunks[index] = data;
        }
    }

    private void HandleGasOverlayUpdate(GasOverlayUpdateEvent ev)
    {
        foreach (var (nent, removedIndicies) in ev.RemovedChunks)
        {
            var grid = GetEntity(nent);

            if (!TryComp(grid, out GasTileOverlayComponent? comp))
                continue;

            foreach (var index in removedIndicies)
            {
                comp.Chunks.Remove(index);
            }
        }

        foreach (var (nent, gridData) in ev.UpdatedChunks)
        {
            var grid = GetEntity(nent);

            if (!TryComp(grid, out GasTileOverlayComponent? comp))
                continue;

            foreach (var chunkData in gridData)
            {
                UpdateTileMasks(chunkData); // Arcane
                comp.Chunks[chunkData.Index] = chunkData;
            }
        }
    }

    // Arcane-Start
    private static void UpdateTileMasks(GasOverlayChunk chunk)
    {
        DebugTools.Assert(ChunkSize * ChunkSize <= sizeof(ulong) * 8, "Gas chunks no longer fit in the tile masks.");

        var visibleGasTiles = 0ul;
        var fireTiles = 0ul;

        for (var i = 0; i < chunk.TileData.Length; i++)
        {
            ref var tile = ref chunk.TileData[i];

            if (tile.FireState != 0)
                fireTiles |= 1ul << i;

            if (tile.Opacity == null)
                continue;

            foreach (var opacity in tile.Opacity)
            {
                if (opacity == 0)
                    continue;

                visibleGasTiles |= 1ul << i;
                break;
            }
        }

        chunk.VisibleGasTiles = visibleGasTiles;
        chunk.FireTiles = fireTiles;
    }
    // Arcane-End
}
