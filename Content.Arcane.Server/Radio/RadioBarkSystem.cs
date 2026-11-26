using Content.Arcane.Common.Radio;
using Content.Arcane.Server.TTS;
using Content.Goobstation.Common.Barks;
using Content.Goobstation.Common.CCVar;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Trauma.Common.Language.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Radio;

/// <summary>
/// Sends the channel's bark to players whose headset received a radio message (Orion #452, #456),
/// and has TTS read the message in the sender's voice.
/// </summary>
public sealed partial class RadioBarkSystem : EntitySystem
{
    [Dependency] private TTSSystem _tts = default!;
    [Dependency] private CommonLanguageSystem _language = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private static readonly SoundPathSpecifier DefaultBark = new("/Audio/_Arcane/Radio/basic.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActorComponent, HeadsetRadioReceiveRelayEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, IntrinsicRadioReceivedEvent>(OnIntrinsicReceive);
    }

    private void OnHeadsetReceive(Entity<ActorComponent> ent, ref HeadsetRadioReceiveRelayEvent args)
    {
        var sound = args.RelayedEvent.Channel.OnSendSound ?? DefaultBark;
        RaiseNetworkEvent(new RadioBarkEvent(sound.Path, sound.Params), ent.Comp.PlayerSession.Channel);

        var ev = args.RelayedEvent;
        _tts.PlayRadio(ent, ev.MessageSource, ev.OriginalChatMsg.Message, ev.Language, ev.Channel.Frequency.Int());
        var audibleMessage = _language.CanUnderstand(ent.Owner, ev.Language.ID)
            ? ev.OriginalChatMsg.Message
            : ev.LanguageObfuscatedChatMsg.Message;
        if (ev.Language.SpeechOverride.RequireSpeech)
            SendSpeechBark(ent, ev.MessageSource, audibleMessage);
    }

    private void OnIntrinsicReceive(Entity<IntrinsicRadioReceiverComponent> ent, ref IntrinsicRadioReceivedEvent args)
    {
        if (args.RequiresSpeech && TryComp<ActorComponent>(ent.Owner, out var actor))
            SendSpeechBark((ent.Owner, actor), args.MessageSource, args.AudibleMessage);
    }

    private void SendSpeechBark(Entity<ActorComponent> listener, EntityUid speaker, string message)
    {
        if (!_cfg.GetCVar(GoobCVars.BarksEnabled) || listener.Owner == speaker || string.IsNullOrEmpty(message)
            || !TryComp<SpeechSynthesisComponent>(speaker, out var speech)
            || speech.VoicePrototypeId is not { } voice)
            return;

        RaiseNetworkEvent(new PlayRadioSpeechBarkEvent(voice.ToString(), message), listener.Comp.PlayerSession.Channel);
    }
}
