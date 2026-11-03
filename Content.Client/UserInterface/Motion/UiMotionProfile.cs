// Arcane-Start
namespace Content.Client.UserInterface.Motion;

/// <summary>Timing and travel in UI units for an opt-in appearance transition.</summary>
public sealed record UiMotionProfile(TimeSpan EnterDuration, TimeSpan ExitDuration, float Travel, bool Overshoot)
{
    public bool Fade { get; init; } = true;
    public bool Opening { get; init; } = true;
    public bool Sequence { get; init; } = true;
    public float SequenceSeconds { get; init; } = 0.12f;

    public static readonly UiMotionProfile Expressive = new(
        TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(100), 6f, true);

    public static readonly UiMotionProfile Standard = new(
        TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(100), 3f, false);
}
// Arcane-End
