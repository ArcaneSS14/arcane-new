using System.Collections.Generic;
using System.Numerics;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Arcane.Client.Lighting.Shaders;

/// <summary>
/// Draws the blurred halo of glow-shaded sprite layers above every other sprite, so sprites drawn
/// later can't cover it. Lamps are drawn here as well, above everything including the halos.
/// </summary>
public sealed partial class GlowHaloOverlay : Overlay
{
    /// <summary>
    /// Drawn in this order. Each pass draws what the layer's own shader leaves out.
    /// </summary>
    private static readonly (ProtoId<ShaderPrototype> Shader, bool Sprite, bool Halo)[] Passes =
    [
        ("UnshadedGlow", false, true),
        ("LightGlow", false, true),
        ("LightGlow", true, false),
    ];

    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly SpriteSystem _sprite;
    private readonly SpriteTreeSystem _spriteTree;
    private readonly TransformSystem _transform;
    private readonly EntityQuery<MapGridComponent> _gridQuery;

    private readonly HashSet<ProtoId<ShaderPrototype>> _glowShaders = new();
    private readonly List<(ProtoId<ShaderPrototype> Id, ShaderInstance Shader)> _passShaders = new();
    private readonly List<Entity<SpriteComponent, TransformComponent>> _sprites = new();
    private readonly List<(ProtoId<ShaderPrototype> Shader, Matrix3x2 Matrix, Texture Texture, Color Color)> _layers = new();

    // Above entities, below FOV, so halos don't show through walls.
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public GlowHaloOverlay()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entities.System<SpriteSystem>();
        _spriteTree = _entities.System<SpriteTreeSystem>();
        _transform = _entities.System<TransformSystem>();
        _gridQuery = _entities.GetEntityQuery<MapGridComponent>();

        foreach (var (id, drawSprite, drawHalo) in Passes)
        {
            var shader = _prototypes.Index(id).InstanceUnique();
            shader.SetParameter("draw_sprite", drawSprite ? 1f : 0f);
            shader.SetParameter("draw_halo", drawHalo ? 1f : 0f);
            _passShaders.Add((id, shader));
            _glowShaders.Add(id);
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye is not { } eye)
            return;

        _sprites.Clear();
        _spriteTree.QueryAabb(_sprites, args.MapId, args.WorldBounds);

        var eyeRotation = eye.Rotation;
        var viewScale = eye.Scale * args.Viewport.RenderScale * new Vector2(EyeManager.PixelsPerMeter, -EyeManager.PixelsPerMeter);
        var viewPosition = eye.Position.Position + eye.Offset;

        _layers.Clear();
        foreach (var (uid, sprite, xform) in _sprites)
        {
            if (!HasGlowLayer(sprite))
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
        foreach (var (id, shader) in _passShaders)
        {
            handle.UseShader(shader);
            foreach (var layer in _layers)
            {
                if (layer.Shader != id)
                    continue;

                var size = layer.Texture.Size / (float) EyeManager.PixelsPerMeter;
                handle.SetTransform(layer.Matrix);
                handle.DrawTextureRect(layer.Texture, Box2.FromDimensions(size / -2, size), layer.Color);
            }
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private bool HasGlowLayer(SpriteComponent sprite)
    {
        foreach (var layer in sprite.AllLayers)
        {
            if (layer is Layer { ShaderPrototype: { } proto } && _glowShaders.Contains(proto))
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

        foreach (var spriteLayer in comp.AllLayers)
        {
            if (spriteLayer is not Layer layer)
                continue;

            if (!layer.Visible || layer.Blank || layer.CopyToShaderParameters != null)
                continue;

            if (layer.ShaderPrototype is not { } proto || !_glowShaders.Contains(proto))
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
            _layers.Add((proto, matrix, texture, comp.Color * layer.Color));
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
        foreach (var (_, shader) in _passShaders)
        {
            shader.Dispose();
        }

        base.DisposeBehavior();
    }
}
