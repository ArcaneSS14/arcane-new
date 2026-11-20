using Content.Arcane.Common.Radio;
using Content.Arcane.Server.TTS;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Radio;

/// <summary>
/// Sends the channel's bark to players whose headset received a radio message (Orion #452, #456),
/// and has TTS read the message in the sender's voice.
/// </summary>
public sealed partial class RadioBarkSystem : EntitySystem
{
    [Dependency] private TTSSystem _tts = default!;

    private static readonly SoundPathSpecifier DefaultBark = new("/Audio/_Arcane/Radio/basic.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActorComponent, HeadsetRadioReceiveRelayEvent>(OnHeadsetReceive);
    }

    private void OnHeadsetReceive(Entity<ActorComponent> ent, ref HeadsetRadioReceiveRelayEvent args)
    {
        var sound = args.RelayedEvent.Channel.OnSendSound ?? DefaultBark;
        RaiseNetworkEvent(new RadioBarkEvent(sound.Path, sound.Params), ent.Comp.PlayerSession.Channel);

        var ev = args.RelayedEvent;
        _tts.PlayRadio(ent, ev.MessageSource, ev.OriginalChatMsg.Message, ev.Language, ev.Channel.Frequency.Int());
    }
}
