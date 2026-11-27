using Content.Client.Ghost;
using Content.Goobstation.Shared.StationRadio.Components;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.TTS;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Arcane.Client.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IResourceManager _res = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private GhostSystem _ghostSystem = default!;

    private ISawmill _sawmill = default!;
    private readonly MemoryContentRoot _contentRoot = new();
    private ResPath _prefix;

    /// <summary>
    /// Volume reduction for whispered TTS (converted to logarithmic scale)
    /// </summary>
    private const float WhisperVolumeReduction = 4f;

    private float _volume = 0.0f;
    private float _radioVolume = 0.0f;
    private bool _useTTS = false;
    private bool _ghostRadioUseTTS = false;
    private readonly HashSet<int> _mutedRadioChannels = new();
    private readonly Dictionary<int, float> _radioChannelVolumes = new();
    private readonly Dictionary<EntityUid, AudioStream> _playingStreams = new();
    private EntityUid? _previewStream;
    private ulong _fileIdx = 0;

    public override void Initialize()
    {
        EntityManager.EntityDeleted += OnAudioEntityDeleted;
        _prefix = ResPath.Root / $"TTS-{Guid.NewGuid():N}";
        _sawmill = Logger.GetSawmill("tts");
        _res.AddRoot(_prefix, _contentRoot);
        _cfg.OnValueChanged(ArcaneCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(ArcaneCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);
        _cfg.OnValueChanged(ArcaneCVars.UseTTS, OnUseTTSChanged, true);
        _cfg.OnValueChanged(ArcaneCVars.TTSRadioChannelMuted, OnTTSRadioChannelMutedChanged, true);
        _cfg.OnValueChanged(ArcaneCVars.TTSRadioChannelVolumes, OnTTSRadioChannelVolumesChanged, true);
        _cfg.OnValueChanged(ArcaneCVars.TTSGhostRadioUseTTS, OnGhostRadioUseTTSChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _contentRoot.Clear();
    }

    public override void Shutdown()
    {
        EntityManager.EntityDeleted -= OnAudioEntityDeleted;
        foreach (var (uid, stream) in _playingStreams)
        {
            Del(uid);
            stream.Dispose();
        }
        _playingStreams.Clear();
        base.Shutdown();
        _cfg.UnsubValueChanged(ArcaneCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(ArcaneCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);
        _cfg.UnsubValueChanged(ArcaneCVars.UseTTS, OnUseTTSChanged);
        _cfg.UnsubValueChanged(ArcaneCVars.TTSRadioChannelMuted, OnTTSRadioChannelMutedChanged);
        _cfg.UnsubValueChanged(ArcaneCVars.TTSRadioChannelVolumes, OnTTSRadioChannelVolumesChanged);
        _cfg.UnsubValueChanged(ArcaneCVars.TTSGhostRadioUseTTS, OnGhostRadioUseTTSChanged);
        // The resource manager keeps mounted roots; leave this one usable and empty.
        _contentRoot.Clear();
    }

    public void RequestGlobalTTS(VoiceRequestType text, string voiceId)
    {
        StopPreview();
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId));
    }

    private void StopPreview()
    {
        _audio.Stop(_previewStream);
        _previewStream = null;
    }

    private void OnAudioEntityDeleted(Entity<MetaDataComponent> entity)
    {
        // The audio component has released its source before the buffer is freed.
        if (_playingStreams.Remove(entity.Owner, out var stream))
            stream.Dispose();
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnTtsRadioVolumeChanged(float volume)
    {
        _radioVolume = volume;
    }

    private void OnUseTTSChanged(bool value)
    {
        _useTTS = value;
    }

    private void OnGhostRadioUseTTSChanged(bool value)
    {
        _ghostRadioUseTTS = value;
    }

    private void OnTTSRadioChannelMutedChanged(string value)
    {
        _mutedRadioChannels.Clear();
        _mutedRadioChannels.UnionWith(TTSRadioChannelSettings.ParseMuted(value));
    }

    private void OnTTSRadioChannelVolumesChanged(string value)
    {
        _radioChannelVolumes.Clear();
        foreach (var (frequency, volume) in TTSRadioChannelSettings.ParseVolumes(value))
            _radioChannelVolumes[frequency] = volume;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        var isRadio = ev.SourceUid == null && ev.Frequency is { };
        var isGhost = _ghostSystem.IsGhost;

        if (!_useTTS)
            return;

        if (ev.IsPreview)
            StopPreview();

        if (isRadio && isGhost && !_ghostRadioUseTTS)
            return;

        if (ev.SourceUid == null && ev.Frequency is { } frequency && _mutedRadioChannels.Contains(frequency))
            return;

        _sawmill.Verbose($"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity");

        var filePath = new ResPath($"{_fileIdx++}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, ev.Data);

        AudioStream? stream = null;
        try
        {
            var audioResource = new AudioResource();
            audioResource.Load(IoCManager.Instance!, _prefix / filePath);
            stream = audioResource.AudioStream;

            var audioParams = AudioParams.Default
                .WithVolume(AdjustVolume(ev.IsWhisper, isRadio, ev.Frequency, GetSourceVolumeOffset(ev.SourceUid)))
                .WithMaxDistance(AdjustDistance(ev.IsWhisper));

            EntityUid? playing = null;
            if (ev.IsPreview)
            {
                playing = _audio.PlayGlobal(stream, null, audioParams)?.Entity;
                _previewStream = playing;
            }
            else if (ev.SourceUid != null)
            {
                if (!TryGetEntity(ev.SourceUid.Value, out _))
                    return;
                var sourceUid = GetEntity(ev.SourceUid.Value);
                if (sourceUid.IsValid())
                    playing = _audio.PlayEntity(stream, sourceUid, null, audioParams)?.Entity;
            }
            else
            {
                playing = _audio.PlayGlobal(stream, null, audioParams)?.Entity;
            }

            if (playing is { } uid)
            {
                _playingStreams.Add(uid, stream);
                stream = null;
            }
        }
        finally
        {
            stream?.Dispose();
            _contentRoot.RemoveFile(filePath);
        }
    }

    /// <summary>
    ///     Volume offset for speech relayed by a station radio. Zero when the speaker is not one.
    /// </summary>
    private float GetSourceVolumeOffset(NetEntity? sourceUid)
    {
        if (sourceUid is not { } netUid || !TryGetEntity(netUid, out var ent))
            return 0f;

        var gain = 1f;

        if (TryComp<StationRadioReceiverComponent>(ent, out var stationRadio))
        {
            if (!stationRadio.Active)
                return float.NegativeInfinity;
            gain *= _cfg.GetCVar(ArcaneCVars.StationRadioVolume);
        }

        if (gain == 1f)
            return 0f;

        return gain <= 0.01f ? float.NegativeInfinity : SharedAudioSystem.GainToVolume(gain);
    }

    private float AdjustVolume(bool isWhisper, bool isRadio = false, int? frequency = null, float sourceOffset = 0f)
    {
        var volume = SharedAudioSystem.GainToVolume(_volume);

        if (isWhisper)
            volume -= SharedAudioSystem.GainToVolume(WhisperVolumeReduction);

        if (isRadio)
        {
            var multiplier = 1f;
            if (frequency is { } freq && _radioChannelVolumes.TryGetValue(freq, out var channelVolume))
                multiplier = channelVolume;

            volume = SharedAudioSystem.GainToVolume(_radioVolume * multiplier);
        }

        return volume + sourceOffset;
    }

    private float AdjustDistance(bool isWhisper)
    {
        return isWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange;
    }
}
