using System.Net;
using System.Net.Sockets;
using Content.Arcane.Client.TTS;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Server.TTS;
using Content.Arcane.Shared.TTS;
using Content.Shared.TTS;
using Robust.Client.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Serilog.Events;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class TtsRuntimeTest : GameTest
{
    private static readonly EntProtoId Speaker = "MobHuman";
    private static readonly ProtoId<TTSVoicePrototype> OriginalVoice = "Gman";
    private static readonly ProtoId<TTSVoicePrototype> SelectedVoice = "Glados";

    [Test]
    [EnsureCVar(Side.Server, typeof(ArcaneCVars), nameof(ArcaneCVars.TTSEnabled), false)]
    public async Task AnnouncementsRespectServerSwitchAndUnavailableProvider()
    {
        await OverrideCVar(Side.Server, ArcaneCVars.TTSApiUrl, "");
        var map = await Pair.CreateTestMap();
        var missingEndpointLogs = 0;
        bool JudgeMissingEndpoint(string sawmill, LogEvent message)
        {
            if (sawmill != "tts" || message.RenderMessage() != "TTS API URL is not configured")
                return false;

            missingEndpointLogs++;
            return true;
        }

        var previousFailureLevel = Pair.ServerLogHandler.FailureLevel;
        Pair.ServerLogHandler.JudgeLog += JudgeMissingEndpoint;
        Pair.ServerLogHandler.FailureLevel = LogLevel.Warning;
        try
        {
            EntityUid speaker = default;
            await Server.WaitAssertion(() =>
            {
                speaker = SSpawnAtPosition(Speaker.Id, map.GridCoords);
                SEntMan.EnsureComponent<TTSComponent>(speaker).VoicePrototype = OriginalVoice;
                var announce = new TTSAnnouncePlayEvent("Disabled TTS announcement", speaker, Filter.Empty());
                SEntMan.EventBus.RaiseEvent(EventSource.Local, ref announce);
            });
            await Server.WaitPost(() => Server.ResolveDependency<ITimerManager>().UpdateTimers(new FrameEventArgs(3)));
            Assert.That(missingEndpointLogs, Is.Zero, "Disabled announcements must not reach the provider.");

            await OverrideCVar(Side.Server, ArcaneCVars.TTSEnabled, true);
            await Server.WaitPost(() =>
            {
                var announce = new TTSAnnouncePlayEvent("Enabled TTS announcement", speaker, Filter.Empty());
                SEntMan.EventBus.RaiseEvent(EventSource.Local, ref announce);
            });
            // Timer.Spawn is advanced by ITimerManager, independently of simulated game ticks.
            await Server.WaitPost(() => Server.ResolveDependency<ITimerManager>().UpdateTimers(new FrameEventArgs(3)));
            Assert.That(missingEndpointLogs, Is.EqualTo(1), "The enabled path must attempt synthesis and handle the missing endpoint.");
        }
        finally
        {
            Pair.ServerLogHandler.FailureLevel = previousFailureLevel;
            Pair.ServerLogHandler.JudgeLog -= JudgeMissingEndpoint;
            await Server.WaitPost(() => Server.ResolveDependency<TTSManager>().ResetCache());
        }
    }

    [Test]
    public async Task ProviderFailureAllowsRetryWithTheChosenVoice()
    {
        var portReservation = new TcpListener(IPAddress.Loopback, 0);
        portReservation.Start();
        var port = ((IPEndPoint) portReservation.LocalEndpoint).Port;
        portReservation.Stop();
        using var provider = new HttpListener();
        var endpoint = $"http://127.0.0.1:{port}/";
        provider.Prefixes.Add(endpoint);
        provider.Start();

        await OverrideCVar(Side.Server, ArcaneCVars.TTSApiUrl, endpoint);
        await OverrideCVar(Side.Server, ArcaneCVars.TTSApiToken, "local-tts-test-token");
        var manager = Server.ResolveDependency<TTSManager>();
        await Server.WaitPost(manager.ResetCache);
        bool JudgeUnavailableProvider(string sawmill, LogEvent message) => sawmill == "tts"
            && message.RenderMessage() == "TTS request returned bad status code: ServiceUnavailable";
        Pair.ServerLogHandler.JudgeLog += JudgeUnavailableProvider;
        try
        {
            var voice = Server.ProtoMan.Index(SelectedVoice);
            var failedSynthesis = manager.ConvertTextToSpeech(voice.Speaker, "Local test", TTSEffects.RadioHeadset);
            var failedRequest = await provider.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(failedRequest.Request.QueryString["speaker"], Is.EqualTo(voice.Speaker));
            Assert.That(failedRequest.Request.QueryString["text"], Is.EqualTo("Local test"));
            Assert.That(failedRequest.Request.QueryString["effect"], Is.EqualTo(TTSEffects.RadioHeadset));
            failedRequest.Response.StatusCode = (int) HttpStatusCode.ServiceUnavailable;
            failedRequest.Response.Close();
            Assert.That(await failedSynthesis, Is.Null);

            var successfulSynthesis = manager.ConvertTextToSpeech(voice.Speaker, "Local test", TTSEffects.RadioHeadset);
            var successfulRequest = await provider.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(successfulRequest.Request.QueryString["speaker"], Is.EqualTo(voice.Speaker));
            Assert.That(successfulRequest.Request.QueryString["ext"], Is.EqualTo("ogg"));
            var payload = new byte[] { 1, 2, 3 };
            successfulRequest.Response.ContentType = "audio/ogg";
            successfulRequest.Response.ContentLength64 = payload.Length;
            await successfulRequest.Response.OutputStream.WriteAsync(payload);
            successfulRequest.Response.Close();
            Assert.That(await successfulSynthesis, Is.EqualTo(payload));
            Assert.That(manager.Cache, Has.Count.EqualTo(1));
        }
        finally
        {
            Pair.ServerLogHandler.JudgeLog -= JudgeUnavailableProvider;
            await Server.WaitPost(manager.ResetCache);
        }
    }

    [Test]
    public async Task VoiceSelectionRequiresAnOpenMenuAndAnAllowedVoice()
    {
        var map = await Pair.CreateTestMap();
        EntityUid speaker = default;
        EntityUid? previousAttachment = null;
        try
        {
            await Server.WaitPost(() =>
            {
                previousAttachment = ServerSession!.AttachedEntity;
                speaker = SSpawnAtPosition(Speaker.Id, map.GridCoords);
                SEntMan.EnsureComponent<TTSComponent>(speaker).VoicePrototype = OriginalVoice;
                Server.PlayerMan.SetAttachedEntity(ServerSession, speaker);
            });
            await Pair.RunUntilSynced();

            await Client.WaitPost(() => Client.System<TTSVoiceChangeSystem>().SendSelectedVoice(SelectedVoice));
            await Pair.RunUntilSynced();
            await Server.WaitAssertion(() => Assert.That(SComp<TTSComponent>(speaker).VoicePrototype, Is.EqualTo(OriginalVoice)));

            await Server.WaitPost(() =>
            {
                var open = new TTSVoiceChangeOpenMenuEvent { Performer = speaker };
                SEntMan.EventBus.RaiseEvent(EventSource.Local, open);
            });
            await Pair.RunUntilSynced();
            await Client.WaitPost(() => Client.System<TTSVoiceChangeSystem>().SendSelectedVoice(OriginalVoice));
            await Pair.RunUntilSynced();
            await Client.WaitPost(() => Client.System<TTSVoiceChangeSystem>().SendSelectedVoice(SelectedVoice));
            await Pair.RunUntilSynced();
            await Server.WaitAssertion(() => Assert.That(SComp<TTSComponent>(speaker).VoicePrototype, Is.EqualTo(OriginalVoice),
                "A rejected choice consumes the pending menu."));

            await Server.WaitPost(() =>
            {
                var open = new TTSVoiceChangeOpenMenuEvent { Performer = speaker };
                SEntMan.EventBus.RaiseEvent(EventSource.Local, open);
            });
            await Pair.RunUntilSynced();
            await Client.WaitPost(() => Client.System<TTSVoiceChangeSystem>().SendSelectedVoice(SelectedVoice));
            await Pair.RunUntilSynced();
            await Server.WaitAssertion(() => Assert.That(SComp<TTSComponent>(speaker).VoicePrototype, Is.EqualTo(SelectedVoice)));
        }
        finally
        {
            await Client.WaitPost(() => Client.ResolveDependency<IUserInterfaceManager>()
                .GetUIController<TTSVoiceChangeUIController>().CloseWindow());
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession!, previousAttachment));
        }
    }
}
