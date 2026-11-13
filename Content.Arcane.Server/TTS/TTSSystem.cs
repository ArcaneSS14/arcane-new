using System.Threading;
using System.Threading.Tasks;
using Content.Trauma.Common.Language.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Arcane.Common.Radio;
using Content.Server.Communications;
using Content.Shared.Station.Components;
using Content.Shared.Station.Systems;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Components;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.TTS;
using Content.Shared.Chat;
using Content.Shared.Humanoid;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private TTSManager _ttsManager = default!;
    [Dependency] private SharedTransformSystem _xforms = default!;
    [Dependency] private CommonLanguageSystem _language = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private ExamineSystemShared _examineSystem = default!;

    private const int MaxMessageChars = 300;
    private bool _isEnabled;


    public override void Initialize()
    {
        _cfg.OnValueChanged(ArcaneCVars.TTSEnabled, v => _isEnabled = v, true);

        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke, after: [typeof(RadioSystem), typeof(HeadsetSystem)]);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ =>
        {
            _ttsManager.ResetCache();
            _pendingVoiceChange.Clear();
        });
        SubscribeLocalEvent<ActorComponent, TTSRadioPlayEvent>(OnTTSRadioPlayEvent);
        SubscribeLocalEvent<ActorComponent, IntrinsicRadioReceivedEvent>(OnIntrinsicRadioReceived);
        SubscribeLocalEvent<TTSAnnouncePlayEvent>(OnTTSAnnouncePlayEvent);
        SubscribeLocalEvent<CommunicationConsoleAnnouncementEvent>(OnConsoleAnnouncement);
        SubscribeLocalEvent<TTSComponent, HumanoidProfileAppliedEvent>(OnProfileApplied);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);

        SubscribeLocalEvent<TTSVoiceChangeOpenMenuEvent>(OnVoiceChangeMenu);
        SubscribeNetworkEvent<TTSVoiceChangeSelectedMessage>(OnVoiceChangeSelected);
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled || args.Message.Length > MaxMessageChars)
            return;

        if (!args.Language.SpeechOverride.RequireSpeech)
            return;

        var voiceId = component.VoicePrototype;
        var effect = component.Effect;

        if (!_prototypeManager.TryIndex(voiceId, out var protoVoice))
            return;

        if (args.IsWhisper)
        {
            HandleWhisper(uid, args.Message, args.Language, protoVoice.Speaker, effect);
            return;
        }

        HandleSay(uid, args.Message, args.Language, protoVoice.Speaker, effect);
    }

    /// <summary>
    /// Reads a radio message to its receiver in the sender's voice. The sender does not hear their own message.
    /// </summary>
    public void PlayRadio(EntityUid receiver, EntityUid source, string message, LanguagePrototype language, int frequency)
    {
        if (receiver == source
            || !TryComp<TTSComponent>(source, out var tts)
            || tts.VoicePrototype is not { } voiceId
            || !_prototypeManager.TryIndex(voiceId, out var voice)
            || !_language.CanUnderstand(receiver, language.ID))
        {
            return;
        }

        var ev = new TTSRadioPlayEvent(message, language, voice.Speaker, frequency);
        RaiseLocalEvent(receiver, ref ev);
    }

    private void OnIntrinsicRadioReceived(Entity<ActorComponent> ent, ref IntrinsicRadioReceivedEvent args)
    {
        if (_prototypeManager.TryIndex<LanguagePrototype>(args.Language, out var language))
            PlayRadio(ent, args.MessageSource, args.Message, language, args.Frequency);
    }

    private void OnTTSRadioPlayEvent(EntityUid uid, ActorComponent comp, ref TTSRadioPlayEvent args)
    {
        if (!_isEnabled || args.Message.Length > MaxMessageChars)
            return;

        HandleReceiveRadio(Filter.SinglePlayer(comp.PlayerSession), args.Message, args.Voice, TTSEffects.RadioHeadset, args.Language, args.Frequency);
    }

    private void OnProfileApplied(Entity<TTSComponent> ent, ref HumanoidProfileAppliedEvent args)
    {
        ent.Comp.VoicePrototype = args.Profile.TtsVoice;
    }

    /// <summary>
    /// Reads a communications console announcement to everyone on the console's station, in the author's voice.
    /// </summary>
    private void OnConsoleAnnouncement(ref CommunicationConsoleAnnouncementEvent args)
    {
        if (_station.GetOwningStation(args.Uid) is not { } station
            || !TryComp<StationDataComponent>(station, out var data))
        {
            return;
        }

        var ev = new TTSAnnouncePlayEvent(args.Text, args.Sender, _station.GetInStation(data));
        RaiseLocalEvent(ref ev);
    }

    private void OnTTSAnnouncePlayEvent(ref TTSAnnouncePlayEvent args)
    {
        string? voice = null;
        if (TryComp<TTSComponent>(args.Sender, out var ttsComponent)
            && ttsComponent.VoicePrototype is { } voiceId
            && _prototypeManager.TryIndex(voiceId, out var voicePrototype))
        {
            voice = voicePrototype.Speaker;
        }

        if (voice != null)
        {
            var receivers = args.Receievers;
            var message = args.Message;
            Robust.Shared.Timing.Timer.Spawn(TimeSpan.FromSeconds(2), () => HandleReceiveRadio(receivers, message, voice, TTSEffects.Announce));
        }
    }

    private async void HandleReceiveRadio(Filter filter, string message, string speaker, string effect, LanguagePrototype? language = null, int? frequency = null)
    {
        var soundData = await GenerateTTS(message, speaker, effect);
        if (soundData is null)
            return;

        foreach (var recipient in filter.Recipients)
        {
            var uid = recipient.AttachedEntity;
            if (uid == null)
                continue;

            if (language != null && !_language.CanUnderstand(uid.Value, language.ID))
                continue;

            RaiseNetworkEvent(new PlayTTSEvent(soundData, null, false, frequency), recipient);
        }
    }

    private async void HandleSay(EntityUid uid, string message, LanguagePrototype language, string speaker, string? effect)
    {
        var normal = await GenerateTTS(message, speaker, effect);
        if (normal is null)
            return;

        // var obfuscated = await GenerateTTS(_language.ObfuscateSpeech(message, language), speaker);
        // if (obfuscated is null)
        //     return;

        var nilter = Filter.Empty();
        var lilter = Filter.Empty();
        foreach (var session in Filter.Pvs(uid).Recipients)
        {
            if (!session.AttachedEntity.HasValue)
                continue;

            if (!message.EndsWith("!") && !InLocalChatRangeUnOccluded(uid, session.AttachedEntity.Value, ChatSystem.VoiceRange))
                continue;

            TryComp(session.AttachedEntity.Value, out LanguageSpeakerComponent? lang);
            if (_language.CanUnderstand(new(session.AttachedEntity.Value, lang), language.ID))
                nilter.AddPlayer(session);
            else
                lilter.AddPlayer(session);
        }

        RaiseNetworkEvent(new PlayTTSEvent(normal, GetNetEntity(uid)), nilter);
        // RaiseNetworkEvent(new PlayTTSEvent(obfuscated, GetNetEntity(uid)), lilter, false);
    }

    private async void HandleWhisper(EntityUid uid, string message, LanguagePrototype language, string speaker, string? effect = null)
    {
        var normal = await GenerateTTS(message, speaker, effect);
        if (normal is null)
            return;

        // var obfuscated = await GenerateTTS(_language.ObfuscateSpeech(message, language), speaker);
        // if (obfuscated is null)
        //     return;

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var nilter = Filter.Empty();
        var lilter = Filter.Empty();
        foreach (var session in Filter.Pvs(uid).Recipients)
        {
            if (!session.AttachedEntity.HasValue)
                continue;

            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();
            if (distance > ChatSystem.WhisperMuffledRange)
                continue;

            if (!InLocalChatRangeUnOccluded(uid, session.AttachedEntity.Value, ChatSystem.WhisperMuffledRange))
                continue;

            TryComp(session.AttachedEntity.Value, out LanguageSpeakerComponent? lang);
            if (_language.CanUnderstand(new(session.AttachedEntity.Value, lang), language.ID)
                && distance <= ChatSystem.WhisperClearRange)
                nilter.AddPlayer(session);
            else
                lilter.AddPlayer(session);
        }

        RaiseNetworkEvent(new PlayTTSEvent(normal, GetNetEntity(uid), true), nilter);
        // RaiseNetworkEvent(new PlayTTSEvent(obfuscated, GetNetEntity(uid), true), lilter, false);
    }

    private bool InLocalChatRangeUnOccluded(EntityUid source, EntityUid listener, float range)
    {
        return HasComp<GhostHearingComponent>(listener)
            || _examineSystem.InRangeUnOccluded(source, listener, range);
    }

    private readonly Dictionary<string, Task<byte[]?>> _ttsTasks = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    private async Task<byte[]?> GenerateTTS(string text, string speaker, string? effect = null)
    {
        var textSanitized = Sanitize(text);
        if (string.IsNullOrEmpty(textSanitized))
            return null;

        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        var taskKey = $"{textSanitized}_{speaker}_{effect}";

        await _lock.WaitAsync();
        Task<byte[]?> task;
        try
        {
            if (_ttsTasks.TryGetValue(taskKey, out var existing))
                return await existing;

            task = _ttsManager.ConvertTextToSpeech(speaker, textSanitized, effect);
            _ttsTasks[taskKey] = task;
        }
        finally
        {
            _lock.Release();
        }

        try
        {
            return await task;
        }
        finally
        {
            await _lock.WaitAsync();
            try { _ttsTasks.Remove(taskKey); }
            finally { _lock.Release(); }
        }
    }
}
