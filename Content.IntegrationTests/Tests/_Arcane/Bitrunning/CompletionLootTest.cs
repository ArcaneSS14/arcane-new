#nullable enable
using System.Linq;
using Content.Arcane.Server.Bitrunning.Systems;
using Content.Arcane.Shared.Bitrunning;
using Content.Arcane.Shared.Bitrunning.Components;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane.Bitrunning;

[TestOf(typeof(ByteforgeSystem))]
public sealed class CompletionLootTest : GameTest
{
    private const string Domain = "BeachBar";
    private static readonly EntProtoId QuantumServer = "QuantumServer";
    private static readonly EntProtoId Byteforge = "Byteforge";
    private static readonly EntProtoId Cargo = "BitrunningObjectiveCacheStructure";
    private static readonly EntProtoId Loot = "BeachBall";

    [SidedDependency(Side.Server)] private readonly ByteforgeSystem _byteforge = null!;
    [SidedDependency(Side.Server)] private readonly QuantumServerSystem _server = null!;

    [Test]
    public async Task FixedLootComesOnceAfterTheObjective()
    {
        await Pair.CreateTestMap();
        EntityUid server = default;

        await Server.WaitAssertion(() =>
        {
            server = SSpawnAtPosition(QuantumServer, TestMap!.GridCoords);
            var byteforge = SSpawnAtPosition(Byteforge, TestMap.GridCoords);
            var serverComp = SComp<QuantumServerComponent>(server);
            serverComp.LinkedByteforge = byteforge;
            serverComp.CurrentDomain = Domain;
            SComp<ByteforgeComponent>(byteforge).LinkedServer = server;

            Assert.That(Deliver(server, false), Is.True);
            Assert.That(CountLoot(), Is.Zero, "Delivered before the objective was done.");

            Assert.That(Deliver(server, true), Is.True);
            Assert.That(CountLoot(), Is.EqualTo(1), "The delivery that finished the objective has no fixed loot.");

            Assert.That(Deliver(server, true), Is.True);
            Assert.That(CountLoot(), Is.EqualTo(1), "A second delivery repeated the fixed loot.");
        });
    }

    [Test]
    public async Task LastDeliveryCountsAsDone()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var server = SComp<QuantumServerComponent>(SSpawnAtPosition(QuantumServer, TestMap!.GridCoords));
            server.State = BitrunningServerState.Running;
            server.ObjectiveType = BitrunningObjectiveType.DeliveryCacheCrate;
            server.ObjectiveGoal = 2;
            server.ObjectivePoints = 1;

            Assert.That(_server.IsObjectiveDoneWith(server, 0), Is.False);
            Assert.That(_server.IsObjectiveDoneWith(server, 1), Is.True);

            server.State = BitrunningServerState.Ready;
            Assert.That(_server.IsObjectiveDoneWith(server, 1), Is.False);
        });
    }

    private bool Deliver(EntityUid server, bool objectiveDone)
    {
        var cargo = SSpawnAtPosition(Cargo, TestMap!.GridCoords);
        return _byteforge.TryDeliverObjectiveCargoToByteforge(server, cargo, objectiveDone);
    }

    private int CountLoot()
    {
        return SEntMan.AllEntities<MetaDataComponent>().Count(ent => ent.Comp.EntityPrototype?.ID == Loot.Id);
    }
}
