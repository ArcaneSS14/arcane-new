using Robust.Shared.Audio.Systems;

namespace Content.Shared.Audio.Jukebox;

public abstract partial class SharedJukeboxSystem : EntitySystem
{
    [Dependency] protected SharedAudioSystem Audio = default!;

    /// <summary>
    /// Returns whether or not the given jukebox is currently playing a song.
    /// </summary>
    public bool IsPlaying(Entity<JukeboxComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        return entity.Comp.AudioStream is { } audio && Audio.IsPlaying(audio);
    }

    // Arcane-Start
    /// <summary>
    /// Converts a volume slider value to the audio volume in decibels.
    /// </summary>
    public static float SliderToVolume(float slider)
    {
        var value = Math.Clamp(slider, JukeboxComponent.MinSlider, JukeboxComponent.MaxSlider);
        return JukeboxComponent.MinVolume + (value - JukeboxComponent.MinSlider)
            * (JukeboxComponent.MaxVolume - JukeboxComponent.MinVolume)
            / (JukeboxComponent.MaxSlider - JukeboxComponent.MinSlider);
    }
    // Arcane-End
}
