// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Common.CCVar;
using Content.Goobstation.Shared.StationRadio.Components;
using Robust.Client.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;

namespace Content.Arcane.Client.Radio;

/// <summary>Personal radio volume as in old Arcane #415 by ReWAFFlution, applied to the existing networked audio.</summary>
public sealed partial class StationRadioVolumeSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IConfigurationManager _config = default!;

    private float _volumeOffset;
    private readonly HashSet<EntityUid> _pending = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<StationRadioPlaybackComponent, ComponentStartup>(OnPlaybackStarted);
        SubscribeLocalEvent<StationRadioPlaybackComponent, AfterAutoHandleStateEvent>(OnPlaybackState);
        Subs.CVar(_config, ArcaneCVars.StationRadioVolume, OnVolumeChanged, true);
    }

    private void OnPlaybackStarted(Entity<StationRadioPlaybackComponent> ent, ref ComponentStartup args)
        => _pending.Add(ent);

    private void OnPlaybackState(Entity<StationRadioPlaybackComponent> ent, ref AfterAutoHandleStateEvent args)
        => _pending.Add(ent);

    public override void FrameUpdate(float frameTime)
    {
        // Apply after both playback and the engine audio state have been received.
        foreach (var uid in _pending)
        {
            if (TryComp<StationRadioPlaybackComponent>(uid, out var playback))
                SetVolume((uid, playback));
        }
        _pending.Clear();
    }

    private void OnVolumeChanged(float gain)
    {
        _volumeOffset = gain > 0f && float.IsFinite(gain)
            ? SharedAudioSystem.GainToVolume(Math.Min(gain, 1f))
            : float.NegativeInfinity;
        var query = AllEntityQuery<StationRadioPlaybackComponent>();
        while (query.MoveNext(out var uid, out var playback))
            SetVolume((uid, playback));
    }

    private void SetVolume(Entity<StationRadioPlaybackComponent> ent)
    {
        if (TryComp<AudioComponent>(ent, out var audio))
            _audio.SetVolume(ent, ent.Comp.BaseVolume + _volumeOffset, audio);
    }
}
