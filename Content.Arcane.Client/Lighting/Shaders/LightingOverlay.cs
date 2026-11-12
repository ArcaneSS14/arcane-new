using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.Lighting.Shaders;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Arcane.Client.Lighting.Shaders;

/// <summary>
/// Draws the glow masks of lamps. Only Strong quality matches the mask to the background, which needs
/// a copy of the screen; Silly blends against it directly and doesn't pay for the copy.
/// </summary>
public sealed class LightingOverlay : Overlay
{
    private readonly EntityManager _entityManager;
    private readonly SpriteSystem _spriteSystem;
    private readonly TransformSystem _transformSystem;
    private readonly LightTreeSystem _lightTree;
    private readonly EntityQuery<LightingOverlayComponent> _overlayQuery;
    private readonly IConfigurationManager _cfg;
    private readonly ShaderInstance _shader;
    private readonly ShaderInstance _lowShader;
    private readonly Action<bool> _lightCvarChanged;
    private readonly Action<float> _lightScaleChanged;
    private readonly Action<int> _qualityChanged;
    private bool _enableGlowing;
    private int _quality;
    private bool _useLowResMask;
    private readonly List<Entity<SharedPointLightComponent, TransformComponent>> _lights = new();
    private readonly List<(Vector2 Pos, Color Color, Matrix3x2 Matrix, LightingOverlayComponent Comp)> _visible = new();
    private readonly Dictionary<SpriteSpecifier, Texture> _masks = new();

    // Visible lights by OverlapRadius sized cell, so a light only has to look at lights in the neighbouring cells.
    private readonly Dictionary<Vector2i, List<int>> _cells = new();
    private readonly Stack<List<int>> _cellPool = new();
    private const float OverlapRadius = 4f;
    private const float OverlapStrength = 2f;
    private const int QualityNone = 0;
    private const int QualitySilly = 1;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => _quality != QualitySilly;

    private static readonly ProtoId<ShaderPrototype> LightingOverlayShader = "LightingOverlay";
    private static readonly ProtoId<ShaderPrototype> LightingOverlayLowShader = "LightingOverlayLow";

    public LightingOverlay(EntityManager entityManager, IPrototypeManager prototypeManager)
    {
        _entityManager = entityManager;
        _spriteSystem = entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _transformSystem = entityManager.EntitySysManager.GetEntitySystem<TransformSystem>();
        _lightTree = entityManager.EntitySysManager.GetEntitySystem<LightTreeSystem>();
        _overlayQuery = entityManager.GetEntityQuery<LightingOverlayComponent>();
        _cfg = IoCManager.Resolve<IConfigurationManager>();
        _lightCvarChanged = value => _enableGlowing = value;
        _cfg.OnValueChanged(ArcaneCCVars.EnableLightsGlowing, _lightCvarChanged, true);
        _qualityChanged = value => _quality = value;
        _cfg.OnValueChanged(ArcaneCCVars.LightingShaderQuality, _qualityChanged, true);
        _lightScaleChanged = value => _useLowResMask = value <= 0.5f;
        _cfg.OnValueChanged(CVars.LightResolutionScale, _lightScaleChanged, true);

        _shader = prototypeManager.Index(LightingOverlayShader).InstanceUnique();
        _lowShader = prototypeManager.Index(LightingOverlayLowShader).Instance();
        ZIndex = (int) DrawDepth.Overdoors;
    }

    /// <summary>
    /// Runs before the engine copies the screen, so a view without lamps costs nothing.
    /// </summary>
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _visible.Clear();

        if (!_enableGlowing || _quality == QualityNone)
            return false;

        var bounds = args.WorldAABB.Enlarged(5f);

        _lights.Clear();
        _lightTree.QueryAabb(_lights, args.MapId, bounds);

        foreach (var (uid, pointLight, xform) in _lights)
        {
            if (!pointLight.Enabled || !_overlayQuery.TryComp(uid, out var component) || component.Enabled is false)
                continue;

            var worldMatrix = _transformSystem.GetWorldMatrix(xform);
            if (!bounds.Contains(worldMatrix.Translation))
                continue;

            _visible.Add((worldMatrix.Translation, component.Color ?? pointLight.Color, worldMatrix, component));
        }

        return _visible.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        if (_quality == QualitySilly)
        {
            handle.UseShader(_lowShader);
        }
        else
        {
            if (ScreenTexture == null)
                return;

            _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            handle.UseShader(_shader);
        }

        // Silly quality, or a reduced light resolution, draws the low resolution masks.
        var lowMask = _useLowResMask || _quality == QualitySilly;
        BuildCells();
        foreach (var (pos, baseColor, worldMatrix, component) in _visible)
        {
            var overlap = GetOverlap(pos);
            var color = baseColor.WithAlpha(baseColor.A / (1f + (overlap - 1f) * OverlapStrength));
            handle.SetTransform(worldMatrix);

            var mask = GetMask(lowMask && component.SpriteLow != null ? component.SpriteLow : component.Sprite);
            var xOffset = component.OffsetX - mask.Width / 2f / EyeManager.PixelsPerMeter;
            var yOffset = component.OffsetY - mask.Height / 2f / EyeManager.PixelsPerMeter;
            var textureVector = new Vector2(xOffset, yOffset);

            handle.DrawTexture(mask, textureVector, color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private static Vector2i CellOf(Vector2 position)
    {
        return new Vector2i((int) MathF.Floor(position.X / OverlapRadius), (int) MathF.Floor(position.Y / OverlapRadius));
    }

    private void BuildCells()
    {
        foreach (var cell in _cells.Values)
        {
            cell.Clear();
            _cellPool.Push(cell);
        }

        _cells.Clear();

        for (var i = 0; i < _visible.Count; i++)
        {
            var key = CellOf(_visible[i].Pos);
            if (!_cells.TryGetValue(key, out var cell))
                _cells[key] = cell = _cellPool.Count > 0 ? _cellPool.Pop() : new List<int>();

            cell.Add(i);
        }
    }

    /// <summary>
    /// How much the lights within <see cref="OverlapRadius"/> of a position overlap it, counting a light there.
    /// Cells are as wide as the radius, so any light close enough is in one of the 3x3 cells around it.
    /// </summary>
    private float GetOverlap(Vector2 position)
    {
        var overlap = 0f;
        var center = CellOf(position);

        for (var y = -1; y <= 1; y++)
        {
            for (var x = -1; x <= 1; x++)
            {
                if (!_cells.TryGetValue(center + new Vector2i(x, y), out var cell))
                    continue;

                foreach (var index in cell)
                {
                    var distanceSquared = (_visible[index].Pos - position).LengthSquared();
                    if (distanceSquared < OverlapRadius * OverlapRadius)
                        overlap += 1f - MathF.Sqrt(distanceSquared) / OverlapRadius;
                }
            }
        }

        return overlap;
    }

    private Texture GetMask(SpriteSpecifier specifier)
    {
        if (!_masks.TryGetValue(specifier, out var mask))
            _masks[specifier] = mask = _spriteSystem.Frame0(specifier);

        return mask;
    }

    protected override void DisposeBehavior()
    {
        _cfg.UnsubValueChanged(ArcaneCCVars.EnableLightsGlowing, _lightCvarChanged);
        _cfg.UnsubValueChanged(ArcaneCCVars.LightingShaderQuality, _qualityChanged);
        _cfg.UnsubValueChanged(CVars.LightResolutionScale, _lightScaleChanged);
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
