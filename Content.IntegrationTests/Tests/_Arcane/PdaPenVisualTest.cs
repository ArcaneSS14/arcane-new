using Content.Client.PDA;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.PDA;
using Robust.Client.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PdaPenVisualTest : InteractionTest
{
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
