using Content.Arcane.Common.Radio;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Radio;

/// <summary>
/// Sends the channel's bark to players whose headset received a radio message (Orion #452, #456).
/// </summary>
public sealed partial class RadioBarkSystem : EntitySystem
{
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
    }
}
