// Arcane-Start
namespace Content.Client.UserInterface.Motion;

/// <summary>Bounded delays shared by window content and the settings preview.</summary>
public static class UiMotionSequence
{
    public static float Delay(int index, int mode, float interval, float jitter, float maximum, float random)
    {
        random = Math.Clamp(random, 0, 1);
        var ordered = Math.Max(0, index) * interval;
        return Math.Clamp(mode switch
        {
            0 => 0,
            1 => ordered,
            3 => random * maximum,
            2 => ordered + (2 * random - 1) * Math.Min(jitter, interval * 0.45f),
            _ => 0
        }, 0, maximum);
    }
}
// Arcane-End
