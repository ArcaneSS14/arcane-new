// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.StationRadio.Components;
using Content.Goobstation.Shared.StationRadio.Events;
using Content.Trauma.Common.Audio;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Shared.StationRadio.Systems;

public sealed partial class StationRadioReceiverSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaPlayedEvent>(OnMediaPlayed);
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaStoppedEvent>(OnMediaStopped);
        SubscribeLocalEvent<StationRadioReceiverComponent, ActivateInWorldEvent>(OnRadioToggle);
        SubscribeLocalEvent<StationRadioReceiverComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationRadioReceiverComponent, ComponentShutdown>(OnShutdown); // Arcane
    }

    private void OnPowerChanged(EntityUid uid, StationRadioReceiverComponent comp, PowerChangedEvent args)
    {
        SetAudible(comp, comp.Active && args.Powered);
    }

    private void OnRadioToggle(EntityUid uid, StationRadioReceiverComponent comp, ActivateInWorldEvent args)
    {
        comp.Active = !comp.Active;
        Dirty(uid, comp);
        UpdateAudible((uid, comp));
    }

    private void OnMediaPlayed(EntityUid uid, StationRadioReceiverComponent comp, ref StationRadioMediaPlayedEvent args)
    {
        // client cant predict storing a long term sound
        if (_net.IsClient || _audio.PlayPvs(args.MediaPlayed, uid, comp.DefaultParams) is not {} audio) // Arcane-Edit: Play the already selected track
            return;

        _audio.Stop(comp.SoundEntity); // Arcane: Replace the previous stream
        comp.SoundEntity = audio.Entity;
        EnsureComp<CopyrightedAudioComponent>(audio.Entity);
        EnsureComp<StationRadioPlaybackComponent>(audio.Entity); // Arcane

        UpdateAudible((uid, comp));
    }

    private void OnMediaStopped(EntityUid uid, StationRadioReceiverComponent comp, ref StationRadioMediaStoppedEvent args)
    {
        comp.SoundEntity = _audio.Stop(comp.SoundEntity);
    }

    // Arcane-Start
    private void OnShutdown(EntityUid uid, StationRadioReceiverComponent comp, ComponentShutdown args)
        => _audio.Stop(comp.SoundEntity);
    // Arcane-End

    private void UpdateAudible(Entity<StationRadioReceiverComponent> ent)
    {
        SetAudible(ent.Comp, ent.Comp.Active && _power.IsPowered(ent.Owner));
    }

    private void SetAudible(StationRadioReceiverComponent comp, bool audible)
    {
        // Arcane-Edit-Start
        if (_net.IsClient || comp.SoundEntity is not { } sound || TerminatingOrDeleted(sound))
            return;

        var volume = audible ? comp.DefaultParams.Volume : float.NegativeInfinity;
        _audio.SetVolume(sound, volume);
        if (TryComp<StationRadioPlaybackComponent>(sound, out var playback) && !playback.BaseVolume.Equals(volume))
        {
            playback.BaseVolume = volume;
            Dirty(sound, playback);
        }
        // Arcane-Edit-End
    }
}
