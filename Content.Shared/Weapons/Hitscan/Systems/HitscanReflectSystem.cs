using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed partial class HitscanReflectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanReflectComponent, AttemptHitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanReflectComponent> hitscan, ref AttemptHitscanRaycastFiredEvent args)
    {
        var data = args.Data;

        if (hitscan.Comp.ReflectiveType == ReflectType.None || data.HitEntity == null)
            return;

        if (hitscan.Comp.CurrentReflections >= hitscan.Comp.MaxReflections)
            return;

        var ev = new HitScanReflectAttemptEvent(data.Shooter ?? data.Gun, data.Gun, hitscan.Comp.ReflectiveType, data.ShotDirection, false,
            CompOrNull<HitscanBasicDamageComponent>(hitscan)?.Damage, data.HitEntity.Value); // Trauma - shitcode version of old goob thing from #1152, it needs rework for new system. And pass the target.
        RaiseLocalEvent(data.HitEntity.Value, ref ev);

        if (!ev.Reflected)
            return;

        hitscan.Comp.CurrentReflections++;

        args.Cancelled = true;

        // Arcane-Start: the same shot again, as if the target wasn't there
        if (ev.PassThrough)
        {
            var trace = data.Trace;
            trace.Ignored = trace.Ignored == null ? [data.HitEntity.Value] : [..trace.Ignored, data.HitEntity.Value];
            args.NextTrace = trace;
            args.SkipEffects = true;
            return;
        }
        // Arcane-End

        var fromEffect = Transform(data.HitEntity.Value).Coordinates;

        var hitFiredEvent = new HitscanTraceEvent
        {
            FromCoordinates = fromEffect,
            ShotDirection = ev.Direction,
            Gun = data.Gun,
            Shooter = data.HitEntity.Value,
        };

        args.NextTrace = hitFiredEvent; // Arcane-Edit: RaiseLocalEvent > fired by the raycast system after its effects
    }
}
