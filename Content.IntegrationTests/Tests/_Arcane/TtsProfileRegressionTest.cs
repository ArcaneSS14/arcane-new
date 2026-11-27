// Arcane-Start: cloning and profile validation preserve the intended voice contract
using Content.Arcane.Shared.TTS;
using Content.IntegrationTests.Fixtures;
using Content.Server.Cloning;
using Content.Shared.Cloning;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.TTS;
using Content.Trauma.Server.Abductor;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class TtsProfileRegressionTest : GameTest
{
    [Test]
    public async Task DisguiseRestoresLiveVoice()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var mob = SSpawnAtPosition("MobHuman", map.GridCoords);
            var humanoids = Server.System<HumanoidProfileSystem>();
            var original = HumanoidCharacterProfile.DefaultWithSpecies("Human").WithTtsVoice("Lina_dota_2");
            humanoids.ApplyProfileTo(mob, original);
            var disguise = Server.System<AbductorVestDisguiseSystem>();
            disguise.ApplyDisguise(mob, HumanoidCharacterProfile.DefaultWithSpecies("Human").WithName("Temporary disguise").WithTtsVoice("Jackie"));
            Assert.That(SEntMan.GetComponent<TTSComponent>(mob).VoicePrototype, Is.EqualTo(new ProtoId<TTSVoicePrototype>("Jackie")));
            disguise.RestoreAppearance(mob);
            Assert.That(SEntMan.GetComponent<TTSComponent>(mob).VoicePrototype, Is.EqualTo(new ProtoId<TTSVoicePrototype>("Lina_dota_2")));
        });
    }

    [Test]
    public async Task CloneKeepsSelectedVoice()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var original = SSpawnAtPosition("MobHuman", map.GridCoords);
            SEntMan.GetComponent<TTSComponent>(original).VoicePrototype = "Lina_dota_2";
            var cloning = Server.System<CloningSystem>();
            Assert.That(cloning.TryCloning(original, map.MapCoords, new ProtoId<CloningSettingsPrototype>("CloningPod"), out var clone), Is.True);
            Assert.That(clone, Is.Not.Null);
            Assert.That(SEntMan.GetComponent<TTSComponent>(clone!.Value).VoicePrototype,
                Is.EqualTo(new ProtoId<TTSVoicePrototype>("Lina_dota_2")));
        });
    }

    [Test]
    public async Task RestrictedVoiceIsRejectedButAllowedVoiceIsKept()
    {
        await Server.WaitAssertion(() =>
        {
            var prototypes = Server.ResolveDependency<IPrototypeManager>();
            var restricted = HumanoidCharacterProfile.DefaultWithSpecies("Human").WithTtsVoice("Papich");
            restricted.EnsureValid(ServerSession!, IoCManager.Instance!);
            Assert.That(prototypes.Index(restricted.TtsVoice).RoundStart, Is.True);
            Assert.That(restricted.TtsVoice, Is.Not.EqualTo(new ProtoId<TTSVoicePrototype>("Papich")));

            var allowed = HumanoidCharacterProfile.DefaultWithSpecies("Human").WithTtsVoice("Lina_dota_2");
            allowed.EnsureValid(ServerSession!, IoCManager.Instance!);
            Assert.That(allowed.TtsVoice, Is.EqualTo(new ProtoId<TTSVoicePrototype>("Lina_dota_2")));
        });
    }
}
// Arcane-End
