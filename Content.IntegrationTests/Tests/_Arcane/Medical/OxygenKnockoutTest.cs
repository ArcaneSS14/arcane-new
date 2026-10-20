using Content.Arcane.Common.Medical.HealthAnalyzer;
using Content.Arcane.Common.Medical.Respiration;
using Content.Arcane.Client.Medical.Overlays;
using Content.Arcane.Server.Medical.Respiration;
using Content.Arcane.Shared.Medical.Respiration;
using Content.Goobstation.Common.Body.Components;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Common.Body;
using Content.Medical.Common.Targeting;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Bed.Components;
using Content.Shared.Buckle;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(OxygenKnockoutSystem))]
public sealed class OxygenKnockoutTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damage = null!;
    [SidedDependency(Side.Server)] private readonly StatusEffectsSystem _effects = null!;
    [SidedDependency(Side.Server)] private readonly IGameTiming _timing = null!;
    [SidedDependency(Side.Client)] private readonly IOverlayManager _overlays = null!;
    [SidedDependency(Side.Client)] private readonly MedicalDamageOverlaySystem _medical = null!;

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  parent: MobHuman
  id: TestArcaneOxygenHuman
  components:
  - type: Respirator
    updateInterval: 1000
""";

    [Test]
    [RunOnSide(Side.Server)]
    public void OxygenSleepFollowsDamageThresholdsAndImmunityWithoutClearingSedation()
    {
        var patient = SSpawn("MobHuman");
        var thresholds = SEntMan.System<MobThresholdSystem>();
        var limit = SEntMan.System<OxygenThresholdSystem>().GetThreshold(patient)!.Value;
        Damage(patient, limit - 1f);
        Assert.That(Asleep(patient), Is.False);
        Damage(patient, 1);
        Assert.That(Asleep(patient), Is.True);
        Assert.That(SEntMan.System<MobStateSystem>().IsAlive(patient), Is.True);
        Assert.That(SEntMan.System<RespiratorSystem>().CanBreathe(patient, SComp<RespiratorComponent>(patient)), Is.True);

        thresholds.SetMobStateThreshold(patient, (limit + 10f) * 2f, MobState.SoftCrit);
        Assert.That(Asleep(patient), Is.False, "The same damage is below the new body's threshold.");
        Damage(patient, 10);
        Assert.That(Asleep(patient), Is.True);
        SEntMan.AddComponent<BreathingImmunityComponent>(patient);
        Assert.That(Asleep(patient), Is.False);
        SEntMan.RemoveComponent<BreathingImmunityComponent>(patient);
        Assert.That(Asleep(patient), Is.True);

        SEntMan.AddComponent<SpecialBreathingImmunityComponent>(patient);
        Assert.That(Asleep(patient), Is.False);
        SEntMan.RemoveComponent<SpecialBreathingImmunityComponent>(patient);
        Assert.That(Asleep(patient), Is.True);

        SEntMan.RemoveComponent<RespiratorComponent>(patient);
        Assert.That(Asleep(patient), Is.False);
        SEntMan.AddComponent<RespiratorComponent>(patient);
        Assert.That(Asleep(patient), Is.True);

        _effects.TrySetStatusEffectDuration(patient, SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(10));
        Damage(patient, -1);
        Assert.That(Asleep(patient), Is.False);
        Assert.That(SEntMan.HasComponent<SleepingComponent>(patient), Is.True);
    }

    [Test]
    public async Task DeadBodiesAccumulateOxygenDamageOnlyAfterEffectiveTreatmentStops()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        float afterTreatment = default;
        await Server.WaitPost(() =>
        {
            patient = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap!.GridCoords);
            Damage(patient, 20);
            _damage.ChangeDamage(patient, new() { DamageDict = { ["Blunt"] = 5 } },
                targetPart: TargetBodyPart.Vital, ignoreResistances: true);
            SEntMan.System<MobStateSystem>().ChangeMobState(patient, MobState.Dead);
        });
        await RunSeconds(9f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(patient), Is.EqualTo(20), "Ordinary dead bodies must not passively heal suffocation.");
            _damage.ChangeDamage(patient, new() { DamageDict = { ["Blunt"] = -1 } },
                targetPart: TargetBodyPart.Vital, ignoreResistances: true);
        });
        await RunSeconds(2.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(patient), Is.GreaterThan(20f), "Healing another damage type must not reset oxygen treatment grace.");
            Damage(patient, -5);
            afterTreatment = Oxygen(patient);
        });
        await RunSeconds(9f);
        await Server.WaitPost(() => Assert.That(Oxygen(patient), Is.EqualTo(afterTreatment)));
        await RunSeconds(2.2f);
        await Server.WaitPost(() => Assert.That(Oxygen(patient), Is.GreaterThan(afterTreatment)));
    }

    [Test]
    public async Task PostmortemLimitsFollowTheBodyAndTreatmentRestartsCappedPatients()
    {
        await Pair.CreateTestMap();
        EntityUid capped = default, scaled = default, excessive = default, upperStage = default;
        float cap = default, lower = default, upperStart = default;
        await Server.WaitPost(() =>
        {
            capped = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap!.GridCoords);
            scaled = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap.GridCoords);
            excessive = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap.GridCoords);
            upperStage = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap.GridCoords);
            lower = SEntMan.System<OxygenThresholdSystem>().GetThreshold(capped)!.Value;
            cap = lower * 2f;
            upperStart = (lower + cap) / 2f;
            Damage(capped, cap - 0.1f);
            Damage(scaled, lower);
            Damage(excessive, cap + 30f);
            Damage(upperStage, upperStart);
            SEntMan.System<MobThresholdSystem>().SetMobStateThreshold(scaled, cap * 1.2f, MobState.SoftCrit);
            foreach (var body in new[] { capped, scaled, excessive, upperStage })
                SEntMan.System<MobStateSystem>().ChangeMobState(body, MobState.Dead);
            Assert.That(SEntMan.HasComponent<PostmortemHypoxiaComponent>(excessive), Is.False);
        });
        await RunSeconds(12.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(capped), Is.EqualTo(cap));
            Assert.That(SEntMan.HasComponent<PostmortemHypoxiaComponent>(capped), Is.False);
            Assert.That(Oxygen(excessive), Is.EqualTo(cap + 30f), "Do not truncate existing damage above the cap.");
            var upperIncrease = Oxygen(upperStage) - upperStart;
            Assert.That(upperIncrease, Is.GreaterThan(0f));
            Assert.That(Oxygen(scaled) - lower, Is.GreaterThan(upperIncrease),
                "The same damage remains in the faster lower stage for the body with a higher threshold.");
            Damage(capped, -1);
            Assert.That(SEntMan.HasComponent<PostmortemHypoxiaComponent>(capped), Is.True);
        });
        await RunSeconds(9f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(capped), Is.EqualTo(cap - 1f));
        });
        await RunSeconds(2.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(Oxygen(capped), Is.GreaterThan(cap - 1f).And.LessThanOrEqualTo(cap),
                "Treatment restarts accumulation after its grace period without exceeding the body's cap.");
        });
    }

    [Test]
    public async Task BriefStasisAndTreatmentDuringStasisPreserveTheExactGracePeriod()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        EntityUid bed = default;
        TimeSpan deadline = default, pauseStart = default;
        await Server.WaitAssertion(() =>
        {
            patient = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap!.GridCoords);
            bed = SSpawnAtPosition("StasisBed", TestMap!.GridCoords);
            Damage(patient, 20);
            SEntMan.System<MobStateSystem>().ChangeMobState(patient, MobState.Dead);
        });
        await RunSeconds(0.1f);
        await Server.WaitAssertion(() =>
        {
            deadline = SComp<PostmortemHypoxiaComponent>(patient).NextDamage;
            pauseStart = _timing.CurTime;
            Assert.That(SEntMan.System<SharedBuckleSystem>().TryBuckle(patient, null, bed), Is.True);
            Assert.That(SEntMan.HasComponent<StasisBedBuckledComponent>(patient), Is.True);
        });
        await RunSeconds(0.3f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<SharedBuckleSystem>().TryUnbuckle(patient, null), Is.True);
            Assert.That(SEntMan.HasComponent<StasisBedBuckledComponent>(patient), Is.False);
            Assert.That(SComp<PostmortemHypoxiaComponent>(patient).NextDamage - _timing.CurTime, Is.EqualTo(deadline - pauseStart),
                "Stasis preserves the remaining grace period without restarting ten seconds.");
            Assert.That(SEntMan.System<SharedBuckleSystem>().TryBuckle(patient, null, bed), Is.True);
        });
        await RunSeconds(0.1f);
        await Server.WaitPost(() => Damage(patient, -1));
        await RunSeconds(0.3f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<SharedBuckleSystem>().TryUnbuckle(patient, null), Is.True);
            Assert.That(SComp<PostmortemHypoxiaComponent>(patient).NextDamage - _timing.CurTime, Is.EqualTo(TimeSpan.FromSeconds(10)),
                "Only effective oxygen treatment starts a new ten-second grace period, frozen until stasis ends.");

            var mobs = SEntMan.System<MobStateSystem>();
            mobs.ChangeMobState(patient, MobState.Alive);
            Assert.That(SEntMan.HasComponent<PostmortemHypoxiaComponent>(patient), Is.False);
            mobs.ChangeMobState(patient, MobState.Dead);
            Assert.That(SComp<PostmortemHypoxiaComponent>(patient).NextDamage - _timing.CurTime, Is.EqualTo(TimeSpan.FromSeconds(10)),
                "A new death starts a fresh grace period.");
        });
    }

    [Test]
    public async Task AnalyzerAndVignetteUseThePatientsReplicatedKnockoutThreshold()
    {
        await Pair.CreateTestMap();
        NetEntity patientNet = default;
        EntityUid? previousAttachment = null;
        try
        {
            await Server.WaitAssertion(() =>
            {
                previousAttachment = ServerSession!.AttachedEntity;
                var patient = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap!.GridCoords);
                SEntMan.System<MobThresholdSystem>().SetMobStateThreshold(patient, 120, MobState.SoftCrit);
                Damage(patient, 59);
                patientNet = SEntMan.GetNetEntity(patient);
                Server.PlayerMan.SetAttachedEntity(ServerSession, patient);
            });
            await AssertSymptoms(false, 59f / 60f);
            await Server.WaitPost(() => Damage(SEntMan.GetEntity(patientNet), 1));
            await AssertSymptoms(true, 1f);
            await Server.WaitPost(() => Damage(SEntMan.GetEntity(patientNet), 10));
            await AssertSymptoms(true, 1f);
            await Server.WaitPost(() => SEntMan.System<MobThresholdSystem>()
                .SetMobStateThreshold(SEntMan.GetEntity(patientNet), 160, MobState.SoftCrit));
            await AssertSymptoms(false, 70f / 80f);
            await Server.WaitPost(() => Damage(SEntMan.GetEntity(patientNet), -35));
            await AssertSymptoms(false, 35f / 80f);
            await Server.WaitPost(() => SEntMan.AddComponent<BreathingImmunityComponent>(SEntMan.GetEntity(patientNet)));
            await AssertSymptoms(false, 0f);
            await Server.WaitPost(() => SEntMan.RemoveComponent<BreathingImmunityComponent>(SEntMan.GetEntity(patientNet)));
            await AssertSymptoms(false, 35f / 80f);
        }
        finally
        {
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, previousAttachment));
            await Pair.RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                _medical.FrameUpdate(0f);
                Assert.That(_overlays.HasOverlay<OxygenVignetteOverlay>(), Is.False);
            });
        }

        async Task AssertSymptoms(bool expectedWarning, float expectedIntensity)
        {
            await Pair.RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                _medical.FrameUpdate(0f);
                var diagnostics = new HealthAnalyzerPopulateEvent();
                CEntMan.EventBus.RaiseLocalEvent(CEntMan.GetEntity(patientNet), ref diagnostics);
                Assert.That(diagnostics.Conditions.Contains(Loc.GetString("condition-body-asphyxiation-severe")), Is.EqualTo(expectedWarning));
                if (expectedIntensity == 0f)
                    Assert.That(_overlays.HasOverlay<OxygenVignetteOverlay>(), Is.False);
                else
                {
                    Assert.That(_overlays.TryGetOverlay<OxygenVignetteOverlay>(out var overlay), Is.True);
                    Assert.That(overlay!.Intensity, Is.EqualTo(expectedIntensity).Within(0.001f));
                }
            });
        }
    }

    private bool Asleep(EntityUid patient) => _effects.TryGetStatusEffect(patient, OxygenKnockoutSystem.OxygenSleep, out _);

    private float Oxygen(EntityUid patient) => _damage.GetAllDamage(patient).DamageDict.GetValueOrDefault("Asphyxiation").Float();

    private void Damage(EntityUid patient, float amount)
        => _damage.ChangeDamage(patient, new() { DamageDict = { ["Asphyxiation"] = amount } },
            targetPart: TargetBodyPart.Vital, ignoreResistances: true);
}
