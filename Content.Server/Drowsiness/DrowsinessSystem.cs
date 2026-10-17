using Content.Shared.Bed.Sleep;
using Content.Shared.Drowsiness;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Drowsiness;

public sealed partial class DrowsinessSystem : SharedDrowsinessSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DrowsinessStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
    }

    private void OnEffectApplied(Entity<DrowsinessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        // Arcane-Start: Visual-only effects do not schedule sleeping incidents.
        if (ent.Comp.TimeBetweenIncidents is not { } timeBetweenIncidents)
            return;
        // Arcane-End

        ent.Comp.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(timeBetweenIncidents.X, timeBetweenIncidents.Y)); // Arcane-Edit: Use the configured interval.
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DrowsinessStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var drowsiness, out var statusEffect))
        {
            // Arcane-Start: Visual-only effects do not cause forced sleeping.
            if (drowsiness.TimeBetweenIncidents is not { } timeBetweenIncidents)
                continue;
            // Arcane-End

            if (_timing.CurTime < drowsiness.NextIncidentTime)
                continue;

            if (statusEffect.AppliedTo is null)
                continue;

            // Set the new time.
            drowsiness.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(timeBetweenIncidents.X, timeBetweenIncidents.Y)); // Arcane-Edit: Use the configured interval.

            // sleep duration
            var duration = TimeSpan.FromSeconds(_random.NextFloat(drowsiness.DurationOfIncident.X, drowsiness.DurationOfIncident.Y));

            // Make sure the sleep time doesn't cut into the time to next incident.
            drowsiness.NextIncidentTime += duration;

            _statusEffects.TryAddStatusEffectDuration(statusEffect.AppliedTo.Value, SleepingSystem.StatusEffectForcedSleeping, duration);
        }
    }
}
