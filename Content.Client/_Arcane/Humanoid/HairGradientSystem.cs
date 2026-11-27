// Arcane-Start: hair gradient rendering
using System.Linq;
using System.Numerics;
using Content.Arcane.Common.Preferences;
using Content.Client.Body;
using Content.Client.DisplacementMap;
using Content.Client.Stylesheets.Colorspace;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Humanoid;

/// <summary>
/// Draws hair marking layers with the character's hair gradient (<see cref="CharacterCustomization"/>).
/// The body system reports every hair layer it builds; this system keeps one shader per layer and refreshes the
/// layers when the character's gradient changes.
/// </summary>
public sealed partial class HairGradientSystem : EntitySystem
{
    public static readonly ProtoId<ShaderPrototype> ShaderPrototype = "ArcaneHairGradient";

    private static readonly Vector4 NoFrame = new(-1f);

    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private readonly Dictionary<EntityUid, Tracked> _tracked = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpriteComponent, VisualBodyLayersChangedEvent>(OnLayersChanged);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var (uid, tracked) in _tracked)
            DisposeAll(uid, tracked);

        _tracked.Clear();
    }

    /// <summary>
    /// Called by the body system for every hair marking layer it adds or updates on <paramref name="target"/>.
    /// </summary>
    public void OnHairLayer(EntityUid target, string layerId, SpriteSpecifier.Rsi rsi)
    {
        var tracked = _tracked.GetOrNew(target);
        if (TryComp<SpriteComponent>(target, out var sprite)
            && _sprite.LayerMapTryGet((target, sprite), layerId, out var index, false)
            && sprite[index] is SpriteComponent.Layer layer)
        {
            // VisualBody has just assigned the marking color and its non-gradient shader.
            tracked.Colors[layerId] = layer.Color;
            if (!tracked.Shaders.TryGetValue(layerId, out var gradient) || !ReferenceEquals(layer.Shader, gradient))
                tracked.OriginalShaders[layerId] = (layer.Shader, layer.ShaderPrototype?.Id);
        }
        tracked.Layers[layerId] = rsi;
        Apply(target, tracked, layerId, rsi);
    }

    private void OnLayersChanged(Entity<SpriteComponent> ent, ref VisualBodyLayersChangedEvent args)
    {
        if (!_tracked.TryGetValue(ent.Owner, out var tracked))
            return;

        foreach (var layerId in tracked.Layers.Keys.ToArray())
        {
            if (_sprite.LayerMapTryGet(ent.AsNullable(), layerId, out _, false))
                continue;
            tracked.Layers.Remove(layerId);
            tracked.Colors.Remove(layerId);
            tracked.OriginalShaders.Remove(layerId);
            if (tracked.Shaders.Remove(layerId, out var shader))
                shader.Dispose();
        }

        if (tracked.Layers.Count == 0)
            _tracked.Remove(ent.Owner);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_tracked.Count == 0)
            return;

        List<EntityUid>? gone = null;
        foreach (var (uid, tracked) in _tracked)
        {
            if (!TryComp<HumanoidProfileComponent>(uid, out var profile) || !TryComp<SpriteComponent>(uid, out _))
            {
                (gone ??= new List<EntityUid>()).Add(uid);
                continue;
            }

            if (Equals(tracked.Applied, profile.Customization))
                continue;

            foreach (var (layerId, rsi) in tracked.Layers.ToArray())
                Apply(uid, tracked, layerId, rsi);
            tracked.Applied = profile.Customization.Clone();
        }

        if (gone == null)
            return;

        foreach (var uid in gone)
        {
            if (_tracked.Remove(uid, out var tracked))
                DisposeAll(uid, tracked);
        }
    }

    private void Apply(EntityUid uid, Tracked tracked, string layerId, SpriteSpecifier.Rsi rsi)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !_sprite.LayerMapTryGet((uid, sprite), layerId, out var index, false))
        {
            // The marking is gone.
            tracked.Layers.Remove(layerId);
            tracked.Colors.Remove(layerId);
            tracked.OriginalShaders.Remove(layerId);
            if (tracked.Shaders.Remove(layerId, out var stale))
                stale.Dispose();
            return;
        }

        var customization = CompOrNull<HumanoidProfileComponent>(uid)?.Customization;
        if (customization is not { HairGradientEnabled: true })
        {
            if (tracked.Shaders.Remove(layerId, out var old))
            {
                RestoreLayer(uid, sprite, tracked, layerId, index, old);
                old.Dispose();
            }

            return;
        }

        if (!tracked.Shaders.TryGetValue(layerId, out var shader))
        {
            shader = _prototype.Index(ShaderPrototype).InstanceUnique();
            tracked.Shaders[layerId] = shader;
        }

        var colors = customization.HairGradientColors;
        var roots = colors.Count > 0 ? colors[0] : Color.Black;
        var tips = colors.Count > 1 ? colors[1] : roots;
        shader.SetParameter("rootsLab", ToOklab(roots));
        shader.SetParameter("tipsLab", ToOklab(tips));
        shader.SetParameter("gradientStyle", (int) customization.HairGradientStyle);
        shader.SetParameter("gradientOffset", customization.HairGradientOffset);
        var displacementKey = DisplacementMapSystem.BuildDisplacementLayerKey(layerId);
        var displaced = displacementKey != null && _sprite.LayerMapTryGet((uid, sprite), displacementKey, out _, false);
        shader.SetParameter("applyDisplacement", displaced ? 1 : 0);

        // The shader cannot tell which direction is drawn, so it compares UV against each direction's atlas frame.
        var state = _sprite.GetState(rsi);
        shader.SetParameter("frameWidth", (float) state.Size.X);
        shader.SetParameter("frameNorth", GetFrameUv(state, RsiDirection.North));
        shader.SetParameter("frameEast", GetFrameUv(state, RsiDirection.East));
        shader.SetParameter("frameWest", GetFrameUv(state, RsiDirection.West));
        if (tracked.Colors.TryGetValue(layerId, out var color))
            _sprite.LayerSetColor((uid, sprite), index, Color.White.WithAlpha(color.A));
        sprite.LayerSetShader(index, shader);
    }

    private void RestoreLayer(EntityUid uid, SpriteComponent sprite, Tracked tracked, string layerId, int index, ShaderInstance gradient)
    {
        if (sprite[index] is SpriteComponent.Layer layer && ReferenceEquals(layer.Shader, gradient))
        {
            if (tracked.OriginalShaders.TryGetValue(layerId, out var original) && original.Prototype == SpriteSystem.UnshadedId.Id)
                sprite.LayerSetShader(index, SpriteSystem.UnshadedId.Id);
            else if (tracked.OriginalShaders.TryGetValue(layerId, out original))
                sprite.LayerSetShader(index, original.Shader, original.Prototype);
            else
                sprite.LayerSetShader(index, (ShaderInstance?) null);
        }

        if (tracked.Colors.TryGetValue(layerId, out var color))
            _sprite.LayerSetColor((uid, sprite), index, color);
    }

    private void DisposeAll(EntityUid uid, Tracked tracked)
    {
        TryComp<SpriteComponent>(uid, out var sprite);
        foreach (var (layerId, shader) in tracked.Shaders)
        {
            // Detach first: a renderer may still hold the layer.
            if (sprite != null &&
                _sprite.LayerMapTryGet((uid, sprite), layerId, out var index, false) &&
                sprite[index] is SpriteComponent.Layer layer &&
                ReferenceEquals(layer.Shader, shader))
            {
                RestoreLayer(uid, sprite, tracked, layerId, index, shader);
            }

            shader.Dispose();
        }

        tracked.Shaders.Clear();
    }

    private static Vector3 ToOklab(Color color)
    {
        var lab = color.LabFromSrgb();
        return new Vector3(lab.X, lab.Y, lab.Z);
    }

    private static Vector4 GetFrameUv(RSI.State state, RsiDirection direction)
    {
        if (state.RsiDirections == RsiDirectionType.Dir1 || state.GetFrame(direction, 0) is not AtlasTexture frame)
            return NoFrame;

        var (width, height) = frame.SourceTexture.Size;
        var region = frame.SubRegion;
        return new Vector4(
            region.Left / width,
            (height - region.Bottom) / height,
            region.Right / width,
            (height - region.Top) / height);
    }

    private sealed partial class Tracked
    {
        public readonly Dictionary<string, SpriteSpecifier.Rsi> Layers = new();
        public readonly Dictionary<string, ShaderInstance> Shaders = new();
        public readonly Dictionary<string, Color> Colors = new();
        public readonly Dictionary<string, (ShaderInstance? Shader, string? Prototype)> OriginalShaders = new();
        public CharacterCustomization? Applied;
    }
}
// Arcane-End
