using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Arcane.Common.CCVar;
using Content.Client.Graphics;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Arcane.Client.Lighting.Shaders;

/// <summary>
/// Draws the blurred halo of glow-shaded sprite layers above every other sprite, so sprites drawn
/// later can't cover it. Lamps are drawn here as well, above everything including the halos.
/// Halos and the lamp brightness boost only show at the Strong lighting shader quality.
/// </summary>
public sealed partial class GlowHaloOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedGlowShader = "UnshadedGlow";
    private static readonly ProtoId<ShaderPrototype> LightGlowShader = "LightGlow";
    private static readonly ProtoId<ShaderPrototype> UnshadedHaloShader = "UnshadedGlowHalo";
    private static readonly ProtoId<ShaderPrototype> LightHaloShader = "LightGlowHalo";

    private const int QualityStrong = 2;

    /// <summary>
    /// Finding glow sprites means checking every sprite or light around the view, so it isn't done
    /// each frame. A sprite that starts to glow can show up this late.
    /// </summary>
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(0.25);

    /// <summary>
    /// How far around the view to look for sprites, in meters, so that moving doesn't need a scan each frame.
    /// </summary>
    private const float ScanMargin = 6f;

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly SpriteTreeSystem _spriteTree;
    private readonly LightTreeSystem _lightTree;
    private readonly TransformSystem _transform;
    private readonly EntityQuery<MapGridComponent> _gridQuery;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;

    private readonly ShaderInstance _unshadedHalo;
    private readonly ShaderInstance _lightHalo;
    private readonly ShaderInstance _lamp;
    private readonly ShaderInstance _plainLamp;
    private bool _strong;

    private readonly OverlayResourceCache<ViewportGlow> _viewports = new();
    private readonly List<Entity<SpriteComponent, TransformComponent>> _scanned = new();
    private readonly List<Entity<SharedPointLightComponent, TransformComponent>> _lights = new();
    private readonly List<(Matrix3x2 Matrix, Texture Texture, Color Color)> _glowLayers = new();
    private readonly List<(Matrix3x2 Matrix, Texture Texture, Color Color)> _lampLayers = new();

    // Above entities, below FOV, so halos don't show through walls.
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public GlowHaloOverlay()
    {
        IoCManager.InjectDependencies(this);
        _spriteTree = _entities.System<SpriteTreeSystem>();
        _lightTree = _entities.System<LightTreeSystem>();
        _transform = _entities.System<TransformSystem>();
        _gridQuery = _entities.GetEntityQuery<MapGridComponent>();
        _spriteQuery = _entities.GetEntityQuery<SpriteComponent>();

        _unshadedHalo = _prototypes.Index(UnshadedHaloShader).Instance();
        _lightHalo = _prototypes.Index(LightHaloShader).Instance();

        _lamp = _prototypes.Index(LightGlowShader).InstanceUnique();
        _lamp.SetParameter("draw_sprite", 1f);

        _plainLamp = _prototypes.Index(LightGlowShader).InstanceUnique();
        _plainLamp.SetParameter("draw_sprite", 1f);
        _plainLamp.SetParameter("glow_brightness", 1f);

        _cfg.OnValueChanged(ArcaneCCVars.LightingShaderQuality, OnQualityChanged, true);
    }

    private void OnQualityChanged(int quality)
    {
        _strong = quality == QualityStrong;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye is not { } eye)
            return;

        var glow = _viewports.GetForViewport(args.Viewport, static _ => new ViewportGlow());
        if (glow.Map != args.MapId
            || glow.Strong != _strong
            || _timing.RealTime >= glow.NextScan
            || !glow.Bounds.Encloses(args.WorldAABB))
        {
            Scan(glow, args);
        }

        var eyeRotation = eye.Rotation;
        var viewScale = eye.Scale * args.Viewport.RenderScale * new Vector2(EyeManager.PixelsPerMeter, -EyeManager.PixelsPerMeter);
        var viewPosition = eye.Position.Position + eye.Offset;

        _glowLayers.Clear();
        _lampLayers.Clear();
        foreach (var (uid, sprite, xform) in glow.Sprites)
        {
            // A sprite can get deleted, hidden or put in a container between scans.
            if (sprite.Deleted || !sprite.Visible || sprite.TreeUid == null || xform.MapID != args.MapId)
                continue;

            var (worldPosition, worldRotation) = _transform.GetWorldPositionRotation(xform);

            // The engine snaps every grid to the screen pixel grid, so follow it to keep the halo aligned
            // with the sprite it surrounds.
            if (xform.GridUid is { } gridUid && _gridQuery.HasComponent(gridUid))
            {
                var gridPosition = _transform.GetWorldPosition(gridUid);
                worldPosition += PixelSnapOffset(gridPosition, viewPosition, eyeRotation, viewScale, args.Viewport.Size);
            }

            CollectLayers((uid, sprite), eyeRotation, worldRotation, worldPosition);
        }

        var handle = args.WorldHandle;
        if (_strong)
        {
            DrawLayers(handle, _unshadedHalo, _glowLayers);
            DrawLayers(handle, _lightHalo, _lampLayers);
        }

        DrawLayers(handle, _strong ? _lamp : _plainLamp, _lampLayers);

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private static void DrawLayers(DrawingHandleWorld handle, ShaderInstance shader, List<(Matrix3x2 Matrix, Texture Texture, Color Color)> layers)
    {
        handle.UseShader(shader);
        foreach (var layer in layers)
        {
            var size = layer.Texture.Size / (float) EyeManager.PixelsPerMeter;
            handle.SetTransform(layer.Matrix);
            handle.DrawTextureRect(layer.Texture, Box2.FromDimensions(size / -2, size), layer.Color);
        }
    }

    private void Scan(ViewportGlow glow, in OverlayDrawArgs args)
    {
        glow.Map = args.MapId;
        glow.Strong = _strong;
        glow.NextScan = _timing.RealTime + ScanInterval;
        glow.Bounds = args.WorldAABB.Enlarged(ScanMargin);
        glow.Sprites.Clear();

        // Lamps come from the light tree: a lamp hidden behind a wall has left the sprite tree,
        // but has to be drawn as soon as it comes back.
        _lights.Clear();
        _lightTree.QueryAabb(_lights, args.MapId, glow.Bounds);

        foreach (var (uid, _, xform) in _lights)
        {
            if (_spriteQuery.TryComp(uid, out var sprite) && HasLayer(sprite, LightGlowShader))
                glow.Sprites.Add((uid, sprite, xform));
        }

        if (!_strong)
            return;

        _scanned.Clear();
        _spriteTree.QueryAabb(_scanned, args.MapId, glow.Bounds);

        foreach (var entry in _scanned)
        {
            if (HasLayer(entry.Comp1, UnshadedGlowShader) && !HasLayer(entry.Comp1, LightGlowShader))
                glow.Sprites.Add(entry);
        }
    }

    private static bool HasLayer(SpriteComponent sprite, ProtoId<ShaderPrototype> shader)
    {
        var layers = (IReadOnlyList<ISpriteLayer>) sprite.AllLayers;
        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i] is Layer { ShaderPrototype: { } proto } && proto == shader)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Mirrors how <see cref="SpriteSystem.RenderSprite(Entity{SpriteComponent}, DrawingHandleWorld, Angle, Angle, Vector2, IReadOnlyList{PostShaderEntry}?)"/>
    /// places each layer, for the glow layers only.
    /// </summary>
    private void CollectLayers(Entity<SpriteComponent> sprite, Angle eyeRotation, Angle worldRotation, Vector2 worldPosition)
    {
        var comp = sprite.Comp;
        var angle = (worldRotation + eyeRotation).Reduced().FlipPositive();
        var cardinal = comp is { NoRotation: false, SnapCardinals: true } ? angle.RoundToCardinalAngle() : Angle.Zero;
        var entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, comp.NoRotation ? -eyeRotation : worldRotation - cardinal);
        var spriteMatrix = Matrix3x2.Multiply(comp.LocalMatrix, entityMatrix);
        Direction? overrideDirection = comp.EnableDirectionOverride ? comp.DirectionOverride : null;

        var layers = (IReadOnlyList<ISpriteLayer>) comp.AllLayers;
        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i] is not Layer layer)
                continue;

            if (!layer.Visible || layer.Blank || layer.CopyToShaderParameters != null)
                continue;

            // Sprites using UnshadedGlow draw themselves, so below Strong they have nothing to draw here.
            if (layer.ShaderPrototype is not { } proto || proto != LightGlowShader && (!_strong || proto != UnshadedGlowShader))
                continue;

            var state = layer.ActualState;
            var dir = state == null ? RsiDirection.South : Layer.GetDirection(state.RsiDirections, angle);
            layer.GetLayerDrawMatrix(dir, out var layerMatrix);

            if (overrideDirection != null && state != null)
                dir = overrideDirection.Value.Convert(state.RsiDirections);
            dir = dir.OffsetRsiDir(layer.DirOffset);

            var texture = state != null ? state.GetFrame(dir, layer.AnimationFrame) : layer.Texture;
            if (texture == null)
                continue;

            var matrix = Matrix3x2.Multiply(layerMatrix, LayerSpriteMatrix(comp, layer, spriteMatrix, worldPosition, worldRotation, eyeRotation, angle));
            var target = proto == LightGlowShader ? _lampLayers : _glowLayers;
            target.Add((matrix, texture, comp.Color * layer.Color));
        }
    }

    private static Matrix3x2 LayerSpriteMatrix(SpriteComponent sprite, Layer layer, Matrix3x2 spriteMatrix, Vector2 worldPosition, Angle worldRotation, Angle eyeRotation, Angle angle)
    {
        if (!sprite.GranularLayersRendering)
            return spriteMatrix;

        var rotation = layer.RenderingStrategy switch
        {
            LayerRenderingStrategy.Default => worldRotation,
            LayerRenderingStrategy.SnapToCardinals => worldRotation - angle.RoundToCardinalAngle(),
            LayerRenderingStrategy.NoRotation => -eyeRotation,
            _ => (Angle?) null,
        };

        return rotation is { } rot
            ? Matrix3x2.Multiply(sprite.LocalMatrix, Matrix3Helpers.CreateTransform(worldPosition, rot))
            : spriteMatrix;
    }

    private static Vector2 PixelSnapOffset(Vector2 worldPosition, Vector2 viewPosition, Angle viewRotation, Vector2 viewScale, Vector2 viewportSize)
    {
        var screenPosition = viewRotation.RotateVec(worldPosition - viewPosition) * viewScale + viewportSize / 2f;
        var screenOffset = screenPosition.Rounded() - screenPosition;
        return (-viewRotation).RotateVec(screenOffset / viewScale);
    }

    protected override void DisposeBehavior()
    {
        _cfg.UnsubValueChanged(ArcaneCCVars.LightingShaderQuality, OnQualityChanged);

        _viewports.Dispose();
        _lamp.Dispose();
        _plainLamp.Dispose();

        base.DisposeBehavior();
    }

    /// <summary>
    /// The last scan of a viewport. Viewports can show different places, like camera monitors do.
    /// </summary>
    private sealed class ViewportGlow : IDisposable
    {
        public readonly List<Entity<SpriteComponent, TransformComponent>> Sprites = new();
        public Box2 Bounds;
        public MapId Map = MapId.Nullspace;
        public bool Strong;
        public TimeSpan NextScan;

        public void Dispose()
        {
        }
    }
}
