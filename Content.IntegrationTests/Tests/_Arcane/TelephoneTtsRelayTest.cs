// Arcane-Start
using System.IO;
using System.Net;
using System.Net.Sockets;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Server.TTS;
using Content.Arcane.Shared.TTS;
using Content.IntegrationTests.Fixtures;
using Content.Server._Arcane.Telephone;
using Content.Server.Telephone;
using Content.Shared.Chat;
using Content.Shared.Telephone;
using Content.Shared.Speech;
using Content.Shared.TTS;
using Content.Trauma.Common.Language;
using Robust.Shared.Prototypes;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class TelephoneTtsRelayTest : GameTest
{
    private static readonly ProtoId<LanguagePrototype> TauCetiBasic = "TauCetiBasic";

    [Test]
    public async Task HologramUsesOriginalVoiceAndRestoresItsOwnVoice()
    {
        var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint) reservation.LocalEndpoint).Port;
        reservation.Stop();
        using var provider = new HttpListener();
        provider.Prefixes.Add($"http://127.0.0.1:{port}/");
        provider.Start();

        await OverrideCVar(Side.Server, ArcaneCVars.TTSEnabled, true);
        await OverrideCVar(Side.Server, ArcaneCVars.TTSApiUrl, $"http://127.0.0.1:{port}/");
        await OverrideCVar(Side.Server, ArcaneCVars.TTSApiToken, "local-tts-test-token");
        var manager = Server.ResolveDependency<TTSManager>();
        await Server.WaitPost(manager.ResetCache);
        await OverrideCVar(Side.Client, ArcaneCVars.UseTTS, true);
        var map = await Pair.CreateTestMap();
        await Client.WaitPost(() => Client.System<TelephoneTtsProbeSystem>().Events.Clear());
        byte[] audio = default!;
        await Server.WaitPost(() =>
        {
            using var stream = Server.ResolveDependency<IResourceManager>().ContentFileRead(new ResPath("/Audio/_Arcane/Radio/basic.ogg"));
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            audio = copy.ToArray();
        });
        EntityUid sender = default;
        EntityUid source = default;
        EntityUid receiver = default;
        EntityUid hologram = default;
        EntityUid? previous = null;
        NetEntity hologramNet = default;
        var received = 0;
        try
        {
            await Server.WaitPost(() =>
            {
                previous = ServerSession!.AttachedEntity;
                var listener = SSpawnAtPosition("MobHuman", map.GridCoords);
                Server.PlayerMan.SetAttachedEntity(ServerSession, listener);
                sender = SSpawnAtPosition("MobHuman", map.GridCoords);
                SEntMan.EnsureComponent<TTSComponent>(sender).VoicePrototype = "Glados";
                source = SSpawnAtPosition(null, map.GridCoords);
                receiver = SSpawnAtPosition(null, map.GridCoords);
                hologram = SSpawnAtPosition("HolopadHologram", map.GridCoords);
                hologramNet = SEntMan.GetNetEntity(hologram);
                var sourcePhone = SEntMan.AddComponent<TelephoneComponent>(source);
                var receiverPhone = SEntMan.AddComponent<TelephoneComponent>(receiver);
                var telephones = SEntMan.System<TelephoneSystem>();
                telephones.CallTelephone((source, sourcePhone), (receiver, receiverPhone), sender, new TelephoneCallOptions
                {
                    IgnoreRange = true,
                    ForceConnect = true,
                });
                Assert.That(telephones.IsSourceConnectedToReceiver((source, sourcePhone), (receiver, receiverPhone)), Is.True);
                telephones.SetSpeakerForTelephone((receiver, receiverPhone), (hologram, SEntMan.GetComponent<SpeechComponent>(hologram)));
                var speakerVoice = SEntMan.EnsureComponent<TTSComponent>(hologram);
                speakerVoice.VoicePrototype = "Gman";
                speakerVoice.Effect = TTSEffects.Intercom;
            });

            await Pair.RunUntilSynced();

            async Task SendAndExpect(string text, ProtoId<TTSVoicePrototype> voice, string effect)
            {
                await Server.WaitAssertion(() =>
                {
                    var sourcePhone = SEntMan.GetComponent<TelephoneComponent>(source);
                    var language = SProtoMan.Index(TauCetiBasic);
                    var ev = new TelephoneMessageReceivedEvent(text, new MsgChatMessage(), sender, (source, sourcePhone), language);
                    SEntMan.EventBus.RaiseLocalEvent(receiver, ref ev);
                    Assert.That(SEntMan.HasComponent<TelephoneRelaySpeechComponent>(hologram), Is.False);
                    var speakerVoice = SEntMan.GetComponent<TTSComponent>(hologram);
                    Assert.That(speakerVoice.VoicePrototype, Is.EqualTo((ProtoId<TTSVoicePrototype>) "Gman"));
                    Assert.That(speakerVoice.Effect, Is.EqualTo(TTSEffects.Intercom));
                });
                var request = await provider.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.That(request.Request.QueryString["speaker"], Is.EqualTo(SProtoMan.Index(voice).Speaker));
                Assert.That(request.Request.QueryString["effect"], Is.EqualTo(effect));
                request.Response.StatusCode = (int) HttpStatusCode.OK;
                request.Response.ContentType = "audio/ogg";
                request.Response.ContentLength64 = audio.Length;
                await request.Response.OutputStream.WriteAsync(audio);
                request.Response.Close();
                await Pair.ReallyBeIdle(5);
                received++;
                await Client.WaitAssertion(() =>
                {
                    var events = Client.System<TelephoneTtsProbeSystem>().Events;
                    Assert.That(events.Count, Is.EqualTo(received));
                    Assert.That(events[^1].SourceUid, Is.EqualTo(hologramNet));
                    Assert.That(events[^1].Data, Is.EqualTo(audio));
                });
            }

            await SendAndExpect("A hologram test", "Glados", "robotic");
            await Server.WaitPost(() => SEntMan.RemoveComponent<TTSComponent>(sender));
            await SendAndExpect("Fallback voice test", "Gman", TTSEffects.Intercom);
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                Server.PlayerMan.SetAttachedEntity(ServerSession!, previous);
                manager.ResetCache();
            });
        }
    }
}

public sealed partial class TelephoneTtsProbeSystem : EntitySystem
{
    public readonly List<PlayTTSEvent> Events = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<PlayTTSEvent>(OnTts);
    }

    private void OnTts(PlayTTSEvent ev) => Events.Add(ev);
}
// Arcane-End
