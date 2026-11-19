// Arcane-Start
#nullable enable
using System.Linq;
using Content.Arcane.Server.Xenobiology;
using Content.Goobstation.Shared.Xenobiology;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.NPC.HTN;
using Content.Server.Polymorph.Systems;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Metabolism;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Polymorph;
using Content.Shared.Radio;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P09XenobiologyTest : InteractionTest
{
    private static readonly EntProtoId LivestockCrate = "CrateNPCXenobioSlime";
    private static readonly EntProtoId Human = "MobHuman";

    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    [Test]
    public async Task ExtractSchedulesOneReactionDespiteRepeatedInjection()
    {
        EntityUid extract = default;
        await Server.WaitAssertion(() =>
        {
            extract = SEntMan.SpawnAttachedTo("GreySlimeExtract", Center);
            var reactive = SEntMan.System<ReactiveSystem>();
            var injection = new ReagentQuantity(new ReagentId("Water", null), FixedPoint2.New(1));
            reactive.ReactionEntity(extract, ReactionMethod.Injection, injection);
            reactive.ReactionEntity(extract, ReactionMethod.Injection, injection);
            Assert.That(SEntMan.GetComponent<SlimeExtractComponent>(extract).Used, Is.True);
            Assert.That(GetEpinephrine(extract), Is.EqualTo(FixedPoint2.Zero));
        });

        await RunSeconds(5.2f);
        await Server.WaitAssertion(() =>
            Assert.That(GetEpinephrine(extract), Is.EqualTo(FixedPoint2.New(15))));
    }

    private FixedPoint2 GetEpinephrine(EntityUid extract)
    {
        var solutions = SEntMan.System<SharedSolutionContainerSystem>();
        Assert.That(solutions.TryGetSolution(extract, "extract", out var solution), Is.True);
        return solution!.Value.Comp.Solution.GetTotalPrototypeQuantity("Epinephrine");
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
}

[RegisterComponent]
public sealed partial class P09RadioProbeComponent : Component
{
    public EntityUid ExpectedSource;
    public readonly List<string> Channels = new();
    public readonly List<string> Messages = new();
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
        if (args.RadioSource != ent.Comp.ExpectedSource)
            return;

        ent.Comp.Channels.Add(args.Channel.ID);
        ent.Comp.Messages.Add(args.OriginalChatMsg.Message);
    }
}
// Arcane-End
