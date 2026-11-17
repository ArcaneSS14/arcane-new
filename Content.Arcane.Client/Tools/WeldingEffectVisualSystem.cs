using System.Numerics;
using Content.Arcane.Shared.Tools;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Arcane.Client.Tools;

public sealed partial class WeldingEffectVisualSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MetaDataSystem _metadata = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId Effect = "ArcaneWeldingLight";
    private static readonly EntProtoId ExperimentalEffect = "ArcaneExperimentalWeldingLight";

    private readonly Dictionary<EntityUid, WeldVisual> _visuals = new();

    public override void Initialize()
    {
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

            if (visual.Effect is not { } effect || TerminatingOrDeleted(effect))
            {
                visual.Effect = SpawnAttachedTo(state.Experimental ? ExperimentalEffect : Effect, new EntityCoordinates(anchor, offset));
                visual.Offset = offset;
                continue;
            }

            if (visual.Offset != offset)
            {
                _transform.SetLocalPositionNoLerp(effect, offset);
                visual.Offset = offset;
            }
        }
    }

    private void Hide(WeldVisual visual)
    {
        if (visual.Effect is { } effect && !TerminatingOrDeleted(effect))
        {
            _lights.SetEnabled(effect, false);
            QueueDel(effect);
        }

        visual.Effect = null;
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
        public EntityUid? Effect;
        public Vector2 Offset;
    }
}
