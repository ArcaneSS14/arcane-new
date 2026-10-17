using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Chemistry;

public sealed class ReagentDispenserTest : GameTest
{
    private const string Water = "Water";

    [SidedDependency(Side.Server)]
    private readonly ItemSlotsSystem _itemSlots = default!;

    [SidedDependency(Side.Server)]
    private readonly SharedSolutionContainerSystem _solutions = default!;

    [Test]
    public async Task MaxDispensesMoreThanOneThousandUnits()
    {
        var fixture = await CreateFixture(FixedPoint2.New(1500.25), FixedPoint2.New(2000.5));

        await Dispense(fixture, ReagentDispenserDispenseAmount.Max);
        var (sourceVolume, targetVolume) = await ReadVolumes(fixture);

        Assert.That(targetVolume, Is.EqualTo(FixedPoint2.New(1500.25)));
        Assert.That(sourceVolume, Is.EqualTo(FixedPoint2.Zero));
        Assert.That(sourceVolume + targetVolume, Is.EqualTo(FixedPoint2.New(1500.25)));
    }

    [Test]
    public async Task MaxRespectsFractionalSpaceAndRepeatedDispensePreservesVolume()
    {
        var fixture = await CreateFixture(FixedPoint2.New(10.75), FixedPoint2.New(15.25), FixedPoint2.New(5));

        await Dispense(fixture, ReagentDispenserDispenseAmount.Max);
        var afterFirstClick = await ReadVolumes(fixture);

        Assert.That(afterFirstClick.Source, Is.EqualTo(FixedPoint2.New(0.5)));
        Assert.That(afterFirstClick.Target, Is.EqualTo(FixedPoint2.New(15.25)));
        Assert.That(afterFirstClick.Source + afterFirstClick.Target, Is.EqualTo(FixedPoint2.New(15.75)));

        await Dispense(fixture, ReagentDispenserDispenseAmount.Max);
        var afterSecondClick = await ReadVolumes(fixture);

        Assert.That(afterSecondClick, Is.EqualTo(afterFirstClick));
    }

    [Test]
    public async Task InvalidAmountDoesNotReplacePreviouslySelectedAmount()
    {
        var fixture = await CreateFixture(FixedPoint2.New(100), FixedPoint2.New(100));

        await Server.WaitPost(() =>
        {
            var selected = new ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount.U25);
            SEntMan.EventBus.RaiseLocalEvent(fixture.Machine, selected);

            var invalid = new ReagentDispenserSetDispenseAmountMessage(unchecked((ReagentDispenserDispenseAmount)(-1)));
            SEntMan.EventBus.RaiseLocalEvent(fixture.Machine, invalid);

            var dispense = new ReagentDispenserDispenseReagentMessage(fixture.StorageLocation);
            SEntMan.EventBus.RaiseLocalEvent(fixture.Machine, dispense);
        });

        var (sourceVolume, targetVolume) = await ReadVolumes(fixture);
        Assert.That(targetVolume, Is.EqualTo(FixedPoint2.New(25)));
        Assert.That(sourceVolume, Is.EqualTo(FixedPoint2.New(75)));
    }

    private async Task<DispenserFixture> CreateFixture(
        FixedPoint2 sourceVolume,
        FixedPoint2 outputCapacity,
        FixedPoint2 outputInitialVolume = default)
    {
        var map = await Pair.CreateTestMap();
        var fixture = default(DispenserFixture);

        await Server.WaitPost(() =>
        {
            var machine = SSpawnAtPosition("ChemDispenser", map.GridCoords);
            var beaker = SSpawnAtPosition("LargeBeaker", map.GridCoords);
            Assert.That(_itemSlots.TryInsert(machine, SharedReagentDispenser.OutputSlotName, beaker, null), Is.True);

            var storage = SComp<StorageComponent>(machine);
            Assert.That(storage.StoredItems, Is.Not.Empty);
            var source = storage.StoredItems.Keys.First();
            var storageLocation = storage.StoredItems[source];

            Assert.That(_solutions.TryGetDrainableSolution(source, out var sourceSolutionEntity, out _), Is.True);
            var sourceSolution = sourceSolutionEntity!.Value;
            _solutions.SetCapacity(sourceSolution, sourceVolume);
            _solutions.RemoveAllSolution(sourceSolution);
            Assert.That(_solutions.TryAddSolution(sourceSolution, new Solution(Water, sourceVolume)), Is.True);

            Assert.That(_solutions.TryGetRefillableSolution(beaker, out var targetSolutionEntity, out _), Is.True);
            var targetSolution = targetSolutionEntity!.Value;
            _solutions.SetCapacity(targetSolution, outputCapacity);
            _solutions.RemoveAllSolution(targetSolution);
            if (outputInitialVolume > FixedPoint2.Zero)
                Assert.That(_solutions.TryAddSolution(targetSolution, new Solution(Water, outputInitialVolume)), Is.True);

            fixture = new DispenserFixture(machine, source, beaker, storageLocation);
        });

        return fixture;
    }

    private Task Dispense(DispenserFixture fixture, ReagentDispenserDispenseAmount amount)
    {
        return Server.WaitPost(() =>
        {
            var selected = new ReagentDispenserSetDispenseAmountMessage(amount);
            SEntMan.EventBus.RaiseLocalEvent(fixture.Machine, selected);

            var dispense = new ReagentDispenserDispenseReagentMessage(fixture.StorageLocation);
            SEntMan.EventBus.RaiseLocalEvent(fixture.Machine, dispense);
        });
    }

    private async Task<(FixedPoint2 Source, FixedPoint2 Target)> ReadVolumes(DispenserFixture fixture)
    {
        var volumes = default((FixedPoint2 Source, FixedPoint2 Target));

        await Server.WaitPost(() =>
        {
            Assert.That(_solutions.TryGetDrainableSolution(fixture.Source, out _, out var source), Is.True);
            Assert.That(_solutions.TryGetRefillableSolution(fixture.Beaker, out _, out var target), Is.True);
            volumes = (source!.Volume, target!.Volume);
        });

        return volumes;
    }

    private readonly record struct DispenserFixture(
        EntityUid Machine,
        EntityUid Source,
        EntityUid Beaker,
        ItemStorageLocation StorageLocation);
}
