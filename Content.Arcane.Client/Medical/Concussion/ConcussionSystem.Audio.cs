using Content.Client.Audio;
using Robust.Client.Audio;
using Robust.Shared.Audio.Components;

namespace Content.Arcane.Client.Medical.Concussion;

public sealed partial class ConcussionSystem
{
    [Dependency] private ContentAudioSystem _contentAudio = default!;

    private readonly Dictionary<EntityUid, (AudioComponent Component, float Occlusion)> _muffledAudio = [];
    private readonly HashSet<EntityUid> _interfaceAudio = [];

    // The source's filter gain is exp(-occlusion / 10); invert it for linear hearing recovery.
    private const float HearingOcclusionScale = 10f;

    private void InitializeHearing()
    {
        UpdatesAfter.Add(typeof(AudioSystem));
        SubscribeLocalEvent<AudioComponent, ComponentShutdown>(OnAudioShutdown);
        _contentAudio.InterfaceAudioPlayed += OnInterfaceAudio;
    }

    private void OnInterfaceAudio(EntityUid uid)
    {
        _interfaceAudio.Add(uid);
        if (_muffledAudio.Remove(uid, out var original) && original.Component.Running)
            original.Component.Occlusion = original.Occlusion;
    }

    private void OnAudioShutdown(Entity<AudioComponent> ent, ref ComponentShutdown args)
    {
        _muffledAudio.Remove(ent.Owner);
        _interfaceAudio.Remove(ent.Owner);
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
        _contentAudio.InterfaceAudioPlayed -= OnInterfaceAudio;
        _interfaceAudio.Clear();
    }
}
