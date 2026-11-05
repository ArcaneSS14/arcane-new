// Arcane-Start
namespace Content.Client.UserInterface.Motion;

/// <summary>Timing and travel in UI units for an opt-in appearance transition.</summary>
public sealed record UiMotionProfile(TimeSpan EnterDuration, TimeSpan ExitDuration, float Travel, bool Overshoot)
{
    public TimeSpan? EnterEffectDuration { get; init; }
    public TimeSpan? ExitEffectDuration { get; init; }
    public bool Fade { get; init; } = true;
    public bool Opening { get; init; } = true;
    public bool Sequence { get; init; } = true;
    public float SequenceSeconds { get; init; } = 0.12f;

    public static readonly UiMotionProfile Expressive = new(
        TimeSpan.FromSeconds(UiMotionEasing.FastSpatialSeconds),
        TimeSpan.FromSeconds(UiMotionEasing.FastSpatialSeconds), 6f, true)
    {
        EnterEffectDuration = TimeSpan.FromSeconds(UiMotionEasing.FastEffectsSeconds),
        ExitEffectDuration = TimeSpan.FromSeconds(UiMotionEasing.FastEffectsSeconds)
    };

    public static readonly UiMotionProfile Standard = new(
        TimeSpan.FromSeconds(UiMotionEasing.DefaultEffectsSeconds),
        TimeSpan.FromSeconds(UiMotionEasing.FastEffectsSeconds), 0, false);
}
// Arcane-End
