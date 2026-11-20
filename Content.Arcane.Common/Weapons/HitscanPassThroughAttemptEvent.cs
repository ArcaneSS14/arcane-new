namespace Content.Arcane.Common.Weapons;

/// <summary>
/// Raised on the entity a hitscan is about to hit, before any reflection.
/// Setting <see cref="PassThrough"/> makes the same shot carry on as if the entity wasn't there.
/// Passing through isn't a reflection, so it still works after the hitscan used up its reflections.
/// </summary>
[ByRefEvent]
public record struct HitscanPassThroughAttemptEvent(EntityUid? Shooter, bool PassThrough = false);
