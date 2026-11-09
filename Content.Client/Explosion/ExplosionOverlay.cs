using System.Numerics;
using Content.Shared.Explosion.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing; // Arcane

namespace Content.Client.Explosion;

[UsedImplicitly]
public sealed partial class ExplosionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private IGameTiming _timing = default!; // Arcane-Edit: IRobustRandom _robustRandom > IGameTiming _timing
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    private readonly SharedTransformSystem _transformSystem;
    private SharedAppearanceSystem _appearance;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private ShaderInstance _shader;


    public ExplosionOverlay(SharedAppearanceSystem appearanceSystem)
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(UnshadedShader).Instance();
        _transformSystem = _entMan.System<SharedTransformSystem>();
        _appearance = appearanceSystem;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var drawHandle = args.WorldHandle;
        drawHandle.UseShader(_shader);

        var xforms = _entMan.GetEntityQuery<TransformComponent>();
        var query = _entMan.EntityQueryEnumerator<ExplosionVisualsComponent, ExplosionVisualsTexturesComponent>();

        while (query.MoveNext(out var uid, out var visuals, out var textures))
        {
            if (visuals.Epicenter.MapId != args.MapId)
                continue;

            if (!_appearance.TryGetData(uid, ExplosionAppearanceData.Progress, out int index))
                continue;

            index = Math.Min(index, visuals.Intensity.Count - 1);
            DrawExplosion(drawHandle, args.WorldBounds, visuals, index, xforms, textures);
        }

        drawHandle.SetTransform(Matrix3x2.Identity);
        drawHandle.UseShader(null);
    }

    private void DrawExplosion(
        DrawingHandleWorld drawHandle,
        Box2Rotated worldBounds,
        ExplosionVisualsComponent visuals,
        int index,
        EntityQuery<TransformComponent> xforms,
        ExplosionVisualsTexturesComponent textures)
    {
        Box2 gridBounds;
        foreach (var (gridId, tiles) in visuals.Tiles)
        {
            if (!_entMan.TryGetComponent(gridId, out MapGridComponent? grid))
                continue;

            var xform = xforms.GetComponent(gridId);
            var (_, _, worldMatrix, invWorldMatrix) = _transformSystem.GetWorldPositionRotationMatrixWithInv(xform, xforms);

            gridBounds = invWorldMatrix.TransformBox(worldBounds).Enlarged(grid.TileSize * 2);
            drawHandle.SetTransform(worldMatrix);

            DrawTiles(drawHandle, gridBounds, index, tiles, visuals, grid.TileSize, textures);
        }

        if (visuals.SpaceTiles == null)
            return;

        Matrix3x2.Invert(visuals.SpaceMatrix, out var invSpace);
        gridBounds = invSpace.TransformBox(worldBounds).Enlarged(2);
        drawHandle.SetTransform(visuals.SpaceMatrix);

        DrawTiles(drawHandle, gridBounds, index, visuals.SpaceTiles, visuals, visuals.SpaceTileSize, textures);
    }

    private void DrawTiles(
        DrawingHandleWorld drawHandle,
        Box2 gridBounds,
        int index,
        Dictionary<int, List<Vector2i>> tileSets,
        ExplosionVisualsComponent visuals,
        ushort tileSize,
        ExplosionVisualsTexturesComponent textures)
    {
        for (var j = 0; j <= index; j++)
        {
            if (!tileSets.TryGetValue(j, out var tiles))
                continue;

            var frameIndex = (int) Math.Min(visuals.Intensity[j] / textures.IntensityPerState, textures.FireFrames.Count - 1);
            var frames = textures.FireFrames[frameIndex];
            var texture = GetAnimatedFrame(frames, textures.FireDelays[frameIndex]); // Arcane: one frame for the whole state

            foreach (var tile in tiles)
            {
                var centre = (tile + Vector2Helpers.Half) * tileSize;

                if (!gridBounds.Contains(centre))
                    continue;

                // var texture = _robustRandom.Pick(frames); // Arcane-Edit: random frame > animated frame picked above
                drawHandle.DrawTextureRect(texture, Box2.CenteredAround(centre, new Vector2(tileSize, tileSize)), textures.FireColor);
            }
        }
    }

    // Arcane-Start
    /// <summary>
    ///     The frame of an animated state at the current time, following the state's own frame delays.
    /// </summary>
    private Texture GetAnimatedFrame(Texture[] frames, float[] delays)
    {
        var total = 0f;
        foreach (var delay in delays)
        {
            total += delay;
        }

        if (frames.Length <= 1 || delays.Length != frames.Length || total <= 0f)
            return frames[0];

        var time = (float) (_timing.RealTime.TotalSeconds % total);
        for (var i = 0; i < frames.Length; i++)
        {
            time -= delays[i];
            if (time < 0f)
                return frames[i];
        }

        return frames[^1];
    }
    // Arcane-End
}
