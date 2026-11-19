#nullable enable
using System.Linq;
using Content.Arcane.Shared.MouseSwarm;
using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Common.Targeting;
using Content.Server.NPC.HTN;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class MouseSwarmPortTest : InteractionTest
{
    protected override string PlayerPrototype => "MobMouse";

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  parent: MobMouse
  id: TestArcaneSwarmGhostMouse
  components:
  - type: GhostRole
    prob: 1
""";

    [Test]
    public async Task StoppedGhostRoleCannotReturnWhenItsMindLeaves()
    {
        await Server.WaitAssertion(() =>
        {
            var mouse = SpawnMouse("TestArcaneSwarmGhostMouse");
            var roles = SEntMan.System<GhostRoleSystem>();
            var minds = SEntMan.System<SharedMindSystem>();
            var role = SEntMan.GetComponent<GhostRoleComponent>(mouse);
            var available = roles.GetGhostRoleCount();
            var mind = minds.CreateMind(null, "swarm role lifecycle");
            minds.TransferTo(mind, mouse);
            Assert.That(roles.GetGhostRoleCount(), Is.EqualTo(available - 1));
            minds.TransferTo(mind, null, createGhost: false);
            Assert.That(roles.GetGhostRoleCount(), Is.EqualTo(available),
                "A running, available body still becomes a ghost role when its mind leaves.");
            minds.TransferTo(mind, mouse);

            SEntMan.RemoveComponentDeferred<GhostRoleComponent>(mouse);
            Assert.That(role.LifeStage, Is.GreaterThan(ComponentLifeStage.Running));
            minds.TransferTo(mind, null, createGhost: false);
            Assert.That(roles.GetGhostRoleCount(), Is.EqualTo(available - 1),
                "A stopped role must not re-enter the registry before deferred removal completes.");
            SEntMan.DeleteEntity(mouse);
            Assert.That(roles.GetGhostRoleCount(), Is.EqualTo(available - 1));
        });
    }

    [Test]
    public async Task QueuedAbsorbedSwarmCannotDuplicateMembersAndRemovalRestoresOwnedDamage()
    {
        EntityUid host = default;
        NetEntity netHost = NetEntity.Invalid;
        DamageSpecifier expected = default!;
        var originalCount = 0;
        await Server.WaitAssertion(() =>
        {
            originalCount = CountMice();
            host = SpawnMouse();
            netHost = SEntMan.GetNetEntity(host);
            var middle = SpawnMouse();
            var member = SpawnMouse("MobMouse2");
            var melee = SEntMan.GetComponent<MeleeWeaponComponent>(host);
            expected = new DamageSpecifier(melee.Damage) + Blunt(7);
            Assert.That(Join(middle, member), Is.True);
            Assert.That(Join(host, middle), Is.True);
            Assert.That(Join(host, middle), Is.False);

            // Action and damage are deliberately in one simulation batch, before queued deletion.
            HurtChest(middle, 100);
            Assert.That(SEntMan.GetComponent<MouseSwarmComponent>(host).Members, Has.Count.EqualTo(2));
            melee.Damage += Blunt(7); // An unrelated modifier must survive component cleanup.
        });
        await RunTicks(3);
        await RunUntilSynced();
        await Server.WaitAssertion(() => Assert.That(CountMice(), Is.EqualTo(originalCount + 1)));
        await Client.WaitAssertion(() =>
        {
            var uid = ToClient(netHost);
            Assert.That(CEntMan.GetComponent<MouseSwarmComponent>(uid).Members, Has.Count.EqualTo(2));
            Assert.That(CEntMan.System<Robust.Client.GameObjects.SpriteSystem>()
                .LayerMapTryGet(uid, "mouse-swarm-1", out _, false), Is.True);
        });

        await Server.WaitAssertion(() =>
        {
            SEntMan.RemoveComponent<MouseSwarmComponent>(host);
            Assert.That(SEntMan.GetComponent<MeleeWeaponComponent>(host).Damage.DamageDict.Where(pair => pair.Value != 0),
                Is.EquivalentTo(expected.DamageDict.Where(pair => pair.Value != 0)));
            SEntMan.EnsureComponent<MouseSwarmComponent>(host);
            Assert.That(Join(host, SpawnMouse()), Is.True);
            SEntMan.RemoveComponent<MouseSwarmComponent>(host);
            Assert.That(SEntMan.GetComponent<MeleeWeaponComponent>(host).Damage.DamageDict.Where(pair => pair.Value != 0),
                Is.EquivalentTo(expected.DamageDict.Where(pair => pair.Value != 0)));
        });
        await RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(CEntMan.System<Robust.Client.GameObjects.SpriteSystem>()
            .LayerMapTryGet(ToClient(netHost), "mouse-swarm-0", out _, false), Is.False));
    }

    private EntityUid SpawnMouse(string prototype = "MobMouse")
    {
        var uid = SEntMan.SpawnAtPosition(prototype, SEntMan.GetCoordinates(TargetCoords));
        SEntMan.RemoveComponent<HTNComponent>(uid);
        return uid;
    }

    private bool Join(EntityUid host, EntityUid target)
    {
        var actions = SEntMan.System<SharedActionsSystem>();
        var action = actions.GetActions(host).Single(value =>
            SEntMan.GetComponent<MetaDataComponent>(value).EntityPrototype?.ID == "ActionMouseSwarm");
        return actions.PerformAction(host, action, new MouseSwarmActionEvent { Target = target }, predicted: false);
    }

    private void HurtChest(EntityUid target, FixedPoint2 amount)
    {
        SEntMan.System<DamageableSystem>().ChangeDamage(target, Blunt(amount),
            ignoreResistances: true, ignoreGlobalModifiers: true, targetPart: TargetBodyPart.Chest,
            ignoreBlockers: true, canMiss: false);
    }

    private static DamageSpecifier Blunt(FixedPoint2 amount)
        => new() { DamageDict = new() { ["Blunt"] = amount }, PartDamageVariation = 0 };

    private int CountMice()
    {
        var count = 0;
        var query = SEntMan.EntityQueryEnumerator<MouseSwarmComponent>();
        while (query.MoveNext(out _, out _))
            count++;
        return count;
    }
}
