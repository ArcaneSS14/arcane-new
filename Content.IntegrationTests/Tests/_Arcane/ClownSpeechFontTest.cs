#nullable enable
using System.Linq;
using Content.Arcane.Shared.Speech;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Content.Trauma.Common.Speech;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class ClownSpeechFontTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task WornMaskVerbChangesSpeechAndReplicatesWithoutAffectingUnequippedSpeech()
    {
        await SpawnTarget("ClothingMaskClown");
        var mask = STarget!.Value;
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<ClothingSpeechFontComponent>(mask).Enabled, Is.False);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(SPlayer, mask, "mask", force: true), Is.True);
            Assert.That(WrappedSpeech(), Does.Not.Contain("LDFComicSans"));
            Toggle(mask);
            Assert.That(WrappedSpeech(), Does.Contain("LDFComicSans"));
            var font = new SpeechFontOverrideEvent(SPlayer, "Default");
            SEntMan.EventBus.RaiseLocalEvent(SPlayer, ref font);
            Assert.That(font.Color, Is.EqualTo(Color.FromHex("#e93790")));
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
            Assert.That(CEntMan.GetComponent<ClothingSpeechFontComponent>(CTarget!.Value).Enabled, Is.True));

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<InventorySystem>().TryUnequip(SPlayer, "mask", out var removed, force: true), Is.True);
            Assert.That(removed, Is.EqualTo(mask));
            Assert.That(WrappedSpeech(), Does.Not.Contain("LDFComicSans"));
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(SPlayer, mask, "mask", force: true), Is.True);
            Assert.That(WrappedSpeech(), Does.Contain("LDFComicSans"));
            Toggle(mask);
            Assert.That(WrappedSpeech(), Does.Not.Contain("LDFComicSans"));
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
            Assert.That(CEntMan.GetComponent<ClothingSpeechFontComponent>(CTarget!.Value).Enabled, Is.False));
    }

    private string WrappedSpeech()
    {
        var chat = SEntMan.System<SharedChatSystem>();
        return chat.WrapPublicMessage(SPlayer, "Speaker", "Hello", chat.GetSpeechVerb(SPlayer, "Hello"));
    }

    private void Toggle(EntityUid mask)
    {
        SEntMan.System<SharedVerbSystem>().GetLocalVerbs(mask, SPlayer, typeof(AlternativeVerb))
            .Single(verb => verb.Text == Loc.GetString("arcane-clothing-speech-font-toggle")).Act!.Invoke();
    }
}
