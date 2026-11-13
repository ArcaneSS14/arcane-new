#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Body;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Metabolism;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Arcane.Chemistry;

public sealed class CaffeineMetabolismTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [Test]
    public async Task DrinksShareCaffeineWithoutLosingOtherMetabolites()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var human = SSpawnAtPosition("MobHuman", map.GridCoords);
            var stomach = PrepareMetabolizer(human, "Digestion");
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            var stage = stomach.Comp.Solutions["Digestion"];
            Assert.That(solutions.TryGetSolution(stomach.Owner, stage.SolutionName, out var sourceEntity, out var source), Is.True);
            Assert.That(solutions.TryGetSolution(human, stage.TransferSolutionName!, out _, out var blood), Is.True);
            Assert.That(solutions.TryAddReagent(sourceEntity!.Value, "Coffee", FixedPoint2.New(1)), Is.True);
            Assert.That(solutions.TryAddReagent(sourceEntity.Value, "EnergyDrink", FixedPoint2.New(1)), Is.True);
            Assert.That(solutions.TryAddReagent(sourceEntity.Value, "CoffeeLiqueur", FixedPoint2.New(1)), Is.True);

            SEntMan.System<MetabolizerSystem>().Update(0);

            Assert.That(source!.GetTotalPrototypeQuantity("Coffee"), Is.EqualTo(FixedPoint2.New(0.5)));
            Assert.That(source.GetTotalPrototypeQuantity("EnergyDrink"), Is.EqualTo(FixedPoint2.New(0.5)));
            Assert.That(source.GetTotalPrototypeQuantity("CoffeeLiqueur"), Is.EqualTo(FixedPoint2.New(0.5)));
            Assert.That(blood!.GetTotalPrototypeQuantity("Caffeine"), Is.EqualTo(FixedPoint2.New(1.7)),
                "All three drinks must feed one caffeine pool at Goob's per-cycle amounts.");
            Assert.That(blood.GetTotalPrototypeQuantity("Theobromine"), Is.EqualTo(FixedPoint2.New(0.2)));
            Assert.That(blood.GetTotalPrototypeQuantity("Ethanol"), Is.EqualTo(FixedPoint2.New(0.06)),
                "Adding a drink-specific metabolism must not discard its inherited alcohol.");
        });
    }

    [TestCase(10, 1.1f, false)]
    [TestCase(60, 0.7f, false)]
    [TestCase(10, 1.1f, true)]
    public async Task CaffeineRushAndOverdoseUseOneSpeedModifier(int amount, float multiplier, bool otherStimulant)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var human = SSpawnAtPosition("MobHuman", map.GridCoords);
            var metabolizer = PrepareMetabolizer(human, "Bloodstream");
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            Assert.That(solutions.TryGetSolution(human, metabolizer.Comp.Solutions["Bloodstream"].SolutionName,
                out var bloodEntity, out _), Is.True);
            Assert.That(solutions.TryAddReagent(bloodEntity!.Value, "Caffeine", FixedPoint2.New(amount)), Is.True);
            var movement = SEntMan.System<MovementSpeedModifierSystem>();
            if (otherStimulant)
                Assert.That(SEntMan.System<MovementModStatusSystem>().TryUpdateMovementSpeedModDuration(
                    human, MovementModStatusSystem.ReagentSpeed, TimeSpan.FromSeconds(2), 1.2f), Is.True);
            movement.RefreshMovementSpeedModifiers(human);
            var speedBefore = SComp<MovementSpeedModifierComponent>(human).WalkSpeedModifier;

            SEntMan.System<MetabolizerSystem>().Update(0);
            movement.RefreshMovementSpeedModifiers(human);

            Assert.That(SComp<MovementSpeedModifierComponent>(human).WalkSpeedModifier,
                Is.EqualTo(speedBefore * multiplier).Within(0.001),
                "At the shared 60u boundary the overdose replaces the rush instead of multiplying both.");
        });
    }

    private Entity<MetabolizerComponent> PrepareMetabolizer(EntityUid human, string stage)
    {
        var now = Server.ResolveDependency<IGameTiming>().CurTime;
        Entity<MetabolizerComponent>? selected = null;
        var query = SEntMan.EntityQueryEnumerator<MetabolizerComponent, OrganComponent>();
        while (query.MoveNext(out var uid, out var metabolism, out var organ))
        {
            if (organ.Body != human)
                continue;
            // Isolate one real organ cycle; other organs must not consume the observed products.
            metabolism.NextUpdate = now + TimeSpan.FromDays(1);
            if (selected == null && metabolism.Stages.Contains(stage))
                selected = (uid, metabolism);
        }

        Assert.That(selected, Is.Not.Null, $"No human organ handles {stage}.");
        var organMetabolism = selected!.Value;
        organMetabolism.Comp.NextUpdate = now;
        return organMetabolism;
    }
}
