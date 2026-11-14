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
            var other = SpawnNearby("InteractionTestMob");
            otherMind = CreateChaplain(other);
            var extra = SpawnNearby("Nullrod");
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
            Assert.That(Recall.TryBind(SPlayer, SpawnNearby("Nullrod")), Is.False);
            Assert.That(Recall.TryStartRecall(SPlayer, STarget.Value), Is.False, "Full hands cannot start another recall.");
        });
    }

    [TestCase(false)]
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
    public async Task ANewMindCannotFinishTheOldPrayerAndTheOriginalMindKeepsItsSet()
    {
        await SpawnTarget("AltarConvert");
        EntityUid rod = default;
        EntityUid newBody = default;
        await Server.WaitAssertion(() =>
        {
            rod = SpawnNearby("Nullrod");
            Assert.That(Recall.TryBind(SPlayer, rod), Is.True);
            PutInNestedContainer(rod);
            Assert.That(Recall.TryStartRecall(SPlayer, STarget!.Value), Is.True);
            newBody = SpawnNearby("InteractionTestMob");
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

    [TestCase("TestArcaneImmovableNullrod", true, false)]
    public async Task RecallPreservesTheExactRemovalRestrictionEvenWhenPickupFails(string prototype, bool deleteOnDrop, bool pickedUp)
    {
        await SpawnTarget("AltarConvert");
        EntityUid rod = default;
        await Server.WaitAssertion(() =>
        {
            rod = SpawnNearby(prototype);
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

    private NullrodRecallSystem Recall => SEntMan.System<NullrodRecallSystem>();
    private EntityUid SpawnNearby(string prototype) => SEntMan.SpawnEntity(prototype, SEntMan.GetCoordinates(PlayerCoords));
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
        var outer = SpawnNearby("TestArcaneRecallContainer");
        var inner = SpawnNearby("TestArcaneRecallContainer");
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
