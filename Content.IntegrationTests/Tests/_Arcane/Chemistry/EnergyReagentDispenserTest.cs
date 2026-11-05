#nullable enable
using System.Linq;
using Content.Goobstation.Shared.Chemistry;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane.Chemistry;

[TestOf(typeof(EnergyReagentDispenserSystem))]
public sealed class EnergyReagentDispenserTest : GameTest
{
    private const string Dispenser = "EnergyChemDispenser";
    private const string Beaker = "LargeBeaker";
    private const string User = "MobHuman";
    private const string Unlisted = "Omnizine";

    [SidedDependency(Side.Server)] private readonly ItemSlotsSystem _itemSlots = default!;
    [SidedDependency(Side.Server)] private readonly SharedBatterySystem _battery = default!;
    [SidedDependency(Side.Server)] private readonly SharedSolutionContainerSystem _solutions = default!;

    [Test, Description("A forged UI message must not get a reagent outside the machine's list for free.")]
    public async Task OnlyListedReagentsAreDispensed()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var machine = SSpawnAtPosition(Dispenser, map.GridCoords);
            var beaker = SSpawnAtPosition(Beaker, map.GridCoords);
            var user = SSpawnAtPosition(User, map.GridCoords);
            var comp = SComp<EnergyReagentDispenserComponent>(machine);
            IReadOnlyDictionary<ProtoId<ReagentPrototype>, int> reagents = comp.Reagents;

            Assert.That(reagents.ContainsKey(Unlisted), Is.False);
            Assert.That(_itemSlots.TryInsert(machine, comp.OutputSlotName, beaker, null), Is.True);
            var chargeBefore = _battery.GetCharge(machine);

            Dispense(machine, user, Unlisted);
            Assert.That(Volume(beaker), Is.EqualTo(FixedPoint2.Zero), "An unlisted reagent was dispensed.");

            Dispense(machine, user, reagents.Keys.First());
            Assert.That(Volume(beaker), Is.GreaterThan(FixedPoint2.Zero), "A listed reagent was not dispensed.");
            Assert.That(_battery.GetCharge(machine), Is.LessThan(chargeBefore), "A listed reagent cost no energy.");
        });
    }

    private void Dispense(EntityUid machine, EntityUid user, ProtoId<ReagentPrototype> reagent)
    {
        SEntMan.EventBus.RaiseLocalEvent(machine,
            new EnergyReagentDispenserDispenseReagentMessage(reagent)
            {
                Actor = user,
                UiKey = EnergyReagentDispenserUiKey.Key,
            });
    }

    private FixedPoint2 Volume(EntityUid beaker)
    {
        Assert.That(_solutions.TryGetFitsInDispenser(beaker, out _, out var solution), Is.True);
        return solution!.Volume;
    }
}
