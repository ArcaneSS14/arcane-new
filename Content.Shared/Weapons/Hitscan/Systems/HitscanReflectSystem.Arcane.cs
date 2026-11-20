// Arcane-Start
using Content.Arcane.Common.Weapons;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed partial class HitscanReflectSystem
{
    /// <summary>
    /// Repeats the shot without the hit entity if it lets the shot through, returning true if it did.
    /// </summary>
    private bool TryPassThrough(ref AttemptHitscanRaycastFiredEvent args)
    {
        var data = args.Data;
        if (data.HitEntity is not { } hit)
            return false;

        var ev = new HitscanPassThroughAttemptEvent(data.Shooter);
        RaiseLocalEvent(hit, ref ev);
        if (!ev.PassThrough)
            return false;

        args.Cancelled = true;
        var trace = data.Trace;
        if (trace.Ignored?.Count >= MaxPassThroughs)
            return true;

        trace.Ignored = trace.Ignored == null ? [hit] : [.. trace.Ignored, hit];
        args.NextTrace = trace;
        args.SkipEffects = true;
        return true;
    }
}
// Arcane-End
