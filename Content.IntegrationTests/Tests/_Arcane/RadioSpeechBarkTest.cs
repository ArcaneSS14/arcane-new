// Arcane-Start
using System.Numerics;
using Content.Arcane.Common.CCVar;
using Content.Goobstation.Common.Barks;
using Content.Goobstation.Common.CCVar;
using Content.IntegrationTests.Fixtures;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Trauma.Common.Language;
using Content.Trauma.Server.Language;
using Robust.Shared.Audio.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class RadioSpeechBarkTest : GameTest
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task RadioSpeechBarkOnlyReachesAnEnabledReceiver(bool headset)
    {
        await OverrideCVar(Side.Server, ArcaneCVars.TTSEnabled, false);
        await OverrideCVar(Side.Client, GoobCVars.BarksVolume, 1f);
        await OverrideCVar(Side.Client, ArcaneCVars.RadioBarkVolume, 0.5f);
        var map = await Pair.CreateTestMap();
        EntityUid? previous = null;
        EntityUid listener = default;
        EntityUid speaker = default;
        EntityUid receiver = default;
        try
        {
            await Client.WaitPost(() => Client.System<RadioSpeechBarkProbeSystem>().Events.Clear());
            await Server.WaitPost(() =>
            {
                previous = ServerSession!.AttachedEntity;
                listener = SSpawnAtPosition("MobHuman", map.GridCoords);
                speaker = SSpawnAtPosition("MobHuman", map.GridCoords.Offset(new Vector2(100, 100)));
                Server.PlayerMan.SetAttachedEntity(ServerSession, listener);
                SEntMan.EnsureComponent<SpeechSynthesisComponent>(speaker).VoicePrototypeId = "Borg";
                SEntMan.EnsureComponent<TelecomExemptComponent>(speaker);
                if (headset)
                {
                    receiver = SSpawnAtPosition("ClothingHeadsetGrey", map.GridCoords);
                    Assert.That(SEntMan.System<InventorySystem>().TryEquip(listener, receiver, "ears", force: true), Is.True);
                }
                else
                {
                    receiver = listener;
                    SEntMan.EnsureComponent<IntrinsicRadioReceiverComponent>(receiver);
                }
                SEntMan.EnsureComponent<ActiveRadioComponent>(receiver).ReceiveAllChannels = true;
                Server.ResolveDependency<IConfigurationManager>().SetCVar(GoobCVars.BarksEnabled, true);
            });
            await Pair.ReallyBeIdle(5);
            await Server.WaitPost(() => SEntMan.System<RadioSystem>().SendRadioMessage(speaker, "Radio voice test", SProtoMan.Index<RadioChannelPrototype>("Common"), speaker));
            await Pair.ReallyBeIdle(5);
            await RunTicksSync(2);
            await Client.WaitAssertion(() =>
            {
                var events = Client.System<RadioSpeechBarkProbeSystem>().Events;
                Assert.That(events.Count, Is.EqualTo(1));
                Assert.That(events[0].Voice, Is.EqualTo("Borg"));
                Assert.That(events[0].Message, Is.Not.Empty);
                var audio = CEntMan.EntityQueryEnumerator<AudioComponent>();
                var playedVoice = false;
                while (audio.MoveNext(out var sound))
                {
                    var fileName = sound.FileName;
                    playedVoice |= fileName.Contains("/Audio/Voice/Talk/Silicon/borg", StringComparison.OrdinalIgnoreCase);
                }
                Assert.That(playedVoice, Is.True, "The client must create audio for the radio voice.");
            });

            const string privateMessage = "A secret message in an unfamiliar language";
            await Server.WaitAssertion(() =>
            {
                var languages = SEntMan.System<LanguageSystem>();
                languages.AddLanguage(speaker, "Dwarvish");
                languages.RemoveLanguage(listener, "Dwarvish");
                Assert.That(languages.CanUnderstand(listener, "Dwarvish"), Is.False);
                SEntMan.System<RadioSystem>().SendRadioMessage(speaker, privateMessage, SProtoMan.Index<RadioChannelPrototype>("Common"), speaker, language: SProtoMan.Index<LanguagePrototype>("Dwarvish"));
            });
            await Pair.ReallyBeIdle(5);
            await Client.WaitAssertion(() =>
            {
                var events = Client.System<RadioSpeechBarkProbeSystem>().Events;
                Assert.That(events.Count, Is.EqualTo(2));
                Assert.That(events[1].Message, Is.Not.EqualTo(privateMessage), "Unfamiliar speech must be obfuscated before it reaches the client.");
            });

            await Server.WaitPost(() =>
            {
                SEntMan.GetComponent<ActiveRadioComponent>(receiver).ReceiveAllChannels = false;
                SEntMan.GetComponent<ActiveRadioComponent>(receiver).Channels.Clear();
                SEntMan.System<RadioSystem>().SendRadioMessage(speaker, "No listener", SProtoMan.Index<RadioChannelPrototype>("Common"), speaker);
                SEntMan.GetComponent<ActiveRadioComponent>(receiver).ReceiveAllChannels = true;
                Server.ResolveDependency<IConfigurationManager>().SetCVar(GoobCVars.BarksEnabled, false);
                SEntMan.System<RadioSystem>().SendRadioMessage(speaker, "Barks disabled", SProtoMan.Index<RadioChannelPrototype>("Common"), speaker);
                Server.ResolveDependency<IConfigurationManager>().SetCVar(GoobCVars.BarksEnabled, true);
                SEntMan.System<RadioSystem>().SendRadioMessage(listener, "Own message", SProtoMan.Index<RadioChannelPrototype>("Common"), speaker);
            });
            await Pair.ReallyBeIdle(5);
            await Client.WaitAssertion(() => Assert.That(Client.System<RadioSpeechBarkProbeSystem>().Events.Count, Is.EqualTo(2)));
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                Server.ResolveDependency<IConfigurationManager>().SetCVar(GoobCVars.BarksEnabled, true);
                Server.PlayerMan.SetAttachedEntity(ServerSession!, previous);
            });
        }
    }
}

public sealed partial class RadioSpeechBarkProbeSystem : EntitySystem
{
    public readonly List<PlayRadioSpeechBarkEvent> Events = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<PlayRadioSpeechBarkEvent>(OnBark);
    }

    private void OnBark(PlayRadioSpeechBarkEvent ev) => Events.Add(ev);
}
// Arcane-End
