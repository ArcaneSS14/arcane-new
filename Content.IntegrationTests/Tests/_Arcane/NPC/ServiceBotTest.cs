#nullable enable
using System.Linq;
using Content.Arcane.Server.Silicon.Bots;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks.Operators;
using Content.Server.NPC.Systems;
using Content.Server.Power.Components;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Disposal.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands.EntitySystems;
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
    public async Task AutonomousBotsNavigateDeliverAndCancelInaccessibleService()
    {
        await OverrideCVar(Side.Server, TraumaCVars.DisablePathfinding, false);
        await PrepareFloor();
        EntityUid fillbot = default, machine = default, plantbot = default, soil = default, closet = default;
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<NPCSystem>().Enabled, Is.True);
            fillbot = SpawnAt("MobFillBot", 0, 0);
            machine = SpawnAt("Autolathe", 4, 0);
            SComp<ApcPowerReceiverComponent>(machine).NeedsPower = false;
            SComp<ApcPowerReceiverComponent>(machine).Powered = true;
            SpawnAt("SheetSteel1", 2, 0);
            Link(fillbot, machine);
        });
        await WaitFor(() => SEntMan.System<SharedMaterialStorageSystem>().GetMaterialAmount(machine, "Steel") == 100,
            "The autonomous Fillbot must navigate, pick up a sheet and load its linked machine.", fillbot);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<SharedHandsSystem>().GetActiveItem(fillbot), Is.Null);
            var sheet = SEntMan.SpawnAtPosition("SheetSteel1", SComp<TransformComponent>(fillbot).Coordinates);
            Assert.That(SEntMan.System<FillbotSystem>().TryPickup(fillbot, sheet, machine), Is.True);
            var locker = SEntMan.SpawnAtPosition("ClosetSteelBase", SComp<TransformComponent>(fillbot).Coordinates);
            var storage = SEntMan.System<SharedEntityStorageSystem>();
            storage.OpenStorage((locker, SComp<EntityStorageComponent>(locker)));
            storage.CloseStorage((locker, SComp<EntityStorageComponent>(locker)));
            Assert.That(SEntMan.System<SharedContainerSystem>().IsEntityInContainer(fillbot), Is.True);
            Assert.That(SEntMan.System<FillbotSystem>().TryDeliver(fillbot, sheet, machine), Is.False);
            Assert.That(SEntMan.System<SharedMaterialStorageSystem>().GetMaterialAmount(machine, "Steel"), Is.EqualTo(100));
            SEntMan.DeleteEntity(fillbot);
            SEntMan.DeleteEntity(machine);
            SEntMan.DeleteEntity(locker);
            plantbot = SpawnAt("MobPlantbot", 0, 0);
            soil = SpawnAt("hydroponicsSoil", 2, 0);
            var tray = SComp<PlantTrayComponent>(soil);
            SEntMan.System<PlantTraySystem>().AdjustWater((soil, tray), -tray.MaxWaterLevel);
        });
        await WaitFor(() => SComp<PlantTrayComponent>(soil).WaterLevel >= 10,
            "The autonomous Plantbot must navigate and complete its service wait.");
        await WaitFor(() => SComp<HTNComponent>(plantbot).Plan?.CurrentOperator is WaitOperator,
            "The next real service must reach the delayed execution boundary.");
        float water = 0;
        await Server.WaitAssertion(() =>
        {
            closet = SEntMan.SpawnAtPosition("ClosetSteelBase", SComp<TransformComponent>(plantbot).Coordinates);
            var storage = SEntMan.System<SharedEntityStorageSystem>();
            storage.OpenStorage((closet, SComp<EntityStorageComponent>(closet)));
            storage.CloseStorage((closet, SComp<EntityStorageComponent>(closet)));
            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.IsEntityInContainer(plantbot), Is.True);
            Assert.That(containers.IsEntityInContainer(soil), Is.False);
            water = SComp<PlantTrayComponent>(soil).WaterLevel;
        });
        await RunSeconds(4);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SComp<PlantTrayComponent>(soil).WaterLevel, Is.EqualTo(water));
            Assert.That(SEntMan.System<PlantbotServiceSystem>().TryService(plantbot, soil), Is.False);
            SEntMan.RemoveComponent<Content.Arcane.Shared.Silicon.Bots.PlantbotComponent>(plantbot);
            Assert.That(SComp<HTNComponent>(plantbot).Enabled, Is.False);
        });
    }

    [Test]
    public async Task FillbotLoadsMaterialsOnceAndRespectsPowerAndCapacity()
    {
        await PrepareFloor();
        await Server.WaitAssertion(() =>
        {
            var bot = SpawnBot("MobFillBot");
            var machine = SpawnAt("Autolathe", 1, 0);
            var sheet = SpawnAt("SheetSteel1", 0.25f, 0);
            var bots = SEntMan.System<FillbotSystem>();
            var materials = SEntMan.System<SharedMaterialStorageSystem>();
            var power = SComp<ApcPowerReceiverComponent>(machine);
            Link(bot, machine);

            power.Powered = false;
            Assert.That(bots.TryPickup(bot, sheet, machine), Is.False);
            power.Powered = true;
            materials.SetStorageLimit(machine, 50);
            Assert.That(bots.TryPickup(bot, sheet, machine), Is.False);
            materials.SetStorageLimit(machine, 1000);
            Assert.That(bots.TryPickup(bot, sheet, machine), Is.True);

            var before = materials.GetMaterialAmount(machine, "Steel");
            Assert.That(bots.TryDeliver(bot, sheet, machine), Is.True);
            Assert.That(materials.GetMaterialAmount(machine, "Steel"), Is.EqualTo(before + 100));
            Assert.That(bots.TryDeliver(bot, sheet, machine), Is.False,
                "A consumed item queued for deletion must not be inserted a second time.");
            Assert.That(materials.GetMaterialAmount(machine, "Steel"), Is.EqualTo(before + 100));
        });
    }

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
    public async Task FillbotPutsTrashIntoTheLinkedDisposal()
    {
        await PrepareFloor();
        await Server.WaitAssertion(() =>
        {
            var bot = SpawnBot("MobFillBot");
            var disposal = SpawnAt("DisposalUnit", 1, 0);
            var trash = SpawnAt("FoodPacketBoritosTrash", 0.25f, 0);
            var nonTrash = SpawnAt("Wrench", 0.25f, 0);
            var bots = SEntMan.System<FillbotSystem>();
            Link(bot, disposal);

            Assert.That(bots.TryPickup(bot, nonTrash, disposal), Is.False);
            Assert.That(bots.TryPickup(bot, trash, disposal), Is.True);
            Assert.That(bots.TryDeliver(bot, trash, disposal), Is.True);
            Assert.That(SEntMan.System<SharedHandsSystem>().GetActiveItem(bot), Is.Null);
            Assert.That(SComp<DisposalUnitComponent>(disposal).Container!.ContainedEntities.Contains(trash), Is.True);
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

    private async Task WaitFor(Func<bool> predicate, string message, EntityUid? bot = null)
    {
        var finished = false;
        for (var i = 0; i < 80; i++)
        {
            await RunSeconds(0.25f);
            await Server.WaitPost(() => finished = predicate());
            if (finished)
                return;
        }
        var state = string.Empty;
        if (bot is { } uid)
        {
            await Server.WaitPost(() =>
            {
                var htn = SComp<HTNComponent>(uid);
                state = $" enabled={htn.Enabled} active={SEntMan.HasComponent<Content.Shared.NPC.ActiveNPCComponent>(uid)} " +
                    $"plan={htn.Plan?.CurrentOperator.GetType().Name} index={htn.Plan?.Index} planning={htn.PlanningJob?.Status} " +
                    $"coords={SComp<TransformComponent>(uid).Coordinates} held={SEntMan.System<SharedHandsSystem>().GetActiveItem(uid)} " +
                    $"sink={SEntMan.System<FillbotSystem>().TryGetSink(uid, out var sink)}/{sink}";
            });
        }
        Assert.Fail(message + state);
    }
}
