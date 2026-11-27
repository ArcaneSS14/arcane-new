// Arcane-Start: playable species can offer items, and stale offers cannot move an old item
using System.Linq;
using Content.Arcane.Server.OfferItem;
using Content.Arcane.Shared.OfferItem;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class OfferItemRuntimeTest : GameTest
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task OfferRequiresTheSameHeldItem(bool replaceItem)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var giver = SSpawnAtPosition("MobHuman", map.GridCoords);
            var recipient = SSpawnAtPosition("MobHuman", map.GridCoords);
            var skeleton = SSpawnAtPosition("MobSkeletonPerson", map.GridCoords);
            var ipc = SSpawnAtPosition("MobIPC", map.GridCoords);
            Assert.That(SEntMan.HasComponent<OfferItemComponent>(giver), Is.True);
            Assert.That(SEntMan.HasComponent<OfferItemComponent>(recipient), Is.True);
            Assert.That(SEntMan.HasComponent<OfferItemComponent>(skeleton), Is.True);
            Assert.That(SEntMan.HasComponent<OfferItemComponent>(ipc), Is.True);

            var offered = SSpawnAtPosition("Pen", map.GridCoords);
            var replacement = SSpawnAtPosition("Pen", map.GridCoords);
            var hands = Server.System<SharedHandsSystem>();
            Assert.That(hands.TryPickup(giver, offered, checkActionBlocker: false), Is.True);
            var hand = SEntMan.GetComponent<HandsComponent>(giver).ActiveHandId!;
            var verb = Server.System<SharedVerbSystem>().GetLocalVerbs(recipient, giver, typeof(InteractionVerb))
                .Single(v => v.Text == Loc.GetString("offer-item-verb"));
            verb.Act!();

            if (replaceItem)
            {
                Assert.That(hands.TryDrop(giver, offered), Is.True);
                Assert.That(hands.TryPickup(giver, replacement, hand, checkActionBlocker: false), Is.True);
            }

            Server.System<OfferItemSystem>().Receive(recipient);
            Assert.That(hands.GetActiveItem(recipient), Is.EqualTo(replaceItem ? null : offered));
            if (replaceItem)
                Assert.That(hands.GetHeldItem((giver, SEntMan.GetComponent<HandsComponent>(giver)), hand), Is.EqualTo(replacement));
        });
    }

    [Test]
    public async Task RemovedRecipientDoesNotLeaveAnOfferStuck()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var giver = SSpawnAtPosition("MobHuman", map.GridCoords);
            var recipient = SSpawnAtPosition("MobHuman", map.GridCoords);
            var item = SSpawnAtPosition("Pen", map.GridCoords);
            Assert.That(Server.System<SharedHandsSystem>().TryPickup(giver, item, checkActionBlocker: false), Is.True);
            var verbs = Server.System<SharedVerbSystem>();
            var offer = verbs.GetLocalVerbs(recipient, giver, typeof(InteractionVerb))
                .Single(v => v.Text == Loc.GetString("offer-item-verb"));
            offer.Act!();
            SEntMan.DeleteEntity(recipient);
            Server.System<SharedTransformSystem>().SetCoordinates(giver, map.GridCoords.Offset(new Vector2(0.1f, 0)));
            var next = SSpawnAtPosition("MobHuman", map.GridCoords);
            Assert.That(verbs.GetLocalVerbs(next, giver, typeof(InteractionVerb))
                .Any(v => v.Text == Loc.GetString("offer-item-verb")), Is.True);
        });
    }
}
// Arcane-End
