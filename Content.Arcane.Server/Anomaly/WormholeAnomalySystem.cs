using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Anomaly;

public sealed partial class WormholeAnomalySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WormholeAnomalyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<WormholeAnomalyComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextPulse = _timing.CurTime + ent.Comp.PulseInterval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WormholeAnomalyComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (now < comp.NextPulse)
                continue;

            comp.NextPulse = now + comp.PulseInterval;

            var offset = _random.NextVector2(comp.MaxShuffleRadius);
            _transform.SetWorldPosition((uid, xform), _transform.GetWorldPosition(xform) + offset);
            _audio.PlayPvs(comp.TeleportSound, uid);
        }
    }
}
