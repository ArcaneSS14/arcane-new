using Content.Client.Humanoid;
using Content.Shared.Body;
using Content.Arcane.Shared.Invisibility;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Arcane.Client.Invisibility;

/// <summary>
///     Applies the arcane invisibility shader only to the humanoid's body sprite layers (skin,
///     hair, markings). Clothing layers never receive the shader, so an invisible arcane person
///     looks like a translucent/ghostly body wearing perfectly solid, opaque clothes.
/// </summary>
public sealed partial class InvisibilityVisualSystem : EntitySystem
{
    private static readonly ProtoId<ShaderPrototype> ShaderPrototype = "ArcaneInvisibility";
    private static readonly HumanoidVisualLayers[] HumanoidLayers = Enum.GetValues<HumanoidVisualLayers>();

    [Dependency] private BodySystem _body = default!;
    [Dependency] private HairGradientSystem _hairGradient = default!;
    [Dependency] private MarkingManager _marking = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private readonly Dictionary<EntityUid, ShaderInstance> _shaders = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcaneInvisibilityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ArcaneInvisibilityComponent, AfterAutoHandleStateEvent>(OnArcaneState);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var (uid, shader) in _shaders)
        {
            DetachShader(uid, shader);
            shader.Dispose();
        }

        _shaders.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ArcaneInvisibilityComponent, HumanoidProfileComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var arcane, out _, out _))
            ApplyShader(uid, arcane);
    }

    private void OnArcaneState(Entity<ArcaneInvisibilityComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ApplyShader(ent.Owner, ent.Comp);
    }

    private void OnShutdown(Entity<ArcaneInvisibilityComponent> ent, ref ComponentShutdown args)
    {
        RemoveShader(ent.Owner);
    }

    private void ApplyShader(EntityUid uid, ArcaneInvisibilityComponent arcane)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var shader = GetShader(uid, arcane.ShaderVisibility);
        foreach (var index in BodyLayers((uid, sprite)))
            sprite.LayerSetShader(index, shader);
    }

    /// <summary>
    /// Body part layers (keyed by <see cref="HumanoidVisualLayers"/>) and the marking layers of the body's organs;
    /// clothing and held items keep their own look.
    /// </summary>
    private IEnumerable<int> BodyLayers(Entity<SpriteComponent> ent)
    {
        foreach (var layer in HumanoidLayers)
        {
            if (_sprite.LayerMapTryGet(ent.AsNullable(), layer, out var index, false))
                yield return index;
        }

        foreach (var organ in _body.GetOrgans(ent.Owner))
        {
            if (!TryComp<VisualOrganMarkingsComponent>(organ, out var markings))
                continue;

            foreach (var marking in markings.AppliedMarkings)
            {
                if (!_marking.TryGetMarking(marking, out var proto))
                    continue;

                foreach (var spec in proto.Sprites)
                {
                    if (spec is SpriteSpecifier.Rsi rsi
                        && _sprite.LayerMapTryGet(ent.AsNullable(), $"{proto.ID}-{rsi.RsiState}", out var index, false))
                        yield return index;
                }
            }
        }
    }

    private void RemoveShader(EntityUid uid)
    {
        if (!_shaders.TryGetValue(uid, out var cached))
            return;

        DetachShader(uid, cached);

        // Layers that carry their own shader, such as the hair gradient, get it back.
        if (!TerminatingOrDeleted(uid))
            _hairGradient.Reapply(uid);

        _shaders.Remove(uid);
        cached.Dispose();
    }

    private void DetachShader(EntityUid uid, ShaderInstance shader)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var index = 0;
        foreach (var layer in sprite.AllLayers)
        {
            if (layer is SpriteComponent.Layer actual && ReferenceEquals(actual.Shader, shader))
                sprite.LayerSetShader(index, (ShaderInstance?) null);

            index++;
        }
    }

    private ShaderInstance GetShader(EntityUid uid, float visibility)
    {
        if (_shaders.TryGetValue(uid, out var shader))
        {
            shader.SetParameter("visibility", visibility);
            return shader;
        }

        shader = _protoMan.Index(ShaderPrototype).InstanceUnique();
        shader.SetParameter("visibility", visibility);
        _shaders[uid] = shader;
        return shader;
    }
}
