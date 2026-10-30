using Content.Arcane.Common.CCVar;
using Content.Arcane.Common.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Arcane.Client.Radio;

public sealed partial class RadioBarkSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    /// <summary>
    /// The options slider goes up to this gain.
    /// </summary>
    public const float VolumeMultiplier = 2f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RadioBarkEvent>(OnRadioBark);
    }

    private void OnRadioBark(RadioBarkEvent ev)
    {
        var gain = _cfg.GetCVar(ArcaneCVars.RadioBarkVolume) * VolumeMultiplier;
        if (gain <= 0f)
            return;

        var audioParams = ev.Params.WithVolume(ev.Params.Volume + SharedAudioSystem.GainToVolume(gain));
        _audio.PlayGlobal(new SoundPathSpecifier(ev.Path), Filter.Local(), false, audioParams);
    }
}
