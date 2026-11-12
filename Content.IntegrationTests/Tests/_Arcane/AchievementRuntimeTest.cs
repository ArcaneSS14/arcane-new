// Arcane-Start
#nullable enable
using System.Linq;
using Content.Arcane.Client.Achievements;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Server.Achievements;
using Content.Arcane.Server.Ghost;
using Content.Arcane.Shared.Achievements;
using Content.Goobstation.Common.Traits.Components;
using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Shared.Abductor;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Surgery;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Materials;
using Content.Server.Suicide;
using Content.Shared.ActionBlocker;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.GameTicking.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Arcane;

[EnsureCVar(Side.Server, typeof(ArcaneCVars), nameof(ArcaneCVars.AchievementsEnabled), true)]
public sealed class AchievementRuntimeTest : InteractionTest
{
    private const string MinerAchievement = "LegendaryMiner";
    private static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";
    protected override string PlayerPrototype => "TestArcaneAchievementHuman";
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };
    // This test broadcasts a synthetic round start without changing the ticker's round ID.
    private static PoolSettings LobbyPair => new() { InLobby = true, Fresh = true, Destructive = true };

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  id: TestArcaneAchievementHuman
  parent: MobHuman
  components:
  - type: InteractionPopup
    successChance: 1
    interactDelay: 0

- type: entity
  id: TestArcaneAchievementRecycler
  parent: Recycler
  components:
  - type: ApcPowerReceiver
    needsPower: false
  - type: Emagged
    emagType: Interaction
  - type: MaterialReclaimer
    powered: true
    enabled: true

- type: entity
  id: TestArcaneAchievementZeroRecycler
  parent: TestArcaneAchievementRecycler
  components:
  - type: MaterialReclaimer
    damageOnEmag:
      types:
        Slash: 0

- type: entity
  id: TestArcaneAchievementHeartSurgery
  components:
  - type: SurgeryOrganCondition
    organ: Heart
""";

    [Test]
    [PairConfig(nameof(LobbyPair))]
    public async Task ProgressIsCappedPersistedAndSentWithoutRevealingLockedSecrets()
    {
        var manager = SEntMan.System<AchievementManager>();
        AchievementPrototype counter = null!;
        AchievementPrototype secret = null!;
        await Server.WaitAssertion(() =>
        {
            Assert.That(manager.IsLoaded(ServerSession!), Is.True);
            counter = SProtoMan.Index<AchievementPrototype>(MinerAchievement);
            secret = SProtoMan.EnumeratePrototypes<AchievementPrototype>().First(p => p.Hidden
                && p.Condition is not Content.Arcane.Server.Achievements.Conditions.AchievementCountCondition
                && p.Condition is not Content.Arcane.Server.Achievements.Conditions.PlaytimeCondition);
            var locked = manager.BuildData(ServerSession!, secret);
            Assert.Multiple(() =>
            {
                Assert.That(locked.Name, Is.Null);
                Assert.That(locked.Description, Is.Null);
                Assert.That(locked.Icon, Is.Null);
            });
        });
        var received = 0;
        await Client.WaitPost(() => CEntMan.System<AchievementsSystem>().Unlocked += data =>
        {
            if (data.Id == counter.ID)
                received++;
        });
        await Server.WaitPost(() =>
        {
            manager.AddProgress(ServerSession!, counter, counter.Condition.Count - 1);
            Assert.That(manager.BuildData(ServerSession!, counter).UnlockedAt, Is.Null);
            manager.AddProgress(ServerSession!, counter, int.MaxValue);
            manager.AddProgress(ServerSession!, counter, int.MaxValue);
        });
        Assert.That(await RunServerTask(() => manager.SaveAll(drain: true)), Is.True);
        var rows = await Server.ResolveDependency<IServerDbManager>().GetAchievementProgressAsync(ServerSession!.UserId);
        var stored = rows.Single(r => r.AchievementId == counter.ID);
        Assert.That(stored.Progress, Is.EqualTo(counter.Condition.Count));
        Assert.That(stored.UnlockedAt, Is.Not.Null);
        await Client.WaitPost(() => CEntMan.System<AchievementsSystem>().RequestState());
        await Pair.ReallyBeIdle(10);
        await Client.WaitAssertion(() =>
        {
            var data = CEntMan.System<AchievementsSystem>().Achievements!;
            Assert.That(data, Is.Not.Null.And.Not.Empty);
            Assert.That(data.Single(d => d.Id == counter.ID).Progress, Is.EqualTo(counter.Condition.Count));
            Assert.That(data.Single(d => d.Id == secret.ID).Name, Is.Null);
            Assert.That(received, Is.EqualTo(1));
            var controller = Client.ResolveDependency<Robust.Client.UserInterface.IUserInterfaceManager>()
                .GetUIController<AchievementsUIController>();
            controller.ToggleWindow();
            controller.ToggleWindow();
        });
        await Server.WaitAssertion(() =>
        {
            PrepareMind();
            var cfg = Server.ResolveDependency<IConfigurationManager>();
            cfg.SetCVar(ArcaneCVars.AchievementsExcludedPresets, "");
            cfg.SetCVar(ArcaneCVars.AchievementsExcludedMaps, "");
            var achievements = SEntMan.System<AchievementSystem>();
            Assert.That(achievements.CanProgress(SPlayer, out _), Is.True);
            SEntMan.System<ServerGameTicker>().ShowRoundEndScoreboard();
            Assert.That(achievements.CanProgress(SPlayer, out _), Is.False);
            SEntMan.EventBus.RaiseEvent(EventSource.Local, new RoundStartingEvent(1));
            Assert.That(achievements.CanProgress(SPlayer, out _), Is.True);
        });

        var name = ServerSession!.Name;
        var network = Client.ResolveDependency<IClientNetManager>();
        await Client.WaitPost(() => network.ClientDisconnect("Achievement persistence regression"));
        await Pair.RunTicksSync(20);
        Client.SetConnectTarget(Server);
        await Client.WaitPost(() => network.ClientConnect(null!, 0, name));
        await Pair.RunTicksSync(20);
        await Pair.RunUntilSynced();
        ClientSession = Client.Session!;
        await RunServerTask(async () =>
        {
            await Server.ResolveDependency<UserDbDataManager>().WaitLoadComplete(Server.PlayerMan.GetSessionById(ClientSession.UserId));
            return true;
        });
        await Server.WaitAssertion(() =>
        {
            var session = Server.PlayerMan.GetSessionById(ClientSession.UserId);
            Assert.That(manager.IsLoaded(session), Is.True);
            var restored = manager.BuildData(session, counter);
            Assert.That(restored.Progress, Is.EqualTo(counter.Condition.Count));
            Assert.That(restored.UnlockedAt, Is.EqualTo(stored.UnlockedAt));
        });
    }

    [Test]
    public async Task GhostHugDoesNotParalyzeAndStaleOrLivingActorsCannotUseItsVerb()
    {
        EntityUid ghost = default;
        EntityUid human = default;
        Verb verb = null!;
        await Server.WaitAssertion(() =>
        {
            var mind = PrepareMind();
            SEntMan.EnsureComponent<SocialAnxietyComponent>(SPlayer);
            ghost = SEntMan.System<GhostSystem>().SpawnGhost(mind, SPlayer, canReturn: true)!.Value;
            var hugs = SEntMan.System<GhostHugSystem>();
            var verbs = SEntMan.System<SharedVerbSystem>().GetLocalVerbs(SPlayer, ghost, typeof(AlternativeVerb));
            verb = verbs.Single(v => v.Text == Loc.GetString("ghost-hug-verb"));
            Assert.That(verb.DoContactInteraction, Is.False);
            Assert.That(hugs.CanHug(ghost, SPlayer), Is.True);
            SEntMan.System<SharedVerbSystem>().ExecuteVerb(verb, ghost, SPlayer);
            Assert.That(SEntMan.HasComponent<KnockedDownComponent>(SPlayer), Is.False);
            Assert.That(SEntMan.HasComponent<StunnedComponent>(SPlayer), Is.False);
            Assert.That(SEntMan.System<ActionBlockerSystem>().CanInteract(ghost, SPlayer), Is.False);
            human = SEntMan.SpawnAtPosition("TestArcaneAchievementHuman", SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates);
            Assert.That(hugs.TryHug(human, SPlayer), Is.False);

            var before = SEntMan.GetComponent<InteractionPopupComponent>(SPlayer).LastInteractTime;
            var coordinates = SEntMan.GetComponent<TransformComponent>(ghost).Coordinates;
            SEntMan.System<SharedTransformSystem>().SetCoordinates(ghost, coordinates.Offset(new Vector2(4, 0)));
            verb.Act!();
            Assert.That(SEntMan.GetComponent<InteractionPopupComponent>(SPlayer).LastInteractTime, Is.EqualTo(before));

            // The ordinary physical hug still retains Trauma's trait behavior.
            var popup = SEntMan.GetComponent<InteractionPopupComponent>(SPlayer);
            SEntMan.System<InteractionPopupSystem>().SharedInteract(SPlayer, popup, new InteractHandEvent(human, SPlayer), SPlayer, human);
            Assert.That(SEntMan.HasComponent<KnockedDownComponent>(SPlayer), Is.True);
        });
    }

    [Test]
    public async Task RecyclerZeroDamageDoesNotAwardAndDamageOrSuicideNeverGibsTheBody()
    {
        EntityUid newBody = default;
        await Server.WaitAssertion(() =>
        {
            var mind = PrepareMind();
            var zero = SEntMan.SpawnAtPosition("TestArcaneAchievementZeroRecycler", SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates);
            var recycler = SEntMan.SpawnAtPosition("TestArcaneAchievementRecycler", SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates);
            var system = SEntMan.System<MaterialReclaimerSystem>();
            var manager = SEntMan.System<AchievementManager>();
            Assert.That(system.CanDamageAndGib(recycler, SPlayer,
                SEntMan.GetComponent<Content.Shared.Materials.MaterialReclaimerComponent>(recycler)), Is.True);
            system.Reclaim(zero, SPlayer);
            Assert.That(manager.IsUnlocked(ServerSession!, "FreakWorkplaceAccident"), Is.False);
            system.Reclaim(recycler, SPlayer);
            Assert.That(manager.IsUnlocked(ServerSession!, "FreakWorkplaceAccident"), Is.True);
            Assert.That(SEntMan.HasComponent<BodyComponent>(SPlayer), Is.True);
            newBody = SEntMan.SpawnAtPosition("TestArcaneAchievementHuman", SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates);
            SEntMan.System<SharedMindSystem>().TransferTo(mind, newBody);
            Assert.That(SEntMan.System<SuicideSystem>().Suicide(newBody), Is.True);
        });
        await Pair.RunTicksSync(3);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(SPlayer), Is.True);
            Assert.That(SEntMan.EntityExists(newBody), Is.True);
            Assert.That(SEntMan.HasComponent<BodyComponent>(newBody), Is.True);
        });
    }

    [Test]
    public async Task RepeatedAbductionUsesTheVictimsStateBeforeSurgeonBookkeeping()
    {
        await Server.WaitAssertion(() =>
        {
            PrepareMind();
            var bodies = SEntMan.System<BodySystem>();
            var parts = SEntMan.System<BodyPartSystem>();
            var manager = SEntMan.System<AchievementManager>();
            var originalHeart = bodies.GetOrgan(SPlayer, HeartCategory)!.Value;
            var torso = parts.GetParentPart(originalHeart)!.Value;
            Assert.That(parts.RemoveOrgan(torso, originalHeart), Is.True);
            var coords = SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates;
            var surgeon = SEntMan.SpawnAtPosition("TestArcaneAchievementHuman", coords);
            SEntMan.EnsureComponent<AbductorComponent>(surgeon);
            var victim = SEntMan.EnsureComponent<AbductorVictimComponent>(SPlayer);
            victim.Implanted = false;
            var heart = SEntMan.SpawnAtPosition("OrganDubiousHealth", coords);
            var surgery = SEntMan.SpawnAtPosition("TestArcaneAchievementHeartSurgery", coords);
            var step = SEntMan.SpawnAtPosition("SurgeryStepInsertOrgan", coords);

            var first = new SurgeryStepEvent(surgeon, SPlayer, torso, heart, surgery, step, false);
            SEntMan.EventBus.RaiseLocalEvent(step, ref first);
            Assert.That(first.Complete, Is.True);
            Assert.That(bodies.GetBody(heart), Is.EqualTo(SPlayer));
            Assert.That(manager.IsUnlocked(ServerSession!, "NotAgain"), Is.False);

            // Arrange another implantation after the old gland is gone; normal surgery cannot remove a gland.
            SEntMan.DeleteEntity(heart);
            heart = SEntMan.SpawnAtPosition("OrganDubiousHealth", coords);
            victim.Implanted = true; // This patient had already completed a previous experiment.
            var second = new SurgeryStepEvent(surgeon, SPlayer, torso, heart, surgery, step, false);
            SEntMan.EventBus.RaiseLocalEvent(step, ref second);
            Assert.That(second.Complete, Is.True);
            Assert.That(manager.IsUnlocked(ServerSession!, "NotAgain"), Is.True);
        });
    }

    private EntityUid PrepareMind()
    {
        var minds = SEntMan.System<SharedMindSystem>();
        var mind = minds.CreateMind(ServerSession!.UserId);
        minds.TransferTo(mind, SPlayer);
        SEntMan.System<SharedRoleSystem>().MindAddJobRole(mind, silent: true, jobPrototype: "Passenger");
        return mind;
    }

    private async Task<T> RunServerTask<T>(Func<Task<T>> action)
    {
        Task<T>? task = null;
        await Server.WaitPost(() => task = action());
        return await task!;
    }
}
// Arcane-End
