// Arcane-Start: a mounted TTS resource root must remain readable after shutdown
using System.Reflection;
using Content.Arcane.Client.TTS;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture, Order(1)]
public sealed class TtsResourceRootLifecycleTest : GameTest
{
    [Test]
    public async Task ShutdownLeavesMountedRootEmptyAndReadable()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Fresh = true,
            Destructive = true,
        });
        var client = pair.Client;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var file = new ResPath("lifecycle.ogg");
        MemoryContentRoot root = default!;
        IResourceManager resources = default!;
        ResPath prefix = default;
        await client.WaitAssertion(() =>
        {
            var system = client.System<TTSSystem>();
            prefix = (ResPath) typeof(TTSSystem).GetField("_prefix", flags)!.GetValue(system)!;
            root = (MemoryContentRoot) typeof(TTSSystem).GetField("_contentRoot", flags)!.GetValue(system)!;
            resources = client.ResolveDependency<IResourceManager>();
            root.AddOrUpdateFile(file, new byte[] { 1 });
            Assert.That(resources.ContentFileExists(prefix / file), Is.True);
        });

        await pair.CleanReturnAsync();

        Assert.That(root.FileExists(file), Is.False);
        Assert.That(resources.ContentFileExists(prefix / file), Is.False);
        Assert.That(resources.ContentFileExists(prefix / "missing.ogg"), Is.False);
    }
}
// Arcane-End
