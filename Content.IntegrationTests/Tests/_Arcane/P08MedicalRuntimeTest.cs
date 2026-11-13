#nullable enable
using System.Linq;
using Content.Arcane.Shared.Species.Slime;
using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Shared.Body;
using Content.Server.Zombies;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Emp;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Metabolism;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Zombies;
using Content.Trauma.Common.VentCrawling;
using Content.Trauma.Shared.VentCrawling.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P08MedicalRuntimeTest : InteractionTest
{
    private static readonly EntProtoId Human = "MobHuman";
    private static readonly EntProtoId Slime = "MobSlimePerson";
    private static readonly EntProtoId Ipc = "MobIPC";
    private static readonly EntProtoId Synth = "MobSynth";
    private static readonly EntProtoId Headset = "ClothingHeadsetGrey";
    private static readonly EntProtoId Paper = "Paper";
    private static readonly EntProtoId VentHolder = "VentCrawlerHolder";
    private static readonly ProtoId<OrganCategoryPrototype> Head = "Head";
    private static readonly ProtoId<OrganCategoryPrototype> Torso = "Torso";
    private static readonly ProtoId<OrganCategoryPrototype> Core = "Brain";
    private static readonly ProtoId<OrganCategoryPrototype> Eyes = "Eyes";
    private static readonly ProtoId<OrganCategoryPrototype> Tongue = "Tongue";
    private static readonly ProtoId<OrganCategoryPrototype> Heart = "Heart";
    private static readonly ProtoId<OrganCategoryPrototype> HandLeft = "HandLeft";
    private static readonly ProtoId<OrganCategoryPrototype> ArmLeft = "ArmLeft";

    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    [Test]
    public async Task SlimeRegrowsHeadAndSenseOrgansOnceWithoutReplacingCore()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SEntMan.SpawnAttachedTo(Slime, Center);
            var body = SEntMan.System<BodySystem>();
            var parts = SEntMan.System<BodyPartSystem>();
            var satiation = SEntMan.System<SatiationSystem>();
            var food = SEntMan.GetComponent<SatiationComponent>(uid);
            var originalCore = body.GetOrgan(uid, Core);
            var originalHead = body.GetOrgan(uid, Head);
            Assert.That(originalCore, Is.Not.Null);
            Assert.That(originalHead, Is.Not.Null);
            Assert.That(parts.RemoveOrgan(body.GetOrgan(uid, Torso)!.Value, originalHead!.Value), Is.True);

            satiation.SetValue((uid, food), SatiationSystem.Hunger, 59f);
            satiation.SetValue((uid, food), SatiationSystem.Thirst, 100f);
            Assert.That(Regrow(uid), Is.False);
            Assert.That(body.GetOrgan(uid, Head), Is.Null);
            Assert.That(satiation.GetValueOrNull((uid, food), SatiationSystem.Thirst), Is.EqualTo(100f));

            satiation.SetValue((uid, food), SatiationSystem.Hunger, 60f);
            Assert.That(Regrow(uid), Is.True);
            Assert.That(body.GetOrgan(uid, Head), Is.Not.Null.And.Not.EqualTo(originalHead));
            Assert.That(body.GetOrgan(uid, Core), Is.EqualTo(originalCore));
            Assert.That(body.GetOrgan(uid, Eyes), Is.Not.Null);
            Assert.That(body.GetOrgan(uid, Tongue), Is.Not.Null);
            Assert.That(satiation.GetValueOrNull((uid, food), SatiationSystem.Hunger), Is.Zero);
            Assert.That(satiation.GetValueOrNull((uid, food), SatiationSystem.Thirst), Is.Zero);
            Assert.That(Regrow(uid), Is.False);
        });
    }

    [TestCase(MobState.Critical, "Ambuzol", 10, false)]
    [TestCase(MobState.Dead, "AmbuzolPlus", 5, true)]
    public async Task BloodstreamMedicineRestoresOriginalBodyWithoutRevivingIt(MobState state, string drug, int dose, bool immune)
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SEntMan.SpawnAttachedTo(Human, Center);
            var body = SEntMan.System<BodySystem>();
            var hands = SEntMan.System<SharedHandsSystem>();
            var mobs = SEntMan.System<MobStateSystem>();
            var zombies = SEntMan.System<ZombieSystem>();
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            var inventory = SEntMan.System<InventorySystem>();
            var heart = body.GetOrgan(uid, Heart);
            var metabolizers = body.EnumerateOrgans<MetabolizerComponent>(uid).Count();
            var handCount = hands.GetHandCount(uid);
            var blood = SEntMan.GetComponent<BloodstreamComponent>(uid);
            var before = blood.BloodReferenceSolution.Clone();
            Assert.That(metabolizers, Is.Positive);
            Assert.That(handCount, Is.Positive);
            var left = SEntMan.SpawnAttachedTo(Headset, Center);
            var right = SEntMan.SpawnAttachedTo(Headset, Center);
            var paper = SEntMan.SpawnAttachedTo(Paper, Center);
            Assert.That(inventory.TryEquip(uid, left, "ears", force: true), Is.True);
            Assert.That(inventory.TryEquip(uid, right, "earsright", force: true), Is.True);
            Assert.That(hands.TryPickupAnyHand(uid, paper, checkActionBlocker: false), Is.True);
            if (state == MobState.Dead)
                SEntMan.EnsureComponent<UnremoveableComponent>(paper).DeleteOnDrop = false;

            zombies.ZombifyEntity(uid);
            Assert.That(SEntMan.HasComponent<ZombieComponent>(uid), Is.True);
            Assert.That(hands.GetHandCount(uid), Is.Zero);
            Assert.That(inventory.TryGetSlotEntity(uid, "ears", out var leftWhileInfected) && leftWhileInfected == left, Is.True);
            Assert.That(inventory.TryGetSlotEntity(uid, "earsright", out var rightWhileInfected) && rightWhileInfected == right, Is.True);
            Assert.That(blood.BloodSolution, Is.Not.Null);
            Assert.That(solutions.TryAddReagent(blood.BloodSolution!.Value, drug, FixedPoint2.New(dose)), Is.True);
            zombies.Update(0);
            Assert.That(SEntMan.HasComponent<ZombieComponent>(uid), Is.True);

            mobs.ChangeMobState(uid, state);
            zombies.Update(0);
            Assert.That(SEntMan.HasComponent<ZombieComponent>(uid), Is.False);
            Assert.That(SEntMan.HasComponent<ZombieImmuneComponent>(uid), Is.EqualTo(immune));
            Assert.That(mobs.IsDead(uid), Is.EqualTo(state == MobState.Dead));
            Assert.That(mobs.IsCritical(uid), Is.EqualTo(state == MobState.Critical));
            Assert.That(body.GetOrgan(uid, Heart), Is.EqualTo(heart));
            Assert.That(body.EnumerateOrgans<MetabolizerComponent>(uid).Count(), Is.EqualTo(metabolizers));
            Assert.That(hands.GetHandCount(uid), Is.EqualTo(handCount));
            Assert.That(hands.EnumerateHeld(uid), Does.Contain(paper));
            Assert.That(blood.BloodReferenceSolution.Volume, Is.EqualTo(before.Volume));
            Assert.That(blood.BloodReferenceSolution.GetTotalPrototypeQuantity("Blood"), Is.EqualTo(before.GetTotalPrototypeQuantity("Blood")));
        });
    }

    [Test]
    public async Task ZombificationEjectsFromVentAndCureKeepsLostAnatomy()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SEntMan.SpawnAttachedTo(Human, Center);
            var vent = SEntMan.SpawnAttachedTo(VentHolder, Center);
            var holder = SEntMan.GetComponent<VentCrawlerHolderComponent>(vent);
            var crawler = SEntMan.EnsureComponent<VentCrawlerComponent>(uid);
            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.Insert(uid, holder.Container), Is.True);
            SEntMan.EnsureComponent<BeingVentCrawlerComponent>(uid).Holder = vent;
            crawler.InTube = true;

            var zombies = SEntMan.System<ZombieSystem>();
            var body = SEntMan.System<BodySystem>();
            var hands = SEntMan.System<SharedHandsSystem>();
            var before = hands.GetHandCount(uid);
            zombies.ZombifyEntity(uid);
            Assert.That(SEntMan.HasComponent<BeingVentCrawlerComponent>(uid), Is.False);
            Assert.That(crawler.InTube, Is.False);
            Assert.That(SEntMan.EntityExists(vent), Is.False);

            Assert.That(SEntMan.System<BodyPartSystem>().RemoveOrgan(
                body.GetOrgan(uid, ArmLeft)!.Value, body.GetOrgan(uid, HandLeft)!.Value), Is.True);
            var blood = SEntMan.GetComponent<BloodstreamComponent>(uid);
            Assert.That(SEntMan.System<SharedSolutionContainerSystem>().TryAddReagent(
                blood.BloodSolution!.Value, "Ambuzol", FixedPoint2.New(10)), Is.True);
            SEntMan.System<MobStateSystem>().ChangeMobState(uid, MobState.Critical);
            zombies.Update(0);
            Assert.That(SEntMan.HasComponent<ZombieComponent>(uid), Is.False);
            Assert.That(body.GetOrgan(uid, HandLeft), Is.Null);
            Assert.That(hands.GetHandCount(uid), Is.EqualTo(before - 1));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task EmpPurgesPendingAndIncapacitatedSiliconInfection(bool synth)
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SEntMan.SpawnAttachedTo(synth ? Synth : Ipc, Center);
            var zombies = SEntMan.System<ZombieSystem>();
            var mobs = SEntMan.System<MobStateSystem>();
            Assert.That(SEntMan.HasComponent<SiliconZombieComponent>(uid), Is.True);
            Assert.That(SEntMan.HasComponent<ZombieImmuneComponent>(uid), Is.False);

            SEntMan.AddComponent<PendingZombieComponent>(uid);
            SEntMan.AddComponent<ZombifyOnDeathComponent>(uid);
            Assert.That(Pulse(uid), Is.True);
            Assert.That(SEntMan.HasComponent<PendingZombieComponent>(uid), Is.False);
            Assert.That(SEntMan.HasComponent<ZombifyOnDeathComponent>(uid), Is.False);

            zombies.ZombifyEntity(uid);
            Assert.That(SEntMan.HasComponent<ZombieComponent>(uid), Is.True);
            Pulse(uid);
            Assert.That(SEntMan.HasComponent<ZombieComponent>(uid), Is.True);
            mobs.ChangeMobState(uid, MobState.Dead);
            Assert.That(Pulse(uid), Is.True);
            Assert.That(SEntMan.HasComponent<ZombieComponent>(uid), Is.False);
            Assert.That(mobs.IsDead(uid), Is.True);
        });
    }

    private bool Regrow(EntityUid uid)
    {
        var action = new SlimeRegrowActionEvent { Performer = uid };
        SEntMan.EventBus.RaiseLocalEvent(uid, action);
        return action.Handled;
    }

    [Test]
    public async Task GibbedZombieDropsItsOriginalHeldItem()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SEntMan.SpawnAttachedTo(Human, Center);
            var paper = SEntMan.SpawnAttachedTo(Paper, Center);
            Assert.That(SEntMan.System<SharedHandsSystem>().TryPickupAnyHand(uid, paper, checkActionBlocker: false), Is.True);
            SEntMan.System<ZombieSystem>().ZombifyEntity(uid);
            var giblets = SEntMan.System<GibbingSystem>().Gib(uid);
            Assert.That(giblets, Does.Contain(paper));
            Assert.That(SEntMan.EntityExists(paper), Is.True);
        });
    }

    private bool Pulse(EntityUid uid)
    {
        var pulse = new EmpPulseEvent(0, false, false, TimeSpan.Zero, null);
        SEntMan.EventBus.RaiseLocalEvent(uid, ref pulse);
        return pulse.Affected;
    }
}
