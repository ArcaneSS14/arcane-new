using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.Lighting.Shaders;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Arcane.Client.Lighting.Shaders;

public sealed class LightingOverlay : Overlay
{
    private readonly EntityManager _entityManager;
    private readonly SpriteSystem _spriteSystem;
    private readonly TransformSystem _transformSystem;
    private readonly IConfigurationManager _cfg;
    private readonly ShaderInstance _shader;
    private readonly Action<bool> _lightCvarChanged;
    private readonly Action<float> _lightScaleChanged;
    private bool _enableGlowing;
    private bool _useLowResMask;
    private readonly List<(Vector2 Pos, Color Color, Matrix3x2 Matrix, LightingOverlayComponent Comp)> _visible = new();
    private const float OverlapRadius = 4f;
    private const float OverlapStrength = 2f;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    private static readonly ProtoId<ShaderPrototype> LightingOverlayShader = "LightingOverlay";

    public LightingOverlay(EntityManager entityManager, IPrototypeManager prototypeManager)
    {
        _entityManager = entityManager;
        _spriteSystem = entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _transformSystem = entityManager.EntitySysManager.GetEntitySystem<TransformSystem>();
        _cfg = IoCManager.Resolve<IConfigurationManager>();
        _lightCvarChanged = value => _enableGlowing = value;
        _cfg.OnValueChanged(ArcaneCCVars.EnableLightsGlowing, _lightCvarChanged, true);
        _lightScaleChanged = value => _useLowResMask = value <= 0.5f;
        _cfg.OnValueChanged(CVars.LightResolutionScale, _lightScaleChanged, true);

        _shader = prototypeManager.Index(LightingOverlayShader).InstanceUnique();
        ZIndex = (int) DrawDepth.Overdoors;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_enableGlowing || ScreenTexture == null)
            return;

        var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(5f);

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.UseShader(_shader);

        _visible.Clear();
        var query = _entityManager.AllEntityQueryEnumerator<LightingOverlayComponent, PointLightComponent, TransformComponent>();
        while (query.MoveNext(out _, out var component, out var pointLight, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            if (component.Enabled is false || !pointLight.Enabled)
                continue;

            var worldPos = _transformSystem.GetWorldPosition(xform, xformQuery);
            if (!bounds.Contains(worldPos))
                continue;

            var color = component.Color ?? pointLight.Color;
            var (_, _, worldMatrix) = _transformSystem.GetWorldPositionRotationMatrix(xform, xformQuery);
            _visible.Add((worldPos, color, worldMatrix, component));
        }

        foreach (var (pos, baseColor, worldMatrix, component) in _visible)
        {
            var overlap = 0f;
            foreach (var other in _visible)
            {
                var distance = (other.Pos - pos).Length();
                if (distance < OverlapRadius)
                    overlap += 1f - distance / OverlapRadius;
            }

            var color = baseColor.WithAlpha(baseColor.A / (1f + (overlap - 1f) * OverlapStrength));
            handle.SetTransform(worldMatrix);

            var mask = _spriteSystem.Frame0(_useLowResMask && component.SpriteLow != null ? component.SpriteLow : component.Sprite);
            var xOffset = component.OffsetX - mask.Width / 2f / EyeManager.PixelsPerMeter;
            var yOffset = component.OffsetY - mask.Height / 2f / EyeManager.PixelsPerMeter;
            var textureVector = new Vector2(xOffset, yOffset);

            handle.DrawTexture(mask, textureVector, color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _cfg.UnsubValueChanged(ArcaneCCVars.EnableLightsGlowing, _lightCvarChanged);
        _cfg.UnsubValueChanged(CVars.LightResolutionScale, _lightScaleChanged);
        base.DisposeBehavior();
    }
}
