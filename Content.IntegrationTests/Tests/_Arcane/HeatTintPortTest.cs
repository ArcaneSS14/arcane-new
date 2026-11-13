#nullable enable
using Content.Arcane.Shared.HeatTint;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class HeatTintPortTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [TestCase("LargeBeaker", "beaker")]
    [TestCase("ChemistryEmptyVial", "drink")]
    public async Task LiquidTintPreservesContainerAndTracksReagentRecoloring(string prototype, string solutionName)
    {
        var map = await Pair.CreateTestMap();
        EntityUid container = default;
        NetEntity net = default;
        Color liquidBase = default;
        await Server.WaitAssertion(() =>
        {
            container = SSpawnAtPosition(prototype, map.GridCoords);
            net = SEntMan.GetNetEntity(container);
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            Assert.That(solutions.TryGetSolution(container, solutionName, out var solution, out _), Is.True);
            Assert.That(solutions.TryAddReagent(solution!.Value, "Water", FixedPoint2.New(5)), Is.True);
            solutions.SetTemperature(solution.Value, 293);
        });
        await Pair.RunUntilSynced();
        var containerBase = Color.FromHex("#778899");
        await Client.WaitPost(() => CEntMan.System<SpriteSystem>().SetColor(CEntMan.GetEntity(net), containerBase));
        await ChangeLiquid(false);
        await AssertLiquid();
        await ChangeLiquid(true);
        await AssertLiquid();

        async Task ChangeLiquid(bool mix)
        {
            await Server.WaitAssertion(() =>
            {
                var solutions = SEntMan.System<SharedSolutionContainerSystem>();
                Assert.That(solutions.TryGetSolution(container, solutionName, out var solution, out var contents), Is.True);
                if (mix)
                    Assert.That(solutions.TryAddReagent(solution!.Value, "Milk", FixedPoint2.New(5), temperature: 350), Is.True);
                solutions.SetTemperature(solution!.Value, 350);
                liquidBase = contents!.GetColor(Server.ResolveDependency<IPrototypeManager>());
            });
            await Pair.RunUntilSynced();
        }

        async Task AssertLiquid() => await Client.WaitAssertion(() =>
        {
            var uid = CEntMan.GetEntity(net);
            var sprite = CEntMan.GetComponent<SpriteComponent>(uid);
            var sprites = CEntMan.System<SpriteSystem>();
            Assert.That(sprites.LayerMapTryGet((uid, sprite), SolutionContainerLayers.Fill, out var layer, false), Is.True);
            AssertColor(sprite[layer].Color, liquidBase * Color.FromHex("#FFC0A8"));
            Assert.That(sprite.Color, Is.EqualTo(containerBase), "Only the liquid receives the temperature tint.");
        });
    }

    [Test]
    public async Task TemperatureTintPreservesIndependentBaseColorsAndRestoresOnRemoval()
    {
        var map = await Pair.CreateTestMap();
        EntityUid first = default;
        EntityUid second = default;
        NetEntity firstNet = default;
        NetEntity secondNet = default;
        await Server.WaitPost(() =>
        {
            first = SSpawnAtPosition("FoodBreadBun", map.GridCoords);
            second = SSpawnAtPosition("FoodBreadBun", map.GridCoords);
            firstNet = SEntMan.GetNetEntity(first);
            secondNet = SEntMan.GetNetEntity(second);
        });
        await Pair.RunUntilSynced();
        var firstBase = Color.FromHex("#80C0FF");
        var secondBase = Color.FromHex("#FF8040");
        await Client.WaitPost(() =>
        {
            var sprites = CEntMan.System<SpriteSystem>();
            sprites.SetColor(CEntMan.GetEntity(firstNet), firstBase);
            sprites.SetColor(CEntMan.GetEntity(secondNet), secondBase);
        });

        await Heat(first, 350);
        await Heat(second, 293);
        await AssertColors();
        await Heat(first, 293);
        await Heat(first, 350);
        await AssertColors();

        await Client.WaitAssertion(() =>
        {
            var uid = CEntMan.GetEntity(firstNet);
            var heat = CEntMan.GetComponent<HeatTintComponent>(uid);
            CEntMan.RemoveComponent(uid, heat);
            Assert.That(CEntMan.GetComponent<SpriteComponent>(uid).Color, Is.EqualTo(firstBase),
                "Removing the effect must restore this entity's original tint.");
        });

        async Task Heat(EntityUid uid, float temperature)
        {
            await Server.WaitAssertion(() =>
            {
                var solutions = SEntMan.System<SharedSolutionContainerSystem>();
                Assert.That(solutions.TryGetSolution(uid, "food", out var solution, out _), Is.True);
                solutions.SetTemperature(solution!.Value, temperature);
            });
            await Pair.RunUntilSynced();
        }

        async Task AssertColors()
        {
            await Server.WaitAssertion(() =>
            {
                Assert.That(SEntMan.System<SharedAppearanceSystem>().TryGetData<float>(first,
                    HeatTintVisuals.Temperature, out var temperature), Is.True);
                Assert.That(SEntMan.System<SharedSolutionContainerSystem>().TryGetSolution(first,
                    "food", out _, out var solution), Is.True);
                Assert.That(temperature, Is.EqualTo(solution!.Temperature),
                    "Cooling the item must not replace the temperature of its independent food solution.");
            });
            await Client.WaitAssertion(() =>
            {
                var firstUid = CEntMan.GetEntity(firstNet);
                var secondUid = CEntMan.GetEntity(secondNet);
                var firstColor = CEntMan.GetComponent<SpriteComponent>(firstUid).Color;
                var secondColor = CEntMan.GetComponent<SpriteComponent>(secondUid).Color;
                AssertColor(firstColor, firstBase * Color.FromHex("#FF6600"));
                AssertColor(secondColor, secondBase);
            });
        }
    }

    private static void AssertColor(Color actual, Color expected)
    {
        // OkLab round trips introduce a small floating-point error at gradient stops.
        Assert.That(actual.R, Is.EqualTo(expected.R).Within(0.002));
        Assert.That(actual.G, Is.EqualTo(expected.G).Within(0.002));
        Assert.That(actual.B, Is.EqualTo(expected.B).Within(0.002));
        Assert.That(actual.A, Is.EqualTo(expected.A).Within(0.002));
    }
}
