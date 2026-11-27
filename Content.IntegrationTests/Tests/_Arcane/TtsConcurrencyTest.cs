// Arcane-Start: duplicate synthesis must not delay unrelated requests
#nullable enable
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Diagnostics;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Server.TTS;
using Content.IntegrationTests.Fixtures;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class TtsConcurrencyTest : GameTest
{
    [Test]
    public async Task DuplicateRequestDoesNotBlockDifferentText()
    {
        var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint) reservation.LocalEndpoint).Port;
        reservation.Stop();
        using var provider = new HttpListener();
        provider.Prefixes.Add($"http://127.0.0.1:{port}/");
        provider.Start();

        await OverrideCVar(Side.Server, ArcaneCVars.TTSApiUrl, $"http://127.0.0.1:{port}/");
        await OverrideCVar(Side.Server, ArcaneCVars.TTSApiToken, "local-tts-test-token");
        await OverrideCVar(Side.Server, ArcaneCVars.TTSApiTimeout, 30);
        var manager = Server.ResolveDependency<TTSManager>();
        await Server.WaitPost(manager.ResetCache);
        var system = Server.System<TTSSystem>();
        var generate = typeof(TTSSystem).GetMethod("GenerateTTS", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Task<byte[]?> first = default!;
        Task<byte[]?> duplicate = default!;
        Task<byte[]?> other = default!;

        try
        {
            await Server.WaitPost(() => first = (Task<byte[]?>) generate.Invoke(system, ["First", "Gman", null])!);
            var firstRequest = await provider.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(10));
            await Server.WaitPost(() =>
            {
                duplicate = (Task<byte[]?>) generate.Invoke(system, ["First", "Gman", null])!;
                other = (Task<byte[]?>) generate.Invoke(system, ["Other", "Gman", null])!;
            });

            var otherRequest = await provider.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(otherRequest.Request.QueryString["text"], Is.Not.EqualTo(firstRequest.Request.QueryString["text"]));
            otherRequest.Response.ContentLength64 = 1;
            await otherRequest.Response.OutputStream.WriteAsync(new byte[] { 2 });
            otherRequest.Response.Close();
            firstRequest.Response.ContentLength64 = 1;
            await firstRequest.Response.OutputStream.WriteAsync(new byte[] { 1 });
            firstRequest.Response.Close();

            var combined = Task.WhenAll(first, duplicate, other);
            var deadline = Stopwatch.StartNew();
            while (!combined.IsCompleted && deadline.Elapsed < TimeSpan.FromSeconds(10))
            {
                await Server.WaitRunTicks(1);
                await Task.Delay(10);
            }
            var results = await combined.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.That(results[0], Is.EqualTo(new byte[] { 1 }));
            Assert.That(results[1], Is.EqualTo(new byte[] { 1 }));
            Assert.That(results[2], Is.EqualTo(new byte[] { 2 }));
        }
        finally
        {
            await Server.WaitPost(manager.ResetCache);
        }
    }
}
// Arcane-End
