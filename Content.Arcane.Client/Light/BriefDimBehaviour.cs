using Content.Client.Light.Components;
using JetBrains.Annotations;

namespace Content.Arcane.Client.Light;

/// <summary>
/// Holds a bright light for most of the cycle, with a brief dim interval before the next cycle.
/// </summary>
[UsedImplicitly]
public sealed partial class BriefDimBehaviour : LightBehaviourAnimationTrack
{
    [DataField]
    public float DimDuration { get; private set; }

    public override void OnStart()
    {
        ApplyProperty(StartValue);
    }

    public override (int KeyFrameIndex, float FramePlayingTime) AdvancePlayback(
        object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
    {
        var playingTime = prevPlayingTime + frameTime;
        // The animation player resets the frame index when it starts the next cycle.
        if (prevKeyFrameIndex < 0 && playingTime >= MaxTime - Math.Clamp(DimDuration, 0f, MaxTime))
        {
            ApplyProperty(EndValue);
            return (0, playingTime);
        }

        return (prevKeyFrameIndex, playingTime);
    }
}
