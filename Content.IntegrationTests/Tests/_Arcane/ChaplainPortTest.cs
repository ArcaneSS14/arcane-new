#nullable enable
using System.Linq;
using Content.Arcane.Server.Chaplain;
using Content.Goobstation.Client.SetSelector;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Goobstation.Shared.SetSelector;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Bible.Components;
using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mind;
using Content.Shared.Projectiles;
using Content.Shared.Roles;
using Content.Shared.Throwing;
using Content.Shared.Timing.Systems;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Container = Robust.Shared.Containers.Container;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class ChaplainPortTest : InteractionTest
{
    private readonly List<EntityUid> _minds = [];
    private EntityUid _owner;

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  parent: GodHand
  id: TestArcaneImmovableNullrod
  components:
  - type: Physics
    bodyType: Static

- type: entity
  parent: BaseItem
  id: TestArcaneRecallContainer
""";

    public override async Task DoSetup()
    {
        await base.DoSetup();
        await Server.WaitPost(() => _owner = CreateChaplain(SPlayer));
    }

    protected override async Task TearDown()
    {
        await Server.WaitPost(() =>
        {
            foreach (var mind in _minds)
            {
                if (!SEntMan.EntityExists(mind))
                    continue;
                SEntMan.System<SharedMindSystem>().WipeMind(mind);
                SEntMan.DeleteEntity(mind);
            }
        });
        await base.TearDown();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task AltarIssuedPairsBelongToTheirOwnMindAndRepeatUseDoesNotDuplicate(bool twoHands)
    {
        await SpawnTarget("AltarTechnology");
        var rod = ToServer(await PlaceInHands("Nullrod"));
        await Server.WaitAssertion(() =>
        {
            if (twoHands)
                HandSys.AddHand((SPlayer, Hands), "hand_left", HandLocation.Left);
            Assert.That(Recall.TryBind(SPlayer, rod), Is.True);
        });
        await Interact();
        EntityUid[] ownForms = [];
        EntityUid otherMind = default;
        await Server.WaitAssertion(() =>
        {
            ownForms = SEntMan.GetComponent<NullrodBindingComponent>(_owner).Forms.ToArray();
            Assert.That(ownForms, Has.Length.EqualTo(2));
            Assert.That(ownForms.Select(Prototype), Is.EquivalentTo(new[] { "WeaponPistolCasul", "WeaponPistolJackal" }));
            var other = Spawn("InteractionTestMob");
            otherMind = CreateChaplain(other);
            var extra = Spawn("Nullrod");
            Assert.That(HandSys.TryPickupAnyHand(other, extra), Is.True);
            var coords = SEntMan.GetComponent<TransformComponent>(STarget!.Value).Coordinates;
            InteractSys.UserInteraction(other, coords, STarget.Value);
            InteractSys.UserInteraction(other, coords, STarget.Value);
        });
        await RunTicks(5);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<NullrodBindingComponent>(otherMind).Forms, Has.Count.EqualTo(2));
            Assert.That(Count("WeaponPistolCasul") + Count("WeaponPistolJackal"), Is.EqualTo(4));
            Assert.That(Recall.TryStartRecall(SPlayer, STarget!.Value), Is.True);
        });
        await AwaitDoAfters();
        await Server.WaitAssertion(() =>
        {
            var held = HandSys.EnumerateHeld(SPlayer).ToArray();
            Assert.That(held, Has.Length.EqualTo(twoHands ? 2 : 1));
            Assert.That(held, Is.SubsetOf(ownForms));
            foreach (var form in SEntMan.GetComponent<NullrodBindingComponent>(otherMind).Forms)
            {
                Assert.That(HandSys.IsHolding(SPlayer, form), Is.False);
                Assert.That(Recall.TryBind(SPlayer, form), Is.False);
            }
            Assert.That(Recall.TryBind(SPlayer, Spawn("Nullrod")), Is.False);
            Assert.That(Recall.TryStartRecall(SPlayer, STarget.Value), Is.False, "Full hands cannot start another recall.");
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ADeletedPartnerIsNotRespawnedAndMindCleanupReleasesTheSurvivor(bool removeComponent)
    {
        await SpawnTarget("AltarTechnology");
        await InteractUsing("Nullrod");
        EntityUid survivor = default;
        await Server.WaitAssertion(() =>
        {
            var forms = SEntMan.GetComponent<NullrodBindingComponent>(_owner).Forms.ToArray();
            Assert.That(forms, Has.Length.EqualTo(2));
            SEntMan.DeleteEntity(forms[0]);
            survivor = forms[1];
            Assert.That(SEntMan.GetComponent<NullrodBindingComponent>(_owner).Forms, Is.EquivalentTo(new[] { survivor }));
            Assert.That(Recall.TryStartRecall(SPlayer, STarget!.Value), Is.True);
        });
        await AwaitDoAfters();
        await Server.WaitAssertion(() =>
        {
            Assert.That(HandSys.IsHolding(SPlayer, survivor), Is.True);
            Assert.That(Count("WeaponPistolCasul") + Count("WeaponPistolJackal"), Is.EqualTo(1));
            if (removeComponent)
                SEntMan.RemoveComponent<NullrodBindingComponent>(_owner);
            else
            {
                SEntMan.System<SharedMindSystem>().WipeMind(_owner);
                SEntMan.DeleteEntity(_owner);
            }
            Assert.That(SEntMan.EntityExists(survivor), Is.True);
            Assert.That(SEntMan.HasComponent<BoundNullrodComponent>(survivor), Is.False);
        });
    }

    [Test]
    public async Task CancellingARecallLeavesTheOriginalInsideNestedContainers()
    {
        await SpawnTarget("AltarConvert");
        EntityUid rod = default;
        await Server.WaitAssertion(() =>
        {
            rod = Spawn("Nullrod");
            Assert.That(Recall.TryBind(SPlayer, rod), Is.True);
            PutInNestedContainer(rod);
            Assert.That(Recall.TryStartRecall(SPlayer, STarget!.Value), Is.True);
            Assert.That(Recall.TryStartRecall(SPlayer, STarget.Value), Is.False, "Duplicate prayer requests are rejected.");
        });
        await CancelDoAfters();
        await Server.WaitAssertion(() =>
        {
            Assert.That(HandSys.IsHolding(SPlayer, rod), Is.False);
            Assert.That(SEntMan.System<SharedContainerSystem>().IsEntityInContainer(rod), Is.True);
            Assert.That(Recall.TryStartRecall(SPlayer, STarget!.Value), Is.True);
        });
        await AwaitDoAfters();
        await Server.WaitAssertion(() =>
        {
            Assert.That(HandSys.GetActiveItem(SPlayer), Is.EqualTo(rod));
            Assert.That(Count("Nullrod"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ANewMindCannotFinishTheOldPrayerAndTheOriginalMindKeepsItsSet()
    {
        await SpawnTarget("AltarConvert");
        EntityUid rod = default;
        EntityUid newBody = default;
        await Server.WaitAssertion(() =>
        {
            rod = Spawn("Nullrod");
            Assert.That(Recall.TryBind(SPlayer, rod), Is.True);
            PutInNestedContainer(rod);
            Assert.That(Recall.TryStartRecall(SPlayer, STarget!.Value), Is.True);
            newBody = Spawn("InteractionTestMob");
            SEntMan.EnsureComponent<BibleUserComponent>(newBody);
            SEntMan.System<SharedMindSystem>().TransferTo(_owner, newBody, createGhost: false);
            CreateChaplain(SPlayer);
            Assert.That(Recall.TryBind(SPlayer, rod), Is.False);
        });
        await RunSeconds(6);
        await Server.WaitAssertion(() =>
        {
            Assert.That(HandSys.IsHolding(SPlayer, rod), Is.False);
            Assert.That(HandSys.IsHolding(newBody, rod), Is.False);
            Assert.That(Recall.TryStartRecall(newBody, STarget!.Value), Is.True);
        });
        await RunSeconds(6);
        await Server.WaitAssertion(() => Assert.That(HandSys.IsHolding(newBody, rod), Is.True));
    }

    [TestCase("GodHand", true, true)]
    [TestCase("GodHand", false, true)]
    [TestCase("TestArcaneImmovableNullrod", true, false)]
    [TestCase("TestArcaneImmovableNullrod", false, false)]
    public async Task RecallPreservesTheExactRemovalRestrictionEvenWhenPickupFails(string prototype, bool deleteOnDrop, bool pickedUp)
    {
        await SpawnTarget("AltarConvert");
        EntityUid rod = default;
        await Server.WaitAssertion(() =>
        {
            rod = Spawn(prototype);
            var restriction = SEntMan.GetComponent<UnremoveableComponent>(rod);
            restriction.DeleteOnDrop = deleteOnDrop;
            SEntMan.Dirty(rod, restriction);
            Assert.That(Recall.TryBind(SPlayer, rod), Is.True);
            Assert.That(Recall.TryStartRecall(SPlayer, STarget!.Value), Is.True);
        });
        await AwaitDoAfters();
        await RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(rod), Is.True);
            Assert.That(HandSys.IsHolding(SPlayer, rod), Is.EqualTo(pickedUp));
            Assert.That(SEntMan.GetComponent<UnremoveableComponent>(rod).DeleteOnDrop, Is.EqualTo(deleteOnDrop));
        });
        await Client.WaitAssertion(() => Assert.That(CEntMan.GetComponent<UnremoveableComponent>(
            ToClient(SEntMan.GetNetEntity(rod))).DeleteOnDrop, Is.EqualTo(deleteOnDrop)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RecallUsesTheEmbeddingOwnerAndDoesNotConsumeAnUnextractableForm(bool deleteOnRemove)
    {
        await SpawnTarget("AltarConvert");
        EntityUid rod = default;
        EntityUid victim = default;
        await Server.WaitAssertion(() =>
        {
            rod = Spawn("RatvarianSpear");
            Assert.That(Recall.TryBind(SPlayer, rod), Is.True);
            victim = Spawn("CrateGenericSteel");
            var embedded = SEntMan.GetComponent<EmbeddableProjectileComponent>(rod);
            embedded.DeleteOnRemove = deleteOnRemove;
            var thrown = SEntMan.EnsureComponent<ThrownItemComponent>(rod);
            var hit = new ThrowDoHitEvent(rod, victim, thrown);
            SEntMan.EventBus.RaiseLocalEvent(rod, ref hit);
            Assert.That(embedded.EmbeddedIntoUid, Is.EqualTo(victim));
            Assert.That(Recall.TryStartRecall(SPlayer, STarget!.Value), Is.True);
        });
        await AwaitDoAfters();
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(rod), Is.True);
            Assert.That(HandSys.IsHolding(SPlayer, rod), Is.EqualTo(!deleteOnRemove));
            Assert.That(SEntMan.GetComponent<EmbeddableProjectileComponent>(rod).EmbeddedIntoUid,
                Is.EqualTo(deleteOnRemove ? victim : (EntityUid?) null));
            Assert.That(Count("RatvarianSpear"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task AFormThatMustRemainPartOfItsBodyIsNeverDetachedByRecall()
    {
        await SpawnTarget("AltarConvert");
        EntityUid rod = default;
        await Server.WaitAssertion(() =>
        {
            rod = Spawn("BlessingOfTheReaper");
            Assert.That(SEntMan.HasComponent<NullrodRecallBlockedComponent>(rod), Is.True);
            Assert.That(Recall.TryBind(SPlayer, rod), Is.True);
            Assert.That(Recall.TryStartRecall(SPlayer, STarget!.Value), Is.True);
        });
        await AwaitDoAfters();
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(rod), Is.True);
            Assert.That(SEntMan.HasComponent<UnremoveableComponent>(rod), Is.True);
            Assert.That(HandSys.IsHolding(SPlayer, rod), Is.False);
        });
    }

    [Test]
    public async Task PrayerUsesTheCurrentToggleOwnerAndExpiresWithoutExtendingTheCooldown()
    {
        var sword = ToServer(await PlaceInHands("InquisitorChainSword", enableToggleable: false));
        TimeSpan? end = null;
        await Server.WaitAssertion(() =>
        {
            var prayer = new AlternatePrayEvent(SPlayer);
            SEntMan.EventBus.RaiseLocalEvent(sword, ref prayer);
            end = SEntMan.GetComponent<TimedPrayerToggleComponent>(sword).DeactivateAt;
            Assert.That(end, Is.Not.Null);
            Assert.That(SEntMan.GetComponent<ItemToggleComponent>(sword).Activated, Is.True);
            Assert.That(SEntMan.HasComponent<UnremoveableComponent>(sword), Is.True);
            SEntMan.EventBus.RaiseLocalEvent(sword, ref prayer);
            Assert.That(SEntMan.GetComponent<TimedPrayerToggleComponent>(sword).DeactivateAt, Is.EqualTo(end));
        });
        await RunSeconds(11);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<ItemToggleComponent>(sword).Activated, Is.False);
            Assert.That(SEntMan.HasComponent<UnremoveableComponent>(sword), Is.False);
            Assert.That(SEntMan.GetComponent<DisarmMalusComponent>(sword).Malus, Is.Zero);
            Assert.That(SEntMan.System<UseDelaySystem>().IsDelayed(sword), Is.True);
            var prayer = new AlternatePrayEvent(SPlayer);
            SEntMan.EventBus.RaiseLocalEvent(sword, ref prayer);
            Assert.That(SEntMan.GetComponent<ItemToggleComponent>(sword).Activated, Is.False);
        });
    }

    [TestCase("ClothingHandsGlovesColorYellowBudget", "MaintsAltarNode", "AltarConvertMaint")]
    [TestCase("PlushieCarp", "DragonAltarNode", "AltarDragon")]
    [TestCase("BookRandom", "EmpireAltarNode", "AltarEmpire")]
    public async Task UpgradeAndDeconstructionReturnTheOriginalOfferingOnce(string offering, string node, string altar)
    {
        await SpawnTarget("AltarConvert");
        var item = ToServer(await PlaceInHands(offering));
        await Server.WaitPost(() => SConstruction.SetPathfindingTarget(STarget!.Value, node));
        await Interact();
        await Server.WaitAssertion(() =>
        {
            Assert.That(Prototype(STarget!.Value), Is.EqualTo(altar));
            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(STarget.Value, "altar_offering", out var contents), Is.True);
            Assert.That(contents!.ContainedEntities, Is.EquivalentTo(new[] { item }), "Construction must suppress the map-spawn filler.");
            Assert.That(Count(offering), Is.EqualTo(1));
        });
        await ReturnOffering(item, offering);
    }

    [TestCase("AltarConvertMaint", "ClothingHandsGlovesColorYellowBudget")]
    [TestCase("AltarDragon", "PlushieCarp")]
    [TestCase("AltarEmpire", "BookRandom")]
    public async Task MapSpawnedAltarContainsAndReturnsOneOffering(string altar, string offering)
    {
        await SpawnTarget(altar);
        EntityUid item = default;
        await Server.WaitAssertion(() =>
        {
            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(STarget!.Value, "altar_offering", out var contents), Is.True);
            Assert.That(contents!.ContainedEntities, Has.Count.EqualTo(1));
            item = contents.ContainedEntities.Single();
            Assert.That(Prototype(item), Is.EqualTo(offering));
        });
        await ReturnOffering(item, offering);
    }

    [Test]
    public async Task ACancelledOfferingUpgradeDoesNotConsumeTheItem()
    {
        await SpawnTarget("AltarConvert");
        var offering = ToServer(await PlaceInHands("BookRandom"));
        await Server.WaitPost(() => SConstruction.SetPathfindingTarget(STarget!.Value, "EmpireAltarNode"));
        await Interact(awaitDoAfters: false);
        await CancelDoAfters();
        await Server.WaitAssertion(() =>
        {
            Assert.That(Prototype(STarget!.Value), Is.EqualTo("AltarConvert"));
            Assert.That(HandSys.IsHolding(SPlayer, offering), Is.True);
            Assert.That(Count("BookRandom"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ScriptureSelectionHasThirteenDistinctChoicesAndTheCurrentGearGrantsOneSelector()
    {
        await Server.WaitAssertion(() =>
        {
            var beacon = Spawn("ChaplainUndeterminedBible");
            var sets = SEntMan.GetComponent<SetSelectorComponent>(beacon).AvailableSets;
            Assert.That(sets, Has.Count.EqualTo(13));
            Assert.That(sets.Distinct().Count(), Is.EqualTo(13));
            foreach (var id in sets)
            {
                var set = SProtoMan.Index(id);
                Assert.That(set.Content, Has.Count.EqualTo(1));
                var prototype = SProtoMan.Index<EntityPrototype>(set.Content.Single());
                Assert.That(prototype.Abstract, Is.False, $"{id} must be selectable.");
                var book = Spawn(set.Content.Single());
                Assert.That(SEntMan.HasComponent<BibleComponent>(book), Is.True);
            }
            foreach (var id in new[] { "VisitorChaplain", "VisitorChaplainAlt" })
                Assert.That(SProtoMan.Index<StartingGearPrototype>(id).Equipment["belt"].Id,
                    Is.EqualTo("ChaplainUndeterminedBible"));
            ProtoId<StartingGearPrototype> chaplainGear = "ChaplainGear";
            ProtoId<StartingGearPrototype> ertGear = "ERTChaplainGearEVA";
            Assert.That(SProtoMan.Index(chaplainGear).Storage["back"]
                .Count(id => id.Id == "ChaplainUndeterminedBible"), Is.EqualTo(1));
            Assert.That(SProtoMan.Index(ertGear).Storage["belt"]
                .Count(id => id.Id == "ChaplainUndeterminedBible"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ScriptureBeaconApprovesThroughItsExistingInterfaceOnce()
    {
        var netBeacon = await PlaceInHands("ChaplainUndeterminedBible");
        var beacon = ToServer(netBeacon);
        await Server.WaitAssertion(() => Assert.That(SUiSys.TryOpenUi(beacon, SetSelectorUIKey.Key, SPlayer), Is.True));
        await RunUntilSynced();
        await Client.WaitPost(() =>
        {
            var ui = CEntMan.System<SharedUserInterfaceSystem>();
            var uid = CEntMan.GetEntity(netBeacon);
            ui.ClientSendUiMessage(uid, SetSelectorUIKey.Key, new SetSelectorChangeSetMessage(-1));
            ui.ClientSendUiMessage(uid, SetSelectorUIKey.Key, new SetSelectorApproveMessage());
            ui.ClientSendUiMessage(uid, SetSelectorUIKey.Key, new SetSelectorChangeSetMessage(999));
        });
        await RunUntilSynced();
        await Server.WaitAssertion(() => Assert.That(SEntMan.GetComponent<SetSelectorComponent>(beacon).SelectedSets, Is.Empty));
        Button choice = null!;
        await Client.WaitAssertion(() =>
        {
            Assert.That(TryFindWindow(out SetSelectorMenu? window), Is.True);
            choice = window!.FindControl<BoxContainer>("SetsGrid").Children.OfType<SelectableSet>().First().SetButton;
        });
        await ClickControl(choice);
        await RunUntilSynced();
        await Client.WaitPost(() =>
        {
            var ui = CEntMan.System<SharedUserInterfaceSystem>();
            var uid = CEntMan.GetEntity(netBeacon);
            ui.ClientSendUiMessage(uid, SetSelectorUIKey.Key, new SetSelectorApproveMessage());
            ui.ClientSendUiMessage(uid, SetSelectorUIKey.Key, new SetSelectorApproveMessage());
        });
        await RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(beacon), Is.False);
            Assert.That(Count("Bible"), Is.EqualTo(1));
        });
    }

    private async Task ReturnOffering(EntityUid item, string prototype)
    {
        await PlaceInHands("Crowbar");
        await Server.WaitPost(() => SConstruction.SetPathfindingTarget(STarget!.Value, "AltarConvertNode"));
        await Interact();
        await Server.WaitAssertion(() =>
        {
            Assert.That(Prototype(STarget!.Value), Is.EqualTo("AltarConvert"));
            Assert.That(SEntMan.EntityExists(item), Is.True);
            Assert.That(SEntMan.System<SharedContainerSystem>().IsEntityInContainer(item), Is.False);
            Assert.That(Count(prototype), Is.EqualTo(1));
        });
        await Interact();
        await Server.WaitAssertion(() => Assert.That(Count(prototype), Is.EqualTo(1), "A second pry must not spawn another offering."));
    }

    private NullrodRecallSystem Recall => SEntMan.System<NullrodRecallSystem>();
    private EntityUid Spawn(string prototype) => SEntMan.SpawnEntity(prototype, SEntMan.GetCoordinates(PlayerCoords));
    private string? Prototype(EntityUid uid) => SEntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID;

    private EntityUid CreateChaplain(EntityUid body)
    {
        SEntMan.EnsureComponent<BibleUserComponent>(body);
        var minds = SEntMan.System<SharedMindSystem>();
        var mind = minds.CreateMind(body == SPlayer ? ClientSession.UserId : null, "chaplain port test").Owner;
        _minds.Add(mind);
        minds.TransferTo(mind, body, createGhost: false);
        return mind;
    }

    private void PutInNestedContainer(EntityUid item)
    {
        var containers = SEntMan.System<SharedContainerSystem>();
        var outer = Spawn("TestArcaneRecallContainer");
        var inner = Spawn("TestArcaneRecallContainer");
        Assert.That(containers.Insert(inner, containers.EnsureContainer<Container>(outer, "outer")), Is.True);
        Assert.That(containers.Insert(item, containers.EnsureContainer<Container>(inner, "inner")), Is.True);
    }

    private int Count(string prototype)
    {
        var count = 0;
        var query = SEntMan.EntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out _, out var meta))
        {
            if (meta.EntityPrototype?.ID == prototype)
                count++;
        }
        return count;
    }
}
