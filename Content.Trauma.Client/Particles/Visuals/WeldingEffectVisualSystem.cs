// Arcane-Start
using Content.Arcane.Shared.Tools;
using Content.Trauma.Shared.Particles;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Particles.Visuals;

public sealed partial class WeldingEffectVisualSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MetaDataSystem _metadata = default!;
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ParticleEffectPrototype> Smoke = "ArcaneWeldingSmoke";
    private static readonly ProtoId<ParticleEffectPrototype> Sparks = "ArcaneWeldingSparks";
    private static readonly ProtoId<ParticleEffectPrototype> ExperimentalSparks = "ArcaneExperimentalWeldingSparks";
    private static readonly EntProtoId Light = "ArcaneWeldingLight";
    private static readonly EntProtoId ExperimentalLight = "ArcaneExperimentalWeldingLight";

    private readonly Dictionary<EntityUid, WeldVisual> _visuals = new();

    public override void Initialize()
    {
        UpdatesBefore.Add(typeof(ParticleSystem));
        SubscribeLocalEvent<WeldingEffectComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WeldingEffectComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<WeldingEffectComponent> ent, ref ComponentStartup args)
        => _visuals.TryAdd(ent, new WeldVisual(ent.Comp));

    private void OnShutdown(Entity<WeldingEffectComponent> ent, ref ComponentShutdown args)
    {
        if (_visuals.Remove(ent, out var visual))
            Hide(visual);
    }

    public override void FrameUpdate(float frameTime)
    {
        foreach (var (anchor, visual) in _visuals)
        {
            var state = visual.State;
            var elapsed = _timing.CurTime - state.StartTime;
            if (state.Duration <= TimeSpan.Zero || elapsed >= state.Duration ||
                !TryComp(anchor, out MetaDataComponent? meta) ||
                (meta.Flags & MetaDataFlags.Detached) != 0 || _metadata.EntityPaused(anchor, meta) ||
                Transform(anchor).MapID == MapId.Nullspace)
            {
                Hide(visual);
                continue;
            }

            var progress = (float) Math.Clamp(elapsed / state.Duration, 0, 1);
            var offset = Vector2.Lerp(state.StartOffset, state.EndOffset, progress);

            if (visual.Light is not { } light || TerminatingOrDeleted(light))
            {
                Hide(visual);
                var contact = new EntityCoordinates(anchor, offset);
                visual.Light = SpawnAttachedTo(state.Experimental ? ExperimentalLight : Light, contact);
                visual.LightOffset = offset;
                var origin = _transform.ToMapCoordinates(contact);
                visual.Smoke = _particles.SpawnEffect(Smoke, origin);
                visual.Sparks = _particles.SpawnEffect(state.Experimental ? ExperimentalSparks : Sparks, origin);
                continue;
            }

            if (visual.LightOffset != offset)
            {
                _transform.SetLocalPositionNoLerp(light, offset);
                visual.LightOffset = offset;
            }

            if (visual.Smoke == null && visual.Sparks == null)
                continue;

            var world = _transform.ToMapCoordinates(new EntityCoordinates(anchor, offset));
            if (visual.Smoke != null)
                visual.Smoke.MapCoords = world;
            if (visual.Sparks != null)
                visual.Sparks.MapCoords = world;
        }
    }

    private void Hide(WeldVisual visual)
    {
        if (visual.Smoke != null)
            _particles.RemoveEffectImmediately(visual.Smoke);
        if (visual.Sparks != null)
            _particles.RemoveEffectImmediately(visual.Sparks);
        if (visual.Light is { } light && !TerminatingOrDeleted(light))
        {
            _lights.SetEnabled(light, false);
            QueueDel(light);
        }

        visual.Smoke = null;
        visual.Sparks = null;
        visual.Light = null;
    }

    public override void Shutdown()
    {
        foreach (var visual in _visuals.Values)
            Hide(visual);
        _visuals.Clear();
    }

    private sealed class WeldVisual(WeldingEffectComponent state)
    {
        public readonly WeldingEffectComponent State = state;
        public ActiveEmitter? Smoke;
        public ActiveEmitter? Sparks;
        public EntityUid? Light;
        public Vector2 LightOffset;
    }
}
// Arcane-End
