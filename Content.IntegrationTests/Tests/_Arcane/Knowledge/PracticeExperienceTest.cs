using System.Collections.Generic;
using Content.Arcane.Common.Medical.Skills;
using Content.Arcane.Shared.Knowledge;
using Content.Goobstation.Common.Weapons.Ranged;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Shared.Surgery;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane.Knowledge;

[TestOf(typeof(PracticeExperienceSystem))]
public sealed class PracticeExperienceTest : GameTest
{
    private static readonly EntProtoId Human = "MobHuman";
    private static readonly EntProtoId FirstAid = "FirstAidKnowledge";
    private static readonly EntProtoId Shooting = "ShootingKnowledge";
    private static readonly EntProtoId Surgery = "SurgeryKnowledge";
    private static readonly EntProtoId Unarmed = SharedMeleeWeaponSystem.MeleeKnowledge;

    [SidedDependency(Side.Server)] private readonly SharedKnowledgeSystem _knowledge = null!;

    private int Experience(EntityUid user, EntProtoId skill)
        => _knowledge.GetKnowledge(_knowledge.GetContainer(user)!.Value, skill)!.Value.Comp.Experience;

    private void Learn(EntityUid user, EntProtoId skill, int level = 0)
        => _knowledge.EnsureKnowledge(_knowledge.GetContainer(user)!.Value, skill, level);

    private void Hit(EntityUid user, EntityUid target)
    {
        var ev = new MeleeHitEvent([target], user, user, new DamageSpecifier(), null, EntityCoordinates.Invalid);
        SEntMan.EventBus.RaiseLocalEvent(user, ev, true);
    }

    [Test]
    public async Task HittingALivingTargetTrainsUnarmed()
    {
        await Server.WaitPost(() =>
        {
            var user = SSpawn(Human);
            var target = SSpawn(Human);
            Learn(user, Unarmed);

            Hit(user, user);
            Assert.That(Experience(user, Unarmed), Is.EqualTo(0), "Hitting yourself should not be practice");

            Hit(user, target);
            Assert.That(Experience(user, Unarmed), Is.EqualTo(2));

            SDel(user);
            SDel(target);
        });
    }

    [Test]
    public async Task TreatingPatientsTrainsMedicalSkills()
    {
        await Server.WaitPost(() =>
        {
            var user = SSpawn(Human);
            Learn(user, FirstAid);
            Learn(user, Surgery);

            var healing = new UserAppliedHealingEvent();
            SEntMan.EventBus.RaiseLocalEvent(user, ref healing);
            Assert.That(Experience(user, FirstAid), Is.GreaterThan(0));

            var step = new SurgeryStepEvent(user, user, user, user, user, user, true);
            SEntMan.EventBus.RaiseLocalEvent(user, ref step);
            Assert.That(Experience(user, Surgery), Is.GreaterThan(0));

            SDel(user);
        });
    }

    [Test]
    public async Task ShootingHasNoLevelLimit()
    {
        await Server.WaitPost(() =>
        {
            var user = SSpawn(Human);
            Learn(user, Shooting, 50);

            var ev = new AmmoShotUserEvent(user, new List<EntityUid>());
            SEntMan.EventBus.RaiseLocalEvent(user, ref ev);
            Assert.That(Experience(user, Shooting), Is.EqualTo(2));

            SDel(user);
        });
    }
}
