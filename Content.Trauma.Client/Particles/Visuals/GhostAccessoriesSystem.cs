// Arcane-Start: use the particle renderer owned by Trauma.Client.
using Content.Arcane.Shared.CustomGhost;

namespace Content.Trauma.Client.Particles.Visuals;

public sealed partial class GhostAccessoriesSystem : EntitySystem
{
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private SpriteSystem _sprites = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, ActiveEmitter> _emitters = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostAccessoriesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GhostAccessoriesComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<GhostAccessoriesComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<GhostAccessoriesComponent> ent, ref MapInitEvent args) => UpdateVisuals(ent);

    private void OnState(Entity<GhostAccessoriesComponent> ent, ref AfterAutoHandleStateEvent args) => UpdateVisuals(ent);

    private void OnShutdown(Entity<GhostAccessoriesComponent> ent, ref ComponentShutdown args)
    {
        RemoveEmitter(ent);
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            UpdateLayer((ent, sprite), GhostAccessoryCategory.Hat, null);
            UpdateLayer((ent, sprite), GhostAccessoryCategory.Mask, null);
        }
    }

    private void UpdateVisuals(Entity<GhostAccessoriesComponent> ent)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            UpdateLayer((ent, sprite), GhostAccessoryCategory.Hat, ent.Comp.Hat);
            UpdateLayer((ent, sprite), GhostAccessoryCategory.Mask, ent.Comp.Mask);
        }

        RemoveEmitter(ent);
        if (ent.Comp.Particles is { } id && ProtoMan.TryIndex(id, out var accessory) && accessory.ParticleEffect is { } effect &&
            _particles.SpawnEffect(effect, _transform.GetMapCoordinates(ent.Owner), ent.Owner) is { } emitter)
            _emitters[ent.Owner] = emitter;
    }

    private void UpdateLayer(Entity<SpriteComponent?> sprite, GhostAccessoryCategory layer, ProtoId<GhostAccessoryPrototype>? id)
    {
        if (id is { } selected && ProtoMan.TryIndex(selected, out var accessory) && accessory.Sprite is { } specifier)
        {
            var index = _sprites.LayerMapReserve(sprite, layer);
            _sprites.LayerSetSprite(sprite, index, specifier);
            _sprites.LayerSetVisible(sprite, index, true);
        }
        else if (_sprites.LayerMapTryGet(sprite, layer, out var index, false))
        {
            _sprites.LayerSetVisible(sprite, index, false);
        }
    }

    private void RemoveEmitter(EntityUid uid)
    {
        if (_emitters.Remove(uid, out var emitter))
            _particles.RemoveEffectImmediately(emitter);
    }
}
// Arcane-End
