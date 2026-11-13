#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class SmokingPortTest : InteractionTest
{
    [TestCase("ECigarette", true)]
    [TestCase("CryostasisCigarette", true)]
    [TestCase("Cigarette", false)]
    public async Task ActivationRelightsOnlyReusableCigarettes(string prototype, bool reusable)
    {
        await SpawnTarget(prototype);
        await Activate();
        await AssertState(reusable ? SmokableState.Lit : SmokableState.Unlit);
        if (!reusable)
            await Server.WaitPost(() => SEntMan.System<SmokingSystem>().SetSmokableState(STarget!.Value, SmokableState.Lit));
        await Activate();
        await AssertState(SmokableState.Burnt);
        await Activate();
        await AssertState(reusable ? SmokableState.Lit : SmokableState.Burnt);
        await Pair.RunUntilSynced();
    }

    [Test]
    public async Task GrinderSolutionDoesNotReplaceSyndicateSmokingSolution()
    {
        await SpawnTarget("CigaretteSyndicate");
        await Server.WaitAssertion(() =>
        {
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            var extractor = SEntMan.GetComponent<ExtractableComponent>(STarget!.Value);
            Assert.That(solutions.TryGetSolution(STarget.Value, "smokable", out _, out var smoke), Is.True);
            Assert.That(solutions.TryGetSolution(STarget.Value, extractor.GrindableSolutionName!, out _, out var ground), Is.True);
            Assert.That(smoke!.GetTotalPrototypeQuantity("Omnizine"), Is.EqualTo(FixedPoint2.New(20)));
            Assert.That(ground!.GetTotalPrototypeQuantity("Omnizine"), Is.EqualTo(FixedPoint2.New(5)));
            Assert.That(ground.GetTotalPrototypeQuantity("Nicotine"), Is.EqualTo(FixedPoint2.New(10)));
        });
    }

    private async Task AssertState(SmokableState state) => await Server.WaitAssertion(() =>
        Assert.That(SEntMan.GetComponent<SmokableComponent>(STarget!.Value).State, Is.EqualTo(state)));
}
