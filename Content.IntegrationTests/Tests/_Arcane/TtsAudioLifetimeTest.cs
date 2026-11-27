// Arcane-Start: verify TTS stream ownership across playback and cancellation
using System.IO;
using System.Linq;
using System.Reflection;
using Content.Arcane.Client.TTS;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.TTS;
using Content.IntegrationTests.Fixtures;
using Robust.Client.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.Spawners;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class TtsAudioLifetimeTest : GameTest
{
    [Test]
    public async Task StreamsFollowPlaybackAndCancelledPreview()
    {
        await OverrideCVar(Side.Client, ArcaneCVars.UseTTS, true);
        var system = Client.System<TTSSystem>();
        var handler = typeof(TTSSystem).GetMethod("OnPlayTTS", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var streamsField = typeof(TTSSystem).GetField("_playingStreams", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var streams = (Dictionary<EntityUid, AudioStream>) streamsField.GetValue(system)!;
        byte[] audio = default!;
        EntityUid first = default;
        EntityUid second = default;

        await Client.WaitAssertion(() =>
        {
            using var input = Client.ResolveDependency<IResourceManager>().ContentFileRead(new ResPath("/Audio/_Arcane/Radio/basic.ogg"));
            using var copy = new MemoryStream();
            input.CopyTo(copy);
            audio = copy.ToArray();
            var existing = streams.Keys.ToHashSet();

            handler.Invoke(system, [new PlayTTSEvent(audio, isPreview: true)]);
            first = streams.Keys.Single(uid => !existing.Contains(uid));
            Assert.That(CEntMan.EntityExists(first), Is.True);

            handler.Invoke(system, [new PlayTTSEvent(audio, isPreview: true)]);
            second = streams.Keys.Single(uid => !existing.Contains(uid) && uid != first);
        });

        await Pair.RunTicksSync(5);
        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.EntityExists(first), Is.False);
            Assert.That(streams.ContainsKey(first), Is.False);
            Assert.That(streams.ContainsKey(second), Is.True);
            CEntMan.GetComponent<TimedDespawnComponent>(second).Lifetime = 0;
        });
        await Pair.RunTicksSync(5);
        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.EntityExists(second), Is.False);
            Assert.That(streams.ContainsKey(second), Is.False);
            var previousCount = streams.Count;
            handler.Invoke(system, [new PlayTTSEvent(audio, new NetEntity(int.MaxValue))]);
            Assert.That(streams.Count, Is.EqualTo(previousCount), "An unavailable speaker must not retain its audio stream.");
        });
    }
}
// Arcane-End
