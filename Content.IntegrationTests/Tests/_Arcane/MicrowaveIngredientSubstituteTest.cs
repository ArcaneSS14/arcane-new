// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Kitchen.Components;
using Content.Server.Kitchen.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Containers;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class MicrowaveIngredientSubstituteTest : InteractionTest
{
    [Test]
    public async Task MixedBowlsCookTwoPortionsAndAreConsumedOnce()
    {
        EntityUid ordinary = default;
        EntityUid fancy = default;
        await SpawnTarget("KitchenMicrowave");
        var microwave = STarget!.Value;
        await Server.WaitPost(() => SEntMan.System<PowerReceiverSystem>().SetNeedsPower(microwave, false));
        await RunTicks(15);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<PowerReceiverSystem>().IsPowered(microwave), Is.True);
            var coordinates = Xform(microwave).Coordinates;
            ordinary = SEntMan.SpawnAttachedTo("FoodBowlBig", coordinates);
            fancy = SEntMan.SpawnAttachedTo("FoodBowlFancy", coordinates);
            var cooking = SEntMan.GetComponent<MicrowaveComponent>(microwave);
            var containers = SEntMan.System<SharedContainerSystem>();
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            foreach (var bowl in new[] { ordinary, fancy })
            {
                Assert.That(solutions.TryGetDrainableSolution(bowl, out var solution, out _), Is.True);
                Assert.That(solutions.TryAddReagent(solution!.Value, "Oats", 20), Is.True);
                Assert.That(solutions.TryAddReagent(solution.Value, "Water", 20), Is.True);
                Assert.That(containers.Insert(bowl, cooking.Storage), Is.True);
            }

            cooking.CurrentCookTimerTime = 30;
            cooking.CookTimeMultiplier = 0.01f;
            SEntMan.System<MicrowaveSystem>().Wzhzhzh(microwave, cooking, SPlayer);
            var (recipe, portions) = SEntMan.GetComponent<ActiveMicrowaveComponent>(microwave).PortionedRecipe;
            Assert.That(recipe?.ID, Is.EqualTo("RecipeOatmeal"));
            Assert.That(portions, Is.EqualTo(2));
        });

        await RunSeconds(0.5f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(ordinary), Is.False);
            Assert.That(SEntMan.EntityExists(fancy), Is.False);
            var meals = SEntMan.System<EntityLookupSystem>()
                .GetEntitiesInRange<MetaDataComponent>(Xform(microwave).Coordinates, 1f);
            Assert.That(meals.Count(entity => entity.Comp.EntityPrototype?.ID == "FoodOatmeal"), Is.EqualTo(2));
        });
    }

    // Arcane-Start
    [Test]
    public async Task NuggetRecipeRequiresFullFractionalIngredients()
    {
        await SpawnTarget("KitchenMicrowave");
        var microwave = STarget!.Value;
        await Server.WaitPost(() => SEntMan.System<PowerReceiverSystem>().SetNeedsPower(microwave, false));
        await RunTicks(15);

        await CookNuggets(microwave, drainDuringCooking: false);
        await CookNuggets(microwave, drainDuringCooking: true);
    }

    private async Task CookNuggets(EntityUid microwave, bool drainDuringCooking)
    {
        await Server.WaitAssertion(() =>
        {
            var bowl = SEntMan.SpawnAttachedTo("FoodBowlBig", Xform(microwave).Coordinates);
            var cooking = SEntMan.GetComponent<MicrowaveComponent>(microwave);
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            Assert.That(solutions.TryGetDrainableSolution(bowl, out var solution, out _), Is.True);
            Assert.That(solutions.TryAddReagent(solution!.Value, "UncookedAnimalProteins", 10), Is.True);
            Assert.That(solutions.TryAddReagent(solution.Value, "Fat", 10), Is.True);
            Assert.That(solutions.TryAddReagent(solution.Value, "Flour", 5), Is.True);
            Assert.That(solutions.TryAddReagent(solution.Value, "TableSalt", 5), Is.True);
            Assert.That(SEntMan.System<SharedContainerSystem>().Insert(bowl, cooking.Storage), Is.True);

            cooking.CurrentCookTimerTime = 25;
            cooking.CookTimeMultiplier = 0.01f;
            SEntMan.System<MicrowaveSystem>().Wzhzhzh(microwave, cooking, SPlayer);
            var (recipe, portions) = SEntMan.GetComponent<ActiveMicrowaveComponent>(microwave).PortionedRecipe;
            Assert.That(recipe?.ID, Is.EqualTo("RecipeNugget"));
            Assert.That(portions, Is.EqualTo(4));
            if (drainDuringCooking)
                solutions.RemoveReagent(solution.Value, "Fat", 1);
        });

        await RunSeconds(0.5f);
        await Server.WaitAssertion(() =>
        {
            var meals = SEntMan.System<EntityLookupSystem>()
                .GetEntitiesInRange<MetaDataComponent>(Xform(microwave).Coordinates, 1f);
            Assert.That(meals.Count(entity => entity.Comp.EntityPrototype?.ID == "FoodBakedNugget"), Is.EqualTo(4),
                drainDuringCooking ? "Draining ingredients mid-cycle must not produce another batch." : "Only four full portions fit.");
        });
    }
    // Arcane-End
}
