using Content.Arcane.Shared.PDA;
using Content.Client.PDA;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Clothing.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.PDA;
using Robust.Client.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PdaPenVisualTest : InteractionTest
{
    [Test]
    public async Task DisguisesKeepTheRealPenIdAndFlashlightWithTheSkin()
    {
        var pda = await Spawn("ChameleonPDA");
        var card = await Spawn("PassengerIDCard");
        var id = ToServer(card);
        await Server.WaitAssertion(() =>
        {
            var uid = ToServer(pda);
            var component = SEntMan.GetComponent<PdaComponent>(uid);
            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(uid, component.IdSlot, id, null), Is.True);
            Assert.That(component.ContainedId, Is.EqualTo(id));
            SEntMan.System<UnpoweredFlashlightSystem>().SetLight(uid, true, quiet: true);
        });
        foreach (var penInserted in new[] {true, false})
        {
            if (!penInserted)
            {
                await Server.WaitAssertion(() =>
                {
                    var uid = ToServer(pda);
                    var component = SEntMan.GetComponent<PdaComponent>(uid);
                    Assert.That(SEntMan.System<ItemSlotsSystem>().TryEject(uid, component.PenSlot, null, out _), Is.True);
                });
            }
            foreach (var disguise in new[] {"CaptainPDA", "AltCaptainPDA", "PassengerPDA"})
            {
                await Server.WaitAssertion(() =>
                {
                    var uid = ToServer(pda);
                    SEntMan.System<ChameleonClothingSystem>().SetSelectedPrototype(uid, disguise);
                    Assert.That(SEntMan.GetComponent<ChameleonClothingComponent>(uid).Default, Is.EqualTo(disguise));
                    Assert.That(SEntMan.GetComponent<PdaComponent>(uid).ContainedId, Is.EqualTo(id));
                    Assert.That(SEntMan.GetComponent<UnpoweredFlashlightComponent>(uid).LightOn, Is.True);
                });
                await Pair.RunUntilSynced();
                await AssertPenVisible(pda, penInserted);
                await Client.WaitAssertion(() =>
                {
                    var uid = ToClient(pda);
                    var sprite = CEntMan.GetComponent<SpriteComponent>(uid);
                    var sprites = CEntMan.System<SpriteSystem>();
                    Assert.That(sprites.LayerMapTryGet((uid, sprite), PdaScreenLayers.Screen, out _, false), Is.True);
                    Assert.That(sprite[PdaVisualizerSystem.PdaVisualLayers.Base].Visible, Is.False);
                    Assert.That(sprite[PdaVisualizerSystem.PdaVisualLayers.IdLight].Visible, Is.True);
                    Assert.That(sprite[PdaVisualizerSystem.PdaVisualLayers.Flashlight].Visible, Is.True);
                });
            }
        }
    }

    [TestCase("PassengerPDA")]
    [TestCase("CaptainPDA")]
    [TestCase("AltCaptainPDA")]
    [TestCase("ChronoPDA")]
    public async Task PenVisibilityTracksTheActualSlot(string prototype)
    {
        var pda = await Spawn(prototype);
        await Pair.RunUntilSynced();
        await AssertPenVisible(pda, true);
        EntityUid pen = default;
        EntityUid? id = null;
        await Server.WaitAssertion(() =>
        {
            var uid = ToServer(pda);
            var component = SEntMan.GetComponent<PdaComponent>(uid);
            id = component.ContainedId;
            Assert.That(SEntMan.System<ItemSlotsSystem>().TryEject(uid, component.PenSlot, null, out var ejected), Is.True);
            pen = ejected!.Value;
        });
        await Pair.RunUntilSynced();
        await AssertPenVisible(pda, false);
        await Server.WaitAssertion(() =>
        {
            var uid = ToServer(pda);
            var component = SEntMan.GetComponent<PdaComponent>(uid);
            Assert.That(component.ContainedId, Is.EqualTo(id), "The pen must not change the ID slot.");
            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(uid, component.PenSlot, pen, null), Is.True);
        });
        await Pair.RunUntilSynced();
        await AssertPenVisible(pda, true);
    }

    private async Task AssertPenVisible(NetEntity pda, bool expected)
    {
        await Server.WaitAssertion(() =>
        {
            var uid = ToServer(pda);
            Assert.That(SEntMan.GetComponent<PdaComponent>(uid).PenSlot.HasItem, Is.EqualTo(expected));
            Assert.That(SEntMan.System<SharedAppearanceSystem>().TryGetData<bool>(uid, PdaVisuals.PenInserted, out var visible), Is.True);
            Assert.That(visible, Is.EqualTo(expected));
        });
        await Client.WaitAssertion(() =>
        {
            var uid = ToClient(pda);
            var sprite = CEntMan.GetComponent<SpriteComponent>(uid);
            Assert.That(CEntMan.System<SpriteSystem>().LayerMapTryGet((uid, sprite), PdaVisualizerSystem.PdaVisualLayers.Pen, out var layer, false), Is.True);
            Assert.That(sprite[layer].Visible, Is.EqualTo(expected));
        });
    }
}
