// Arcane-Start: preserve the whisper flag for speech consumers
using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.TTS;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class TtsWhisperTest : GameTest
{
    [Test]
    [EnsureCVar(Side.Server, typeof(ArcaneCVars), nameof(ArcaneCVars.TTSEnabled), false)]
    public async Task SpeechEventsDistinguishWhispersFromOrdinarySpeech()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var speaker = SSpawnAtPosition("MobHuman", map.GridCoords);
            SEntMan.EnsureComponent<TTSComponent>(speaker);
            SEntMan.EnsureComponent<WhisperFlagProbeComponent>(speaker);
            var probe = Server.System<WhisperFlagProbeSystem>();
            probe.Flags.Clear();
            var chat = Server.System<ChatSystem>();
            chat.TrySendInGameICMessage(speaker, "normal speech", InGameICChatType.Speak, false);
            chat.TrySendInGameICMessage(speaker, "quiet speech", InGameICChatType.Whisper, false);
            Assert.That(probe.Flags, Is.EqualTo(new[] { false, true }));
        });
    }
}

public sealed partial class WhisperFlagProbeSystem : EntitySystem
{
    public readonly List<bool> Flags = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<WhisperFlagProbeComponent, EntitySpokeEvent>(OnSpoke);
    }

    private void OnSpoke(EntityUid uid, WhisperFlagProbeComponent component, EntitySpokeEvent args)
    {
        Flags.Add(args.IsWhisper);
    }
}

[RegisterComponent]
public sealed partial class WhisperFlagProbeComponent : Component;
// Arcane-End
