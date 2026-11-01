using Content.Arcane.Shared.Weapons.Ranged;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Arcane.Client.Weapons.Ranged;

public sealed partial class TracerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private TracerOverlay _tracerOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _tracerOverlay = new TracerOverlay(this);
        _overlay.AddOverlay(_tracerOverlay);

        SubscribeLocalEvent<TracerComponent, ComponentStartup>(OnTracerStartup);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_tracerOverlay);
    }

    private void OnTracerStartup(Entity<TracerComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.PositionHistory.Clear();
        ent.Comp.PositionHistory.Add(_transform.GetWorldPosition(ent));
        ent.Comp.EndTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Lifetime);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<TracerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tracer, out var xform))
        {
            if (curTime > tracer.EndTime)
            {
                RemCompDeferred<TracerComponent>(uid);
                continue;
            }

            var positions = tracer.PositionHistory;
            positions.Add(_transform.GetWorldPosition(xform));

            while (positions.Count > 2 && GetTrailLength(positions) > tracer.Length)
            {
                positions.RemoveAt(0);
            }
        }
    }

    private static float GetTrailLength(List<Vector2> positions)
    {
        var length = 0f;
        for (var i = 1; i < positions.Count; i++)
        {
            length += Vector2.Distance(positions[i - 1], positions[i]);
        }

        return length;
    }

    public void Draw(DrawingHandleWorld handle, MapId currentMap)
    {
        handle.SetTransform(Matrix3x2.Identity);

        var query = EntityQueryEnumerator<TracerComponent, TransformComponent>();
        while (query.MoveNext(out _, out var tracer, out var xform))
        {
            if (xform.MapID != currentMap)
                continue;

            var positions = tracer.PositionHistory;
            for (var i = 1; i < positions.Count; i++)
            {
                handle.DrawLine(positions[i - 1], positions[i], tracer.Color);
            }
        }
    }
}
