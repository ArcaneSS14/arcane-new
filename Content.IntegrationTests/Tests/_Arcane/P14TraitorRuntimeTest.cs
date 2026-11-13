using System.Linq;
using Content.Arcane.Shared.Bitrunning.Components;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Ghost;
using Content.Server.Objectives.Systems;
using Content.Shared.Codewords;
using Content.Shared.Emag.Systems;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Projectiles;
using Content.Shared.Salvage.Fulton;
using Content.Shared.Stacks;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Trauma.Server.Objectives;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P14TraitorRuntimeTest : InteractionTest
{
    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    [Test]
    public async Task TeachALessonTracksTheRunnerRatherThanTheAvatar()
    {
        await Server.WaitAssertion(() =>
        {
            var minds = SEntMan.System<SharedMindSystem>();
            var states = SEntMan.System<MobStateSystem>();
            var target = minds.CreateMind(null, "Lesson target");
            var body = SEntMan.SpawnAttachedTo("MobHuman", Center);
            minds.TransferTo(target, body);
            var objective = AssignLesson(target.Owner);

            var avatar = SEntMan.SpawnAttachedTo("MobHuman", Center);
            SEntMan.AddComponent<AvatarConnectionComponent>(avatar).OriginalBody = body;
            minds.TransferTo(target, avatar);
            states.ChangeMobState(avatar, MobState.Dead);
            Assert.That(objective.HasDied, Is.False);

            states.ChangeMobState(body, MobState.Dead);
            Assert.That(objective.HasDied, Is.True);
        });
    }

    [Test]
    public async Task TeachALessonOnlyCountsPermanentGhostDeparture()
    {
        await Server.WaitAssertion(() =>
        {
            var minds = SEntMan.System<SharedMindSystem>();
            var ghostSystem = SEntMan.System<GhostSystem>();
            var target = minds.CreateMind(null, "Lesson target");
            var body = SEntMan.SpawnAttachedTo("MobHuman", Center);
            minds.TransferTo(target, body);
            var objective = AssignLesson(target.Owner);

            Assert.That(ghostSystem.SpawnGhost(target.AsNullable(), Center, canReturn: true), Is.Not.Null);
            Assert.That(objective.HasDied, Is.False);
            minds.UnVisit(target);
            var newBody = SEntMan.SpawnAttachedTo("MobHuman", Center);
            minds.TransferTo(target, newBody);
            Assert.That(objective.HasDied, Is.False);

            Assert.That(ghostSystem.SpawnGhost(target.AsNullable(), Center, canReturn: false), Is.Not.Null);
            Assert.That(objective.HasDied, Is.True);
        });
    }

    [Test]
    public async Task TraitorCodewordsAreDistinctAndKeepTheConfiguredCount()
    {
        await Server.WaitAssertion(() =>
        {
            var generator = SEntMan.System<CodewordSystem>();
            ProtoId<CodewordGeneratorPrototype> traitor = "TraitorCodewordGenerator";
            for (var draw = 0; draw < 20; draw++)
            {
                var words = generator.GenerateCodewords(traitor);
                Assert.That(words, Has.Length.EqualTo(4));
                Assert.That(words.Distinct().Count(), Is.EqualTo(4));
            }
        });
    }

    [Test]
    public async Task SleepingCarpOnlyDodgesWhileActiveAndStanding()
    {
        await Server.WaitAssertion(() =>
        {
            var knowledge = SEntMan.System<SharedKnowledgeSystem>();
            var target = SEntMan.SpawnAttachedTo("MobHuman", Center);
            var projectile = SEntMan.SpawnAttachedTo(null, Center);

            Assert.That(DodgeHitscan(), Is.False);
            knowledge.AddKnowledgeUnits(target, new Dictionary<EntProtoId, int> { ["MartialArtSleepingCarp"] = 1 });

            Assert.That(DodgeHitscan(), Is.True);
            var dodge = new ProjectileDodgeAttemptEvent(projectile, target);
            SEntMan.EventBus.RaiseLocalEvent(target, ref dodge);
            Assert.That(dodge.Dodged, Is.True);

            SEntMan.System<MobStateSystem>().ChangeMobState(target, MobState.Critical);
            Assert.That(DodgeHitscan(), Is.False);

            bool DodgeHitscan()
            {
                var hitscan = new HitscanDodgeAttemptEvent(target);
                SEntMan.EventBus.RaiseLocalEvent(target, ref hitscan);
                return hitscan.Dodged;
            }
        });
    }

    [Test]
    public async Task ScramDoesNotHandleActivationWithoutASafeTile()
    {
        await Server.WaitAssertion(() =>
        {
            var implant = SEntMan.SpawnAttachedTo("ScramImplant", Center);
            var target = SEntMan.SpawnAttachedTo("MobHuman", Center);
            var transform = SEntMan.GetComponent<TransformComponent>(target);
            var activation = new ActivateImplantEvent { Performer = target };

            SEntMan.EventBus.RaiseLocalEvent(implant, activation);

            Assert.That(activation.Handled, Is.False);
            Assert.That(transform.Coordinates, Is.EqualTo(Center));
        });
    }

    [Test]
    public async Task FultonEmagExtendsTheTargetListAndSurvivesSplit()
    {
        await Server.WaitAssertion(() =>
        {
            var fulton = SEntMan.System<SharedFultonSystem>();
            var stacks = SEntMan.System<SharedStackSystem>();
            var user = SEntMan.SpawnAttachedTo("MobHuman", Center);
            var pack = SEntMan.SpawnAttachedTo("Fulton", Center);
            var ordinary = SEntMan.SpawnAttachedTo("Fulton1", Center);
            var target = SEntMan.SpawnAttachedTo(null, Center);
            SEntMan.AddComponent<MindContainerComponent>(target);
            var packComp = SEntMan.GetComponent<FultonComponent>(pack);

            Assert.That(fulton.CanApplyFulton(user, packComp), Is.True);
            Assert.That(fulton.CanApplyFulton(target, packComp), Is.False);
            var emag = new GotEmaggedEvent(user, EmagType.Interaction);
            SEntMan.EventBus.RaiseLocalEvent(pack, ref emag);
            Assert.That(emag.Handled, Is.True);
            Assert.That(fulton.CanApplyFulton(target, packComp), Is.True);

            var split = stacks.Split(pack, 1, Center);
            Assert.That(split, Is.Not.Null);
            Assert.That(fulton.CanApplyFulton(target, SEntMan.GetComponent<FultonComponent>(split!.Value)), Is.True);
            Assert.That(stacks.TryMergeStacks(split.Value, ordinary, out _), Is.False);
            Assert.That(stacks.TryMergeStacks(ordinary, split.Value, out _), Is.False);
            Assert.That(stacks.TryMergeStacks(split.Value, pack, out var moved), Is.True);
            Assert.That(moved, Is.EqualTo(1));
        });
    }

    private TeachALessonConditionComponent AssignLesson(EntityUid target)
    {
        var teacher = SEntMan.System<SharedMindSystem>().CreateMind(null, "Teacher");
        var objective = SEntMan.SpawnAttachedTo("TeachRandomPersonObjective", Center);
        SEntMan.System<TargetObjectiveSystem>().SetTarget(objective, target);
        var assigned = new ObjectiveAfterAssignEvent(teacher, teacher.Comp, SEntMan.GetComponent<ObjectiveComponent>(objective),
            SEntMan.GetComponent<MetaDataComponent>(objective));
        SEntMan.EventBus.RaiseLocalEvent(objective, ref assigned);
        return SEntMan.GetComponent<TeachALessonConditionComponent>(objective);
    }
}
