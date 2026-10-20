#nullable enable
using System.Linq;
using Content.Arcane.Common.Medical.Heart;
using Content.Arcane.Common.Medical.Respiration;
using Content.Arcane.Server.Medical.Respiration;
using Content.Arcane.Shared.Medical.Heart;
using Content.Arcane.Shared.Medical.Respiration;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Common.Body;
using Content.Medical.Common.Targeting;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Trauma.Shared.Medical;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(CPRRecoverySystem))]
public sealed class CPRRecoveryTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damage = null!;
    [SidedDependency(Side.Server)] private readonly MobStateSystem _mob = null!;
    [SidedDependency(Side.Server)] private readonly SharedDoAfterSystem _doAfter = null!;
    [SidedDependency(Side.Server)] private readonly StatusEffectsSystem _effects = null!;
    [SidedDependency(Side.Server)] private readonly HeartRateSystem _heart = null!;

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  parent: MobHuman
  id: TestArcaneCPRHuman
  components:
  - type: Respirator
    updateInterval: 1000
- type: entity
  parent: MobIPC
  id: TestArcaneCPRIPC
- type: entity
  id: TestArcaneRecoveryDummy
  components:
  - type: MobState
""";

    [Test]
    public async Task OneSessionOwnsTimingPerfusionAndHandoff()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default, otherPatient = default, first = default, second = default;
        ushort oldIndex = default;
        await Server.WaitPost(() =>
        {
            patient = SpawnHuman();
            first = SpawnHuman();
            second = SpawnHuman();
            Damage(patient, 180);
            _mob.ChangeMobState(patient, MobState.Dead);
            Start(patient, first);
            oldIndex = SComp<ActiveCPRComponent>(patient).DoAfterIndex!.Value;
            Start(patient, second);
            Assert.That(SComp<ActiveCPRComponent>(patient).Performer, Is.EqualTo(first));
            Assert.That(SEntMan.HasComponent<CPRPerfusionComponent>(patient), Is.False);
        });
        await RunSeconds(2.8f);
        await Server.WaitPost(() => Assert.That(Oxygen(patient), Is.EqualTo(180)));
        await RunSeconds(0.4f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(patient), Is.EqualTo(177));
            Assert.That(SEntMan.HasComponent<CPRPerfusionComponent>(patient), Is.True);
        });
        await RunSeconds(0.8f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(patient), Is.EqualTo(174));
            _doAfter.Cancel(first, oldIndex);
            Assert.That(SEntMan.HasComponent<CPRPerfusionComponent>(patient), Is.False);
        });
        await RunSeconds(0.1f);
        await Server.WaitPost(() =>
        {
            Start(patient, second);
            Assert.That(SComp<ActiveCPRComponent>(patient).Performer, Is.EqualTo(second));
        });
        await RunSeconds(1f);
        await Server.WaitPost(() => Assert.That(Oxygen(patient), Is.EqualTo(174), "A new performer starts a three-second cycle."));

        await Server.WaitPost(() =>
        {
            otherPatient = SpawnHuman();
            Damage(otherPatient, 180);
            _mob.ChangeMobState(otherPatient, MobState.Dead);
            Start(otherPatient, second);
        });
        await RunSeconds(3.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(SEntMan.HasComponent<CPRPerfusionComponent>(patient), Is.True);
            Assert.That(SEntMan.HasComponent<CPRPerfusionComponent>(otherPatient), Is.True);
            SEntMan.DeleteEntity(second);
            Assert.That(SEntMan.HasComponent<CPRPerfusionComponent>(patient), Is.False);
            Assert.That(SEntMan.HasComponent<CPRPerfusionComponent>(otherPatient), Is.False);
        });
        await RunSeconds(0.1f);
        await Server.WaitPost(() =>
        {
            Assert.That(SEntMan.HasComponent<ActiveCPRComponent>(patient), Is.False);
            Assert.That(SEntMan.HasComponent<ActiveCPRComponent>(otherPatient), Is.False);
            Start(patient, first);
            Assert.That(SComp<ActiveCPRComponent>(patient).Performer, Is.EqualTo(first));
        });
    }

    [Test]
    public async Task NonBreathingPerformersCompressWithoutOxygenTreatment()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        await Server.WaitPost(() =>
        {
            patient = SpawnHuman();
            var ipc = SSpawnAtPosition("TestArcaneCPRIPC", TestMap!.GridCoords);
            var human = SpawnHuman();
            Damage(patient, 180);
            _mob.ChangeMobState(patient, MobState.Dead);
            SEntMan.AddComponent<BreathingImmunityComponent>(human);
            var capabilities = new CPRCapabilitiesEvent(human);
            SEntMan.EventBus.RaiseLocalEvent(patient, ref capabilities);
            Assert.That(capabilities.CanPerform, Is.True);
            Assert.That(capabilities.CanVentilate, Is.False);
            Start(patient, ipc);
            Assert.That(SComp<ActiveCPRComponent>(patient).CanVentilate, Is.False);
            var heart = _heart.GetHeart(patient)!.Value;
            _heart.SetRate(heart.Owner, heart.Comp!, 0, true);
        });
        await RunSeconds(3.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(patient), Is.EqualTo(180));
            var circulation = new FindWorkingHeartEvent();
            SEntMan.EventBus.RaiseLocalEvent(patient, ref circulation);
            Assert.That(circulation.Found, Is.True);
        });
    }

    [Test]
    public async Task TreatmentUsesCurrentThresholdAndRevivalRestartsOnlyStoppedHearts()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        await Server.WaitPost(() =>
        {
            patient = SpawnHuman();
            var rescuer = SpawnHuman();
            Damage(patient, 52);
            _mob.ChangeMobState(patient, MobState.Dead);
            var cycle = new CPRCompletedEvent(rescuer, true, 0.3f);
            SEntMan.EventBus.RaiseLocalEvent(patient, ref cycle);
            Assert.That(Oxygen(patient), Is.EqualTo(49));
            Assert.That(cycle.ReviveChance, Is.EqualTo(0.02f).Within(0.0001f));
            SEntMan.System<MobThresholdSystem>().SetMobStateThreshold(patient, 120, MobState.SoftCrit);
            cycle = new CPRCompletedEvent(rescuer, false, 0.3f);
            SEntMan.EventBus.RaiseLocalEvent(patient, ref cycle);
            Assert.That(cycle.ReviveChance, Is.EqualTo(1f - 49f / 60f).Within(0.0001f));
            Damage(patient, -49);
            var heart = _heart.GetHeart(patient)!.Value;
            _heart.SetRate(heart.Owner, heart.Comp!, 0, true);
            _mob.ChangeMobState(patient, MobState.Dead);
            Start(patient, rescuer);
        });
        await RunSeconds(3.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(_mob.IsDead(patient), Is.False);
            Assert.That(_heart.GetHeart(patient)!.Value.Comp!.CurrentRate, Is.GreaterThanOrEqualTo(40));
            Assert.That(_effects.TryGetStatusEffect(patient, PostRevivalRecoverySystem.RecoverySleep, out _), Is.True);
            var heart = _heart.GetHeart(patient)!.Value;
            _heart.SetRate(heart.Owner, heart.Comp!, 20, true);
            var revived = new CPRRevivedEvent(patient);
            SEntMan.EventBus.RaiseLocalEvent(patient, ref revived);
            Assert.That(heart.Comp!.CurrentRate, Is.EqualTo(20), "CPR must not fix a beating fibrillating heart.");
        });
    }

    [Test]
    public async Task ProcedureVerbsAndRecoveryActionsUseSharedState()
    {
        await Pair.CreateTestMap();
        NetEntity patientNet = default, rescuerNet = default;
        EntityUid? previousAttachment = null;
        try
        {
            await Server.WaitPost(() =>
            {
                var patient = SpawnHuman();
                var rescuer = SpawnHuman();
                SEntMan.EnsureComponent<CPRTrainingComponent>(rescuer);
                Damage(patient, 120);
                _mob.ChangeMobState(patient, MobState.SoftCrit);
                previousAttachment = ServerSession!.AttachedEntity;
                Server.PlayerMan.SetAttachedEntity(ServerSession, rescuer);
                patientNet = SEntMan.GetNetEntity(patient);
                rescuerNet = SEntMan.GetNetEntity(rescuer);
            });
            await AssertLocalVerb("cpr-verb");
            await Server.WaitPost(() => SEntMan.AddComponent<BreathingImmunityComponent>(SEntMan.GetEntity(rescuerNet)));
            await AssertLocalVerb("arcane-chest-compressions-verb");
            await Server.WaitPost(() => SEntMan.RemoveComponent<BreathingImmunityComponent>(SEntMan.GetEntity(rescuerNet)));
            await AssertLocalVerb("cpr-verb");
            await Server.WaitPost(() =>
            {
                var patient = SEntMan.GetEntity(patientNet);
                _mob.ChangeMobState(patient, MobState.Dead);
                _mob.ChangeMobState(patient, MobState.Alive);
                Server.PlayerMan.SetAttachedEntity(ServerSession, patient);
            });
            await RunSeconds(3f);
            await RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                var patient = CEntMan.GetEntity(patientNet);
                Assert.That(CEntMan.HasComponent<SleepingComponent>(patient), Is.True);
                Assert.That(CEntMan.System<SleepingSystem>().TryWakeWithCooldown(patient), Is.False);
                Assert.That(CEntMan.System<SharedStunSystem>().TryStanding(patient), Is.False);
            });
        }
        catch (Exception error)
        {
            TestContext.Error.WriteLine(error);
            throw;
        }
        finally
        {
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, previousAttachment));
        }

        async Task AssertLocalVerb(string key)
        {
            await RunSeconds(0.2f);
            await RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                var verbs = CEntMan.System<SharedVerbSystem>().GetLocalVerbs(CEntMan.GetEntity(patientNet),
                    CEntMan.GetEntity(rescuerNet), typeof(InnateVerb));
                Assert.That(verbs.Count(v => v.Text == Loc.GetString(key)), Is.EqualTo(1),
                    "The procedure must be available without fetching server verbs.");
            });
        }
    }

    [Test]
    public async Task RecoveryExpiresWithoutRemovingOtherSleepSources()
    {
        await Pair.CreateTestMap();
        EntityUid recovering = default, sedated = default;
        await Server.WaitPost(() =>
        {
            var dummy = SSpawn("TestArcaneRecoveryDummy");
            _mob.ChangeMobState(dummy, MobState.Dead);
            _mob.ChangeMobState(dummy, MobState.Alive);
            Assert.That(_effects.TryGetStatusEffect(dummy, PostRevivalRecoverySystem.RecoverySleep, out _), Is.False);
            Assert.That(_effects.TryGetStatusEffect(dummy, PostRevivalRecoverySystem.RecoveryDrowsiness, out _), Is.False);

            recovering = SpawnHuman();
            sedated = SpawnHuman();
            foreach (var patient in new[] { recovering, sedated })
            {
                _mob.ChangeMobState(patient, MobState.Dead);
                _mob.ChangeMobState(patient, MobState.Alive);
            }
            _effects.TrySetStatusEffectDuration(sedated, SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(20));
        });
        await RunSeconds(3f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<SleepingComponent>(recovering), Is.True);
            Assert.That(SEntMan.System<SleepingSystem>().TryWakeWithCooldown(recovering), Is.False,
                "The wake action must not end the five-second recovery sleep early.");
            Assert.That(SEntMan.System<SharedStunSystem>().TryStanding(recovering), Is.False,
                "Standing up must not bypass recovery sleep.");
        });
        await RunSeconds(2.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(SEntMan.HasComponent<SleepingComponent>(recovering), Is.False);
            Assert.That(SEntMan.HasComponent<SleepingComponent>(sedated), Is.True);
            Assert.That(_effects.TryGetStatusEffect(recovering, PostRevivalRecoverySystem.RecoveryDrowsiness, out _), Is.True);
        });
        await RunSeconds(30.1f);
        await Server.WaitPost(() => Assert.That(
            _effects.TryGetStatusEffect(recovering, PostRevivalRecoverySystem.RecoveryDrowsiness, out _), Is.False));
    }

    private EntityUid SpawnHuman() => SSpawnAtPosition("TestArcaneCPRHuman", TestMap!.GridCoords);

    private void Damage(EntityUid patient, int amount)
        => _damage.ChangeDamage(patient, new() { DamageDict = { ["Asphyxiation"] = amount } },
            targetPart: TargetBodyPart.Vital, ignoreResistances: true, interruptsDoAfters: false);

    private float Oxygen(EntityUid patient)
        => _damage.GetAllDamage(patient).DamageDict.GetValueOrDefault("Asphyxiation").Float();

    private void Start(EntityUid patient, EntityUid performer)
        => SEntMan.System<SharedVerbSystem>().GetLocalVerbs(patient, performer, typeof(InnateVerb))
            .Single(verb => verb.Text == Loc.GetString("cpr-verb") || verb.Text == Loc.GetString("arcane-chest-compressions-verb"))
            .Act!.Invoke();
}
