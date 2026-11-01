#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared;
using Content.Shared.Mind;
using Content.Trauma.Shared.Kudzu;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class FoliageVisionLifecycleTest : InteractionTest
{
    protected override string PlayerPrototype => "MobDiona";
    public override PoolSettings PoolSettings => new() { Connected = true, DummyTicker = false, Dirty = true };

    [Test]
    public async Task BodyAndComponentTransitionsRestoreOrdinaryFoliageLayering()
    {
        await SpawnTarget("Kudzu");
        var plant = STarget!.Value;
        EntityUid mind = default;
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<FoliageIgnoringVisionComponent>(SPlayer), Is.True);
            var minds = SEntMan.System<SharedMindSystem>();
            mind = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(mind, SPlayer);
        });
        await AssertLayer(true);

        await Server.WaitPost(() =>
        {
            var human = SEntMan.SpawnEntity("MobHuman", SEntMan.GetCoordinates(PlayerCoords));
            SEntMan.System<SharedMindSystem>().TransferTo(mind, human);
        });
        await AssertLayer(false);
        await Server.WaitPost(() => SEntMan.System<SharedMindSystem>().TransferTo(mind, SPlayer));
        await AssertLayer(true);

        await Server.WaitPost(() => SEntMan.RemoveComponent<FoliageIgnoringVisionComponent>(SPlayer));
        await AssertLayer(false);
        await Server.WaitPost(() => SEntMan.EnsureComponent<FoliageIgnoringVisionComponent>(SPlayer));
        await AssertLayer(true);
        await Server.WaitPost(() => SEntMan.RemoveComponent<HideableFoliageComponent>(plant));
        await AssertLayer(false);
        await Server.WaitPost(() =>
        {
            SEntMan.DeleteEntity(plant);
            SEntMan.System<SharedMindSystem>().WipeMind(mind);
        });
        await Pair.RunUntilSynced();
    }

    private async Task AssertLayer(bool lowered)
    {
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var sprite = CEntMan.GetComponent<Robust.Client.GameObjects.SpriteComponent>(CTarget!.Value);
            Assert.That(sprite.DrawDepth, Is.EqualTo(DrawDepth.Default + (lowered ? -5 : 10)));
        });
    }
}
