using System.Linq;
using Content.Arcane.Shared.Bitrunning.Components;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Buckle;
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
using Content.Shared.Ghost.Components;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P14TraitorRuntimeTest : InteractionTest
{
    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    [Test]
    public async Task TeachALessonCountsCurrentBodyDeathOrPermanentGhostDeparture()
    {
        await Server.WaitAssertion(() =>
        {
            var minds = SEntMan.System<SharedMindSystem>();
            var ghostSystem = SEntMan.System<GhostSystem>();
            var mobs = SEntMan.System<MobStateSystem>();
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

            mobs.ChangeMobState(body, MobState.Dead);
            Assert.That(objective.HasDied, Is.False);
            mobs.ChangeMobState(newBody, MobState.Dead);
            Assert.That(objective.HasDied, Is.True);

            var departingTarget = minds.CreateMind(null, "Departing lesson target");
            minds.TransferTo(departingTarget, SEntMan.SpawnAttachedTo("MobHuman", Center));
            var departureObjective = AssignLesson(departingTarget.Owner);
            Assert.That(departureObjective.HasDied, Is.False);
            Assert.That(ghostSystem.SpawnGhost(departingTarget.AsNullable(), Center, canReturn: false), Is.Not.Null);
            Assert.That(departureObjective.HasDied, Is.True);

            var offlineTarget = minds.CreateMind(null, "Offline lesson target");
            var offlineBody = SEntMan.SpawnAttachedTo("MobHuman", Center);
            minds.TransferTo(offlineTarget, offlineBody);
            var offlineObjective = AssignLesson(offlineTarget.Owner);
            var visitingGhost = ghostSystem.SpawnGhost(offlineTarget.AsNullable(), Center, canReturn: true);
            Assert.That(visitingGhost, Is.Not.Null);
            Assert.That(offlineTarget.Comp.VisitingEntity, Is.EqualTo(visitingGhost));
            Assert.That(offlineTarget.Comp.OwnedEntity, Is.EqualTo(offlineBody));
            Assert.That(offlineObjective.HasDied, Is.False);

            SEntMan.DeleteEntity(offlineBody);
            Assert.That(offlineTarget.Comp.OwnedEntity, Is.EqualTo(visitingGhost));
            Assert.That(SEntMan.GetComponent<GhostComponent>(visitingGhost!.Value).CanReturnToBody, Is.False);
            Assert.That(offlineObjective.HasDied, Is.True);
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
