// Arcane-Start
#nullable enable
using System.Linq;
using Content.Arcane.Server.Xenobiology;
using Content.Goobstation.Shared.Xenobiology;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Anomaly.Components;
using Content.Server.Lathe;
using Content.Server.NPC.HTN;
using Content.Server.Polymorph.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Lathe;
using Content.Shared.Metabolism;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Polymorph;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P09XenobiologyTest : InteractionTest
{
    private static readonly EntProtoId LivestockCrate = "CrateNPCXenobioSlime";
    private static readonly EntProtoId Anomaly = "AnomalyLiquid";
    private static readonly EntProtoId Human = "MobHuman";
    private static readonly EntProtoId MetalExtract = "MetalSlimeExtract";
    private static readonly EntProtoId ResearchServer = "ResearchAndDevelopmentServer";
    private static readonly ProtoId<StackPrototype> GlassStack = "Glass";
    private static readonly ProtoId<StackPrototype> ReinforcedGlass = "ReinforcedGlass";
    private static readonly ProtoId<BreedPrototype> Grey = "GreyMutation";

    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    [Test]
    public async Task LivestockCrateContainsTwoRealGreySlimes()
    {
        await Server.WaitAssertion(() =>
        {
            var slimes = SpawnCrate(out _);
            Assert.That(slimes, Has.Length.EqualTo(2));
            foreach (var uid in slimes)
            {
                Assert.That(SEntMan.GetComponent<SlimeComponent>(uid).Breed, Is.EqualTo(Grey));
                Assert.That(SEntMan.System<MobStateSystem>().IsAlive(uid), Is.True);
                Assert.That(SEntMan.HasComponent<PendingSlimeSpawnComponent>(uid), Is.False);
            }
        });
    }

    [Test]
    public async Task ChemicalAnomalyRetainsTheExistingSelectedReagentsAndWeights()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SEntMan.SpawnAttachedTo(Anomaly, Center);
            var chemistry = SEntMan.GetComponent<ReagentProducerAnomalyComponent>(uid);
            Assert.That(chemistry.DangerousChemicals.Select(p => p.Id), Is.SupersetOf(new[]
            {
                "NitricAcid", "Heroin", "Microplastics", "Bungotoxin", "WeldingFuel", "ChloralHydrate", "Allicin", "Phenol",
            }));
            Assert.That(chemistry.UsefulChemicals.Select(p => p.Id), Is.SupersetOf(new[]
            {
                "Probital", "Immurin", "Devirate", "Mitotrophin", "Monover", "Ibuprofen", "Formaldehyde", "Morphine",
            }));
            Assert.That(chemistry.FunChemicals.Select(p => p.Id), Is.SupersetOf(new[] { "MouseBites", "Copium", "Nostalgia", "Profanol" }));
            Assert.That(chemistry.UsefulChemicals.Count(p => p.Id == "Probital"), Is.EqualTo(2));
            var all = chemistry.DangerousChemicals.Concat(chemistry.UsefulChemicals).Concat(chemistry.FunChemicals).Select(p => p.Id);
            Assert.That(all, Has.None.EqualTo("CrapoToxin").And.None.EqualTo("StypticCrystalCatalyst"));
        });
    }

    [Test]
    public async Task LatchReservesOneVictimAndRespectsStomachCapacity()
    {
        EntityUid prey = default;
        EntityUid stomachSolution = default;
        await Server.WaitAssertion(() =>
        {
            var slimes = ReleaseSlimes();
            var slime = slimes[0];
            prey = SEntMan.SpawnAttachedTo(Human, Center.Offset(new Vector2(0.5f, 0)));
            var stomachs = SEntMan.System<BodySystem>().GetOrgans<StomachComponent>(slime);
            Assert.That(stomachs, Has.Count.EqualTo(1));
            // Measure the latch's transfer before digestion can consume the stored blood.
            SEntMan.GetComponent<MetabolizerComponent>(stomachs[0].Owner).Stages.Remove("Digestion");
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            Assert.That(solutions.TryGetSolution(stomachs[0].Owner, StomachSystem.DefaultSolutionName, out var solution), Is.True);
            stomachSolution = solution!.Value.Owner;
            solutions.SetCapacity(solution.Value, FixedPoint2.New(1));
            var latch = SEntMan.System<SlimeLatchSystem>();
            Assert.That(latch.NpcTryLatch((slime, SEntMan.GetComponent<SlimeComponent>(slime)), prey), Is.True);
            Assert.That(latch.NpcTryLatch((slimes[1], SEntMan.GetComponent<SlimeComponent>(slimes[1])), prey), Is.False);
        });

        await RunSeconds(2.6f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<SolutionComponent>(stomachSolution).Solution.Volume, Is.EqualTo(FixedPoint2.New(1)));
            var blood = SEntMan.GetComponent<BloodstreamComponent>(prey);
            Assert.That(blood.BloodSolution!.Value.Comp.Solution.GetTotalPrototypeQuantity("XenobioSlimeToxin"), Is.GreaterThan(FixedPoint2.Zero));
        });
    }

    [Test]
    public async Task ImmediateLatchAndCorpseEatingCompleteWithoutStaleReservations()
    {
        await Server.WaitAssertion(() =>
        {
            var slimes = ReleaseSlimes();
            var prey = SEntMan.SpawnAttachedTo(Human, Center.Offset(new Vector2(0.5f, 0)));
            var first = SEntMan.GetComponent<SlimeComponent>(slimes[0]);
            first.LatchDoAfterDuration = TimeSpan.Zero;
            var latch = SEntMan.System<SlimeLatchSystem>();
            Assert.That(latch.NpcTryLatch((slimes[0], first), prey), Is.True);
            Assert.That(first.PendingLatchTarget, Is.Null);
            Assert.That(first.LatchedTarget, Is.EqualTo(prey));
            latch.Unlatch((slimes[0], first));

            SEntMan.System<MobStateSystem>().ChangeMobState(prey, MobState.Dead);
            var eater = SEntMan.GetComponent<SlimeCorpseEaterComponent>(slimes[1]);
            eater.EatDuration = TimeSpan.Zero;
            var body = SEntMan.System<BodySystem>();
            var initialOrgans = body.GetOrgans<EdibleComponent>(prey).Count;
            Assert.That(SEntMan.System<SlimeCorpseEaterSystem>().TryStartEating(slimes[1], prey, out _), Is.True);
            Assert.That(body.GetOrgans<EdibleComponent>(prey).Count, Is.EqualTo(initialOrgans - 1));
            Assert.That(eater.ActiveTarget, Is.Null);
        });
    }

    [Test]
    public async Task CorpseEatersRecheckRevivalAndRemoveDifferentOrgans()
    {
        EntityUid prey = default;
        EntityUid[] slimes = [];
        var initialOrgans = 0;
        await Server.WaitAssertion(() =>
        {
            slimes = ReleaseSlimes();
            prey = SEntMan.SpawnAttachedTo(Human, Center.Offset(new Vector2(0.5f, 0)));
            initialOrgans = SEntMan.System<BodySystem>().GetOrgans<EdibleComponent>(prey).Count;
            Assert.That(initialOrgans, Is.GreaterThan(2));
            SEntMan.System<MobStateSystem>().ChangeMobState(prey, MobState.Dead);
            Assert.That(SEntMan.System<SlimeCorpseEaterSystem>().TryStartEating(slimes[0], prey, out _), Is.True);
            SEntMan.System<MobStateSystem>().ChangeMobState(prey, MobState.Alive);
        });

        await RunSeconds(5.5f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<BodySystem>().GetOrgans<EdibleComponent>(prey).Count, Is.EqualTo(initialOrgans));
            SEntMan.System<MobStateSystem>().ChangeMobState(prey, MobState.Dead);
            var eater = SEntMan.System<SlimeCorpseEaterSystem>();
            Assert.That(eater.TryStartEating(slimes[0], prey, out _), Is.True);
            Assert.That(eater.TryStartEating(slimes[1], prey, out _), Is.True);
        });

        await RunSeconds(5.5f);
        await Server.WaitAssertion(() =>
            Assert.That(SEntMan.System<BodySystem>().GetOrgans<EdibleComponent>(prey).Count, Is.EqualTo(initialOrgans - 2)));
    }

    [Test]
    public async Task MetalExtractProducesTwoStacksWithoutScalingTheYieldByDose()
    {
        await Server.WaitAssertion(() =>
        {
            var extract = SEntMan.SpawnAttachedTo(MetalExtract, Center);
            var reaction = SEntMan.System<ReactiveSystem>();
            var water = new Solution([new("Water", 20)]);
            reaction.DoEntityReaction(extract, water, ReactionMethod.Injection);
            reaction.DoEntityReaction(extract, water, ReactionMethod.Injection);
            Assert.That(StackCounts(GlassStack), Is.EqualTo(new[] { 15 }));
            Assert.That(StackCounts(ReinforcedGlass), Is.EqualTo(new[] { 5 }));
        });
    }

    [Test]
    public async Task PolymorphRevertsHeldEquipmentAndKeepsOriginalBodyOnDelete()
    {
        var original = EntityUid.Invalid;
        var looseItem = EntityUid.Invalid;
        await Server.WaitAssertion(() =>
        {
            original = SEntMan.SpawnAttachedTo(Human, Center);
            var bag = SEntMan.SpawnAttachedTo("ClothingBackpack", Center);
            var pen = SEntMan.SpawnAttachedTo("Pen", Center);
            var inventory = SEntMan.System<InventorySystem>();
            var hands = SEntMan.System<SharedHandsSystem>();
            Assert.That(inventory.TryEquip(original, bag, "back", force: true), Is.True);
            Assert.That(hands.TryPickupAnyHand(original, pen, checkActionBlocker: false), Is.True);

            var poly = SEntMan.System<PolymorphSystem>();
            var config = new PolymorphConfiguration
            {
                Entity = Human,
                Inventory = PolymorphInventoryChange.Transfer,
                Forced = true,
                TransferDamage = false,
                Delay = 0,
            };
            var first = poly.PolymorphEntity(original, config);
            Assert.That(first, Is.Not.Null);
            Assert.That(inventory.TryGetSlotEntity(first!.Value, "back", out var equipped) && equipped == bag, Is.True);
            Assert.That(hands.IsHolding(first.Value, pen), Is.True);
            Assert.That(poly.RevertPolymorph(first.Value), Is.EqualTo(original));
            Assert.That(poly.RevertPolymorph(first.Value), Is.Null);
            Assert.That(inventory.TryGetSlotEntity(original, "back", out equipped) && equipped == bag, Is.True);
            Assert.That(hands.IsHolding(original, pen), Is.True);

            foreach (var hand in hands.EnumerateHands(original).ToArray())
                hands.RemoveHand(original, hand);

            var second = poly.PolymorphEntity(original, config);
            Assert.That(second, Is.Not.Null);
            looseItem = SEntMan.SpawnAttachedTo("Pen", Center);
            Assert.That(hands.TryPickupAnyHand(second!.Value, looseItem, checkActionBlocker: false), Is.True);
            SEntMan.DeleteEntity(second!.Value);
        });

        await RunTicks(2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(original), Is.True);
            Assert.That(SEntMan.EntityExists(looseItem), Is.True);
            Assert.That(SEntMan.System<SharedContainerSystem>().IsEntityInContainer(looseItem), Is.False);
        });
    }

    [TestCase("SecurityTechFab", "Security")]
    [TestCase("MedicalTechFab", "Medical")]
    public async Task OnlyNewAvailableRecipesTransmitOnTheDepartmentChannel(string prototype, string channel)
    {
        try
        {
            await VerifyTechFabBroadcast(prototype, channel);
        }
        catch (Exception error)
        {
            TestContext.Out.WriteLine(error);
            throw;
        }
    }

    private async Task VerifyTechFabBroadcast(string prototype, string channel)
    {
        EntityUid fab = default;
        EntityUid researchServer = default;
        P09RadioProbeComponent? probe = null;
        await Server.WaitAssertion(() =>
        {
            var station = SEntMan.SpawnAttachedTo("TestStation", Center);
            SEntMan.System<ServerStationSystem>().AddGridToStation(station, MapData.Grid);
            researchServer = SEntMan.SpawnAttachedTo(ResearchServer, Center);
            fab = SEntMan.SpawnAttachedTo(prototype, Center);
            SEntMan.System<PowerReceiverSystem>().SetNeedsPower(fab, false);
            SEntMan.AddComponent<TelecomExemptComponent>(fab);
            var receiver = SEntMan.SpawnAttachedTo(null, Center);
            SEntMan.AddComponent<ActiveRadioComponent>(receiver).ReceiveAllChannels = true;
            probe = SEntMan.AddComponent<P09RadioProbeComponent>(receiver);
            probe.ExpectedSource = fab;
        });

        await RunTicks(30);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<PowerReceiverSystem>().IsPowered(fab), Is.True);
            Assert.That(SEntMan.GetComponent<ResearchClientComponent>(fab).Server, Is.EqualTo(researchServer));
            var lathe = SEntMan.System<LatheSystem>();
            var component = SEntMan.GetComponent<LatheComponent>(fab);
            var dynamicRecipes = new HashSet<ProtoId<LatheRecipePrototype>>();
            lathe.AddRecipesFromPacks(dynamicRecipes, component.DynamicPacks);
            var available = lathe.GetAvailableRecipes(fab, component);
            var recipe = dynamicRecipes.First(id => !available.Contains(id) && SProtoMan.Index(id).RequiredAlerts == null);
            var unrelated = SProtoMan.EnumeratePrototypes<LatheRecipePrototype>()
                .First(p => !p.Abstract && !dynamicRecipes.Any(id => id.Id == p.ID) && !available.Any(id => id.Id == p.ID));

            var research = SEntMan.System<SharedResearchSystem>();
            research.AddLatheRecipe(researchServer, unrelated.ID);
            research.AddLatheRecipe(researchServer, recipe.Id);
            research.AddLatheRecipe(researchServer, recipe.Id);
            var sync = new TechnologyDatabaseSynchronizedEvent();
            SEntMan.EventBus.RaiseLocalEvent(fab, ref sync);
            Assert.That(probe!.Channels, Is.EqualTo(new[] { channel }));
        });
    }

    private EntityUid[] SpawnCrate(out EntityUid crate)
    {
        crate = SEntMan.SpawnAttachedTo(LivestockCrate, Center);
        var containers = SEntMan.System<SharedContainerSystem>();
        Assert.That(containers.TryGetContainer(crate, "entity_storage", out var container), Is.True);
        return container!.ContainedEntities.ToArray();
    }

    private EntityUid[] ReleaseSlimes()
    {
        var slimes = SpawnCrate(out var crate);
        Assert.That(slimes, Has.Length.EqualTo(2));
        var containers = SEntMan.System<SharedContainerSystem>();
        foreach (var slime in slimes)
        {
            SEntMan.RemoveComponent<HTNComponent>(slime);
            Assert.That(containers.TryRemoveFromContainer(slime, true), Is.True);
            Transform.SetCoordinates(slime, Center);
        }

        SEntMan.DeleteEntity(crate);
        return slimes;
    }

    private int[] StackCounts(ProtoId<StackPrototype> stack)
        => SEntMan.AllEntities<StackComponent>()
            .Where(entity => entity.Comp.StackTypeId == stack)
            .Select(entity => SEntMan.System<SharedStackSystem>().GetCount(entity.Owner))
            .ToArray();

}

[RegisterComponent]
public sealed partial class P09RadioProbeComponent : Component
{
    public EntityUid ExpectedSource;
    public readonly List<string> Channels = new();
}

public sealed partial class P09RadioProbeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<P09RadioProbeComponent, RadioReceiveEvent>(OnRadio);
    }

    private void OnRadio(Entity<P09RadioProbeComponent> ent, ref RadioReceiveEvent args)
    {
        if (args.RadioSource == ent.Comp.ExpectedSource)
            ent.Comp.Channels.Add(args.Channel.ID);
    }
}
// Arcane-End
