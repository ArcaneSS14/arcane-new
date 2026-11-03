// Arcane-Start
using Robust.Shared.Configuration;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Motion;

/// <summary>One transition, retargeted from its current values without completion callbacks.</summary>
internal sealed class UiMotionTransition(IConfigurationManager configuration)
{
    private float _elapsed;
    private float _fromOpacity;
    private float _fromOffset;
    private bool _entering = true;
    private UiMotionProfile _profile = UiMotionProfile.Standard;

    public float Opacity { get; private set; } = 1f;
    public float Offset { get; private set; }
    public bool Finished { get; private set; } = true;

    public void Finish()
    {
        Opacity = _entering ? 1 : 0;
        Offset = 0;
        Finished = true;
    }

    public void Start(UiMotionProfile profile, bool entering, float opacity, float offset)
    {
        _profile = UiMotionSettings.Resolve(configuration, profile);
        _entering = entering;
        _fromOpacity = opacity;
        _fromOffset = configuration.GetCVar(UiMotionSettings.Reduce) ? 0 : offset;
        _elapsed = 0;
        Finished = false;
        Advance(0);
    }

    public void Advance(float deltaSeconds)
    {
        if (Finished)
            return;

        _profile = UiMotionSettings.Resolve(configuration, _profile);
        if (configuration.GetCVar(UiMotionSettings.Reduce))
            _fromOffset = 0;
        var duration = (float) (_entering ? _profile.EnterDuration : _profile.ExitDuration).TotalSeconds;
        _elapsed += Math.Max(0, deltaSeconds) * UiMotionSettings.PlaybackSpeed(configuration);
        var progress = !configuration.GetCVar(UiMotionSettings.Enabled)
                       || (_entering && !_profile.Opening)
                       || duration <= 0
            ? 1f
            : Math.Clamp(_elapsed / duration, 0f, 1f);
        var spatial = _entering && _profile.Overshoot ? Easings.OutBack(progress) : Easings.OutCubic(progress);
        var effects = Easings.OutCubic(progress);
        Opacity = _profile.Fade ? Math.Clamp(_fromOpacity + ((_entering ? 1f : 0f) - _fromOpacity) * effects, 0f, 1f) : 1f;
        Offset = _fromOffset + ((_entering ? 0f : -_profile.Travel) - _fromOffset) * spatial;
        Finished = progress >= 1;
    }

    public float GetStaggeredOpacity(int index, int count)
    {
        if (Finished || !_entering || !_profile.Sequence || configuration.GetCVar(UiMotionSettings.Reduce) || count < 4)
            return 1f;

        // Reserve the first half for the sequence and finish every item within the host's transition.
        var duration = (float) _profile.EnterDuration.TotalSeconds;
        var sequence = Math.Min(_profile.SequenceSeconds, duration * 0.45f);
        var delay = sequence * Math.Clamp(index / (float) (count - 1), 0f, 1f);
        var progress = Math.Clamp((_elapsed - delay) / (duration - sequence), 0f, 1f);
        return _fromOpacity + (1f - _fromOpacity) * Easings.OutCubic(progress);
    }
}
// Arcane-End
