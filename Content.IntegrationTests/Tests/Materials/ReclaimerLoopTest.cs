using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.IntegrationTests.Utility;
using Content.Server.Materials;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Materials;

/// <summary>
/// Tests to prevent Recycler loops, where the product of one recycling can be recycled again.
/// </summary>
[TestOf(typeof(MaterialReclaimerSystem))]
[TestOf(typeof(MaterialReclaimerComponent))]
public sealed class ReclaimerLoopTest : InteractionTest
{
    // ProtoIDs we need
    private static readonly EntProtoId ApcId = "APCBasic";
    private static readonly EntProtoId FloorTileId = "FloorTileItemSteel";

    // private static readonly string[] Reclaimers = GameDataScrounger.EntitiesWithComponent("MaterialReclaimer"); // Arcane-Edit

    [SidedDependency(Side.Server)] private readonly SharedMaterialReclaimerSystem _materialReclaimerSystem = null!;

    [Test]
    // [TestCaseSource(nameof(Reclaimers))] // Arcane-Edit
    [TestOf(typeof(MaterialReclaimerSystem))]
    [TestOf(typeof(MaterialReclaimerComponent))]
    [Description("For every material that a reclaimer can spawn, make sure that it cannot get stuck in a loop of spawning then recycling.")]
    [TrackingIssue("https://github.com/space-wizards/space-station-14/issues/39691")]
    // Arcane-Edit-Start
    public async Task MaterialSpawnLoopTest()
    {
        var tested = 0;
        foreach (var reclaimerId in GameDataScrounger.EntitiesWithComponent(SProtoMan, "MaterialReclaimer"))
        {
            var prototype = SProtoMan.Index<EntityPrototype>(reclaimerId);
            Assert.That(prototype.TryComp<MaterialReclaimerComponent>(out var data, SEntMan.ComponentFactory), Is.True, reclaimerId);
            if (!data.ReclaimMaterials)
                continue;

            var apc = await CheckMaterialSpawnLoop(reclaimerId);
            await DeleteHeldEntity();
            await Server.WaitPost(() =>
            {
                SEntMan.DeleteEntity(STarget!.Value);
                SEntMan.DeleteEntity(apc);
            });
            tested++;
        }
        Assert.That(tested, Is.Positive, "No material-reclaiming machines were checked.");
    }

    private async Task<EntityUid> CheckMaterialSpawnLoop(string reclaimerId)
    // Arcane-Edit-End
    {
        // Spawn the reclaimer
        await SpawnTarget(reclaimerId, PlayerCoords);
        Assert.That(STarget, Is.Not.Null, "STarget was null, did the reclaimer spawn correctly?"); // Arcane-Edit

        var reclaimComp = Comp<MaterialReclaimerComponent>(Target);

        // Put a floor tile down
        await InteractUsing(FloorTileId);

        // Power the reclaimer
        var apc = await SpawnEntity(ApcId, SEntMan.GetCoordinates(TargetCoords)); // Arcane-Edit
        await RunTicks(1);
        // Set reclaimer to enabled
        await Server.WaitPost(() =>
        {
            _materialReclaimerSystem.SetReclaimerEnabled((EntityUid)STarget, true);
        });

        // Check that reclaimer enabled
        Assert.That(reclaimComp.Enabled, "The reclaimer did not get or stay enabled"); // Arcane-Edit

        /* // Arcane-Edit-Start
        // Reclaimer can't reclaim materials? Job's done.
        if (!reclaimComp.ReclaimMaterials)
            Assert.Ignore("Cannot reclaim materials");
        */ // Arcane-Edit-End

        using (Assert.EnterMultipleScope())
        {
            // For each material, assert that it is not recyclable (and would thus cause a recycling loop)
            foreach (var material in ProtoMan.EnumeratePrototypes<MaterialPrototype>())
            {
                var matStack = material.StackEntity;
                Assert.That(
                    matStack,
                    Is.Not.Null,
                    $"The material, {material.ID}, did not have a stackentity associated with it. You may need to add a stackEntity to its Reagents/Materials yml file.");

                var matInHands = await PlaceInHands(matStack);
                var matInHandsUid = ToServer(matInHands);

                // Assert we're holding material
                Assert.That(
                    HandSys.GetActiveItem((SPlayer, Hands)),
                    Is.EqualTo(matInHandsUid),
                    $"The material, {matStack}, never got put in our hands.");

                await Interact();

                // Assert Hands not empty
                Assert.That(
                    HandSys.GetActiveItem((SPlayer, Hands)),
                    Is.Not.Null,
                    $"The material that should not have been reclaimed, {matStack}, is no longer in our hands. The reclaimer was {reclaimerId}");
            }
        }
        return apc; // Arcane
    }
}
