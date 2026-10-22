using Content.Client.Audio;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;

namespace Content.Arcane.Client.Medical.Concussion;

public sealed partial class ConcussionSystem
{
    [Dependency] private ContentAudioSystem _contentAudio = default!;
    [Dependency] private IAudioManager _audioManager = default!;

    private readonly Dictionary<EntityUid, (AudioComponent Component, float Occlusion)> _muffledAudio = [];
    private readonly HashSet<EntityUid> _interfaceAudio = [];
    private readonly Dictionary<EntityUid, AudioStream> _tinnitusStreams = [];

    // The source's filter gain is exp(-occlusion / 10); invert it for linear hearing recovery.
    private const float HearingOcclusionScale = 10f;
    private const int TinnitusFrequency = 3000;
    private const int TinnitusSampleRate = 48000;

    private EntityUid? PlayTinnitus(SoundSpecifier? sound, float volume)
    {
        var audioParams = AudioParams.Default.WithLoop(true).WithVolume(volume);
        if (sound != null)
            return _audio.PlayGlobal(sound, Filter.Local(), false, audioParams)?.Entity;

        // A whole number of periods makes the short loop seamless; only concussion controls its volume.
        var samples = new short[TinnitusSampleRate / 10];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = (short) (short.MaxValue * 0.32 * Math.Sin(Math.Tau * TinnitusFrequency * i / TinnitusSampleRate));

        var stream = _audioManager.LoadAudioRaw(samples, 1, TinnitusSampleRate, "Concussion tinnitus");
        if (_audio.PlayGlobal(stream, null, audioParams) is not { } playing)
        {
            stream.Dispose();
            return null;
        }

        _tinnitusStreams.Add(playing.Entity, stream);
        return playing.Entity;
    }

    private void InitializeHearing()
    {
        UpdatesAfter.Add(typeof(AudioSystem));
        EntityManager.EntityDeleted += OnAudioEntityDeleted;
        _contentAudio.InterfaceAudioPlayed += OnInterfaceAudio;
    }

    private void OnInterfaceAudio(EntityUid uid)
    {
        _interfaceAudio.Add(uid);
        if (_muffledAudio.Remove(uid, out var original) && original.Component.Running)
            original.Component.Occlusion = original.Occlusion;
    }

    private void OnAudioEntityDeleted(Entity<MetaDataComponent> ent)
    {
        _muffledAudio.Remove(ent.Owner);
        _interfaceAudio.Remove(ent.Owner);
        // AudioComponent shutdown has released the source before its PCM buffer is disposed.
        if (_tinnitusStreams.Remove(ent.Owner, out var stream))
            stream.Dispose();
    }

    private void UpdateHearing(float intensity, float recoveryThreshold)
    {
        if (intensity <= 0f)
            _contentAudio.ListenerOcclusion = 0f;
        else if (intensity >= recoveryThreshold)
            _contentAudio.ListenerOcclusion = float.PositiveInfinity;
        else
            _contentAudio.ListenerOcclusion = -HearingOcclusionScale * MathF.Log(1f - intensity / recoveryThreshold);

        if (_contentAudio.ListenerOcclusion == 0f)
            RestoreUnoccludedAudio();
    }

    private void UpdateUnoccludedAudio()
    {
        var hearing = _contentAudio.ListenerOcclusion;
        if (hearing == 0f)
            return;

        // Spatial streams use the existing occlusion path. Global and NoOcclusion streams bypass it.
        var query = AllEntityQuery<AudioComponent>();
        while (query.MoveNext(out var uid, out var audio))
        {
            if (uid == _sound || uid == _recoverySound || !audio.Running || !BypassesWorldOcclusion(audio))
                continue;

            if (_interfaceAudio.Contains(uid))
                continue;

            if (!_muffledAudio.TryGetValue(uid, out var original))
            {
                original = (audio, audio.Occlusion);
                _muffledAudio.Add(uid, original);
            }

            var occlusion = Math.Max(original.Occlusion, hearing);
            if (audio.Occlusion != occlusion)
                audio.Occlusion = occlusion;
        }
    }

    private static bool BypassesWorldOcclusion(AudioComponent audio)
    {
        return audio.Global || (audio.Flags & AudioFlags.NoOcclusion) != 0;
    }

    private void RestoreUnoccludedAudio()
    {
        foreach (var (audio, occlusion) in _muffledAudio.Values)
        {
            if (audio.Running && BypassesWorldOcclusion(audio))
                audio.Occlusion = occlusion;
        }
        _muffledAudio.Clear();
    }

    private void ResetHearing()
    {
        _contentAudio.ListenerOcclusion = 0f;
        RestoreUnoccludedAudio();
    }

    private void ShutdownHearing()
    {
        EntityManager.EntityDeleted -= OnAudioEntityDeleted;
        foreach (var (uid, stream) in _tinnitusStreams)
        {
            Del(uid);
            stream.Dispose();
        }
        _tinnitusStreams.Clear();

        _contentAudio.InterfaceAudioPlayed -= OnInterfaceAudio;
        _interfaceAudio.Clear();
    }
}
