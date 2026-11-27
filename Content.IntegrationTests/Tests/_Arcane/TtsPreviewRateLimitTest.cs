// Arcane-Start: enforce the configured per-session preview limit
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Server.TTS;
using Content.Arcane.Shared.TTS;
using Content.IntegrationTests.Fixtures;
using Content.Server.Players.RateLimiting;
using ClientTtsSystem = Content.Arcane.Client.TTS.TTSSystem;
using ServerTtsSystem = Content.Arcane.Server.TTS.TTSSystem;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class TtsPreviewRateLimitTest : GameTest
{
    [Test]
    public async Task PreviewRequestsRespectConfiguredSessionLimit()
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
        await OverrideCVar(Side.Server, ArcaneCVars.TTSRateLimitPeriod, 60f);
        await OverrideCVar(Side.Server, ArcaneCVars.TTSRateLimitCount, 1);
        await OverrideCVar(Side.Client, ArcaneCVars.UseTTS, false);
        var manager = Server.ResolveDependency<TTSManager>();
        await Server.WaitPost(manager.ResetCache);
        var field = typeof(ServerTtsSystem).GetField("_ttsTasks", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var system = Server.System<ServerTtsSystem>();
        var tasks = (IDictionary) field.GetValue(system)!;

        try
        {
            await Client.WaitPost(() => Client.System<ClientTtsSystem>().RequestGlobalTTS(VoiceRequestType.Preview, "Gman"));
            await Pair.RunUntilSynced();
            var request = await provider.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(10));
            await Client.WaitPost(() => Client.System<ClientTtsSystem>().RequestGlobalTTS(VoiceRequestType.Preview, "Glados"));
            await Pair.RunTicksSync(5);
            await Server.WaitAssertion(() =>
            {
                var limiter = Server.ResolveDependency<PlayerRateLimitManager>();
                var dataField = typeof(PlayerRateLimitManager).GetField("_rateLimitData", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var sessions = (IDictionary) dataField.GetValue(limiter)!;
                var actions = (IDictionary) sessions[ServerSession!]!;
                var datum = actions["TTSPreview"]!;
                var count = (int) datum.GetType().GetField("Count")!.GetValue(datum)!;
                Assert.That(count, Is.EqualTo(2), "Both requests must have reached the session limiter.");
                Assert.That(tasks.Count, Is.EqualTo(1), "The second voice must be blocked before synthesis.");
            });
            request.Response.ContentLength64 = 1;
            await request.Response.OutputStream.WriteAsync(new byte[] { 1 });
            request.Response.Close();
            await Pair.RunTicksSync(5);
        }
        finally
        {
            await Server.WaitPost(manager.ResetCache);
        }
    }
}
// Arcane-End
