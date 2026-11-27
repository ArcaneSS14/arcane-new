// Arcane-Start: ordinary transfers respect the finite buffer and update its appearance
using Content.IntegrationTests.Fixtures;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;

namespace Content.IntegrationTests.Tests._Arcane.Chemistry;

[TestFixture]
public sealed class ChemMasterCapacityTest : GameTest
{
    [Test]
    public async Task AddingReagentsDoesNotTriggerBufferReactions()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var machine = SSpawnAtPosition("ChemMaster", map.GridCoords);
            var beaker = SSpawnAtPosition("LargeBeaker", map.GridCoords);
            var actor = SSpawnAtPosition("MobHuman", map.GridCoords);
            var solutions = Server.System<SharedSolutionContainerSystem>();
            Assert.That(Server.System<ItemSlotsSystem>().TryInsert(machine, SharedChemMaster.InputSlotName, beaker, null), Is.True);
            Assert.That(solutions.TryGetSolution(machine, SharedChemMaster.BufferSolutionName, out var bufferEntity, out var buffer), Is.True);
            Assert.That(solutions.TryGetFitsInDispenser(beaker, out var inputEntity, out _), Is.True);
            Assert.That(solutions.TryAddReagent(bufferEntity!.Value, "Ammonia", FixedPoint2.New(1)), Is.True);
            Assert.That(solutions.TryAddReagent(inputEntity!.Value, "Ethanol", FixedPoint2.New(1)), Is.True);
            var message = new ChemMasterReagentAmountButtonMessage(new ReagentId("Ethanol", null), ChemMasterReagentAmount.U1, false)
            {
                Actor = actor,
                UiKey = ChemMasterUiKey.Key,
            };
            SEntMan.EventBus.RaiseLocalEvent(machine, message);
            Assert.That(buffer!.GetReagentQuantity(new ReagentId("Ammonia", null)), Is.EqualTo(FixedPoint2.New(1)));
            Assert.That(buffer.GetReagentQuantity(new ReagentId("Ethanol", null)), Is.EqualTo(FixedPoint2.New(1)));
            Assert.That(buffer.GetReagentQuantity(new ReagentId("Diethylamine", null)), Is.EqualTo(FixedPoint2.Zero));
            buffer.RemoveReagent(new ReagentId("Ammonia", null), FixedPoint2.New(1));
            buffer.RemoveReagent(new ReagentId("Ethanol", null), FixedPoint2.New(1));
            solutions.UpdateChemicals(bufferEntity.Value, needsReactionsProcessing: false);
        });
    }

    [Test]
    public async Task TransferRespectsRemainingCapacityAndUpdatesFill()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var machine = SSpawnAtPosition("ChemMaster", map.GridCoords);
            var beaker = SSpawnAtPosition("LargeBeaker", map.GridCoords);
            var actor = SSpawnAtPosition("MobHuman", map.GridCoords);
            var slots = Server.System<ItemSlotsSystem>();
            var solutions = Server.System<SharedSolutionContainerSystem>();
            Assert.That(slots.TryInsert(machine, SharedChemMaster.InputSlotName, beaker, null), Is.True);
            Assert.That(solutions.TryGetSolution(machine, SharedChemMaster.BufferSolutionName, out var bufferEntity, out var buffer), Is.True);
            Assert.That(solutions.TryGetFitsInDispenser(beaker, out var inputEntity, out var input), Is.True);
            solutions.SetCapacity(bufferEntity!.Value, FixedPoint2.New(20));
            solutions.SetCapacity(inputEntity!.Value, FixedPoint2.New(20));
            Assert.That(solutions.TryAddReagent(bufferEntity.Value, "Water", FixedPoint2.New(15)), Is.True);
            Assert.That(solutions.TryAddReagent(inputEntity.Value, "Water", FixedPoint2.New(10)), Is.True);
            var water = new ReagentId("Water", null);

            void Transfer(ChemMasterReagentAmount amount, bool fromBuffer)
            {
                var message = new ChemMasterReagentAmountButtonMessage(water, amount, fromBuffer)
                {
                    Actor = actor,
                    UiKey = ChemMasterUiKey.Key,
                };
                SEntMan.EventBus.RaiseLocalEvent(machine, message);
            }

            void AssertFill(float expected)
            {
                Assert.That(Server.System<SharedAppearanceSystem>().TryGetData<float>(machine, SolutionContainerVisuals.FillFraction, out var fill), Is.True);
                Assert.That(fill, Is.EqualTo(expected).Within(0.001f));
            }

            Transfer(ChemMasterReagentAmount.U10, false);
            Assert.That(buffer!.Volume, Is.EqualTo(FixedPoint2.New(20)));
            Assert.That(input!.Volume, Is.EqualTo(FixedPoint2.New(5)));
            AssertFill(1f);
            Transfer(ChemMasterReagentAmount.U10, false);
            Assert.That(input.Volume, Is.EqualTo(FixedPoint2.New(5)));
            Transfer(ChemMasterReagentAmount.U5, true);
            Assert.That(buffer.Volume, Is.EqualTo(FixedPoint2.New(15)));
            Assert.That(input.Volume, Is.EqualTo(FixedPoint2.New(10)));
            AssertFill(0.75f);
            SEntMan.EventBus.RaiseLocalEvent(machine, new ChemMasterSetModeMessage(ChemMasterMode.Discard) { Actor = actor, UiKey = ChemMasterUiKey.Key });
            Transfer(ChemMasterReagentAmount.U5, true);
            Assert.That(buffer.Volume, Is.EqualTo(FixedPoint2.New(10)));
            AssertFill(0.5f);
        });
    }
}
// Arcane-End
