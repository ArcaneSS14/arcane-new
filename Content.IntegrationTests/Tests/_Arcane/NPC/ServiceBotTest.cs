#nullable enable
using System.Linq;
using System.Threading;
using Content.Arcane.Server.NPC.HTN.PrimitiveTasks.Operators;
using Content.Arcane.Server.Silicon.Bots;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.HTN.PrimitiveTasks.Operators;
using Content.Server.NPC.Systems;
using Content.Server.Power.Components;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Disposal.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Trauma.Common.CCVar;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Arcane.NPC;

public sealed class ServiceBotTest : GameTest
{
    [Test]
    public async Task RelinkingDropsHeldItemsAndCannotDeliverToAnOldOrRemoteReceiver()
    {
        await PrepareFloor();
        await Server.WaitAssertion(() =>
        {
            var bot = SpawnBot("MobFillBot");
            var first = SpawnAt("Autolathe", 1, 0);
            var second = SpawnAt("Autolathe", 4, 0);
            SComp<ApcPowerReceiverComponent>(first).Powered = true;
            SComp<ApcPowerReceiverComponent>(second).Powered = true;
            var sheet = SpawnAt("SheetSteel1", 0.25f, 0);
            var bots = SEntMan.System<FillbotSystem>();
            var hands = SEntMan.System<SharedHandsSystem>();
            var links = SEntMan.System<SharedDeviceLinkSystem>();
            Link(bot, first);
            Assert.That(bots.TryPickup(bot, sheet, first), Is.True);

            Link(bot, second);
            Assert.That(bots.TryGetSink(bot, out var sink), Is.True);
            Assert.That(sink, Is.EqualTo(second));
            Assert.That(links.GetLinkedSinks(bot, FillbotSystem.SourcePort), Does.Not.Contain(first));
            Assert.That(hands.GetActiveItem(bot), Is.Null);
            Assert.That(bots.TryDeliver(bot, sheet, first), Is.False);
            Assert.That(bots.TryPickup(bot, sheet, second), Is.True);
            Assert.That(bots.TryDeliver(bot, sheet, second), Is.False,
                "A valid link does not authorize loading from outside interaction range.");

            links.RemoveSinkFromSource(bot, second);
            Assert.That(hands.GetActiveItem(bot), Is.Null);
            Assert.That(bots.TryGetSink(bot, out _), Is.False);
            Assert.That(SComp<HTNComponent>(bot).Enabled, Is.False,
                "Changing links must not re-enable an independently disabled HTN.");
        });
    }

    [Test]
    public async Task PlantbotWatersWeedsAndUsesPersistentEmagState()
    {
        await PrepareFloor();
        await Server.WaitAssertion(() =>
        {
            var bot = SpawnBot("MobPlantbot");
            var soil = SpawnAt("hydroponicsSoil", 1, 0);
            var tray = SComp<PlantTrayComponent>(soil);
            var trays = SEntMan.System<PlantTraySystem>();
            var bots = SEntMan.System<PlantbotServiceSystem>();
            trays.AdjustWater((soil, tray), -tray.MaxWaterLevel);

            for (var i = 0; i < 8; i++)
                Assert.That(bots.TryService(bot, soil), Is.True);
            Assert.That(tray.WaterLevel, Is.EqualTo(80));
            Assert.That(bots.TryService(bot, soil), Is.False);

            trays.AdjustWeed((soil, tray), 3);
            Assert.That(bots.TryService(bot, soil), Is.True);
            Assert.That(tray.WeedLevel, Is.EqualTo(2));

            var user = SpawnAt("MobHuman", 0, 1);
            var emag = SpawnAt("EmagUnlimited", 0, 1);
            var emags = SEntMan.System<EmagSystem>();
            Assert.That(emags.TryEmagEffect(emag, user, bot), Is.True);
            Assert.That(emags.CheckFlag(bot, EmagType.Interaction), Is.True);
            Assert.That(bots.TryService(bot, soil), Is.True);
            Assert.That(tray.WaterLevel, Is.EqualTo(70));
            Assert.That(tray.WeedLevel, Is.EqualTo(2), "Sabotage drains water instead of weeding.");

            trays.AdjustWater((soil, tray), -tray.MaxWaterLevel);
            Assert.That(bots.TryService(bot, soil), Is.False);
            Assert.That(tray.WaterLevel, Is.Zero);
            SEntMan.DeleteEntity(soil);
            Assert.That(bots.TryService(bot, soil), Is.False);
        });
    }

    [Test]
    public async Task PlantbotTriesOtherTraysAfterAccessFailures()
    {
        await OverrideCVar(Side.Server, TraumaCVars.DisablePathfinding, false);
        await PrepareFloor();
        NPCBlackboard blackboard = null!;
        PickNearbyServicableHydroponicsTrayOperator picker = null!;
        PlantbotServiceOperator service = null!;
        EntityUid first = default, second = default, fallback = default;
        await Server.WaitAssertion(() =>
        {
            var bot = SpawnAt("MobPlantbot", 0.5f, 0.5f);
            var htn = SComp<HTNComponent>(bot);
            SEntMan.System<HTNSystem>().SetHTNEnabled((bot, htn), false);
            blackboard = htn.Blackboard;

            var prototypes = Server.ResolveDependency<IPrototypeManager>();
            var root = prototypes.Index(htn.RootTask.Task);
            var operators = root.Branches
                .SelectMany(branch => branch.Tasks.OfType<HTNCompoundTask>())
                .SelectMany(task => prototypes.Index(task.Task).Branches)
                .SelectMany(branch => branch.Tasks.OfType<HTNPrimitiveTask>())
                .Select(task => task.Operator)
                .ToArray();
            picker = operators.OfType<PickNearbyServicableHydroponicsTrayOperator>().Single();
            service = operators.OfType<PlantbotServiceOperator>().Single();

            var window = SpawnAt("WindowDirectional", 0.5f, 1.5f);
            first = SpawnAt("hydroponicsSoil", 0.45f, 1.5f);
            second = SpawnAt("hydroponicsSoil", 0.55f, 1.5f);
            foreach (var tray in new[] { first, second })
            {
                SetWater(tray, 0);
                Assert.That(SEntMan.System<PlantbotServiceSystem>().CanService(bot, tray), Is.True);
                blackboard.SetValue(service.TargetKey, tray);
                var status = service.Update(blackboard, 0);
                Assert.That(status, Is.EqualTo(HTNOperatorStatus.Failed));
                service.TaskShutdown(blackboard, status);
            }

            SEntMan.DeleteEntity(window);
            fallback = SpawnAt("hydroponicsSoil", 1.5f, 0.5f);
            SetWater(fallback, SComp<PlantTrayComponent>(fallback).MaxWaterLevel);
            Assert.That(SEntMan.System<SharedInteractionSystem>().InRangeAndAccessible(bot, fallback), Is.True);
        });

        // Both eligible trays were rejected during execution. Neither should be chosen again yet.
        Assert.That(await Pick(), Is.Null);
        await Server.WaitPost(() => SetWater(fallback, 0));
        Assert.That(await Pick(), Is.EqualTo(fallback));

        await Server.WaitAssertion(() =>
        {
            Assert.That(SComp<PlantTrayComponent>(first).WaterLevel, Is.Zero);
            Assert.That(SComp<PlantTrayComponent>(second).WaterLevel, Is.Zero);
            SetWater(fallback, SComp<PlantTrayComponent>(fallback).MaxWaterLevel);
            SetWater(second, SComp<PlantTrayComponent>(second).MaxWaterLevel);
        });
        await RunSeconds(11f);
        Assert.That(await Pick(), Is.EqualTo(first));

        void SetWater(EntityUid entity, float amount)
        {
            var tray = SComp<PlantTrayComponent>(entity);
            SEntMan.System<PlantTraySystem>().AdjustWater((entity, tray), amount - tray.WaterLevel);
        }

        async Task<EntityUid?> Pick()
        {
            Task<(bool Valid, Dictionary<string, object>? Effects)> plan = null!;
            await Server.WaitPost(() =>
            {
                plan = picker.Plan(blackboard.ShallowClone(), CancellationToken.None);
            });
            for (var i = 0; i < 12 && !plan.IsCompleted; i++)
                await RunSeconds(0.25f);

            Assert.That(plan.IsCompleted, Is.True, "The planner did not finish within three simulated seconds.");
            var result = await plan;
            return result.Valid ? (EntityUid) result.Effects![picker.TargetKey] : null;
        }
    }

    private EntityUid SpawnAt(string prototype, float x, float y)
        => SSpawnAtPosition(prototype, new EntityCoordinates(Pair.TestMap!.Grid, x, y));

    private EntityUid SpawnBot(string prototype)
    {
        var bot = SpawnAt(prototype, 0, 0);
        SEntMan.System<HTNSystem>().SetHTNEnabled((bot, SComp<HTNComponent>(bot)), false);
        return bot;
    }

    private void Link(EntityUid bot, EntityUid sink)
    {
        Assert.That(SEntMan.System<SharedDeviceLinkSystem>().ToggleLink(null, bot, sink,
            FillbotSystem.SourcePort, FillbotSystem.SinkPort), Is.True);
    }

    private async Task PrepareFloor()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var maps = SEntMan.System<SharedMapSystem>();
            for (var x = -1; x <= 5; x++)
            for (var y = -1; y <= 1; y++)
                maps.SetTile(map.Grid.Owner, map.Grid.Comp, new EntityCoordinates(map.Grid, x, y), map.Tile.Tile);
        });
    }
}
