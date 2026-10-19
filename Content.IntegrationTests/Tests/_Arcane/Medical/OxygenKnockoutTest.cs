using Content.Arcane.Common.Medical.HealthAnalyzer;
using Content.Arcane.Server.Medical.Respiration;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Common.Body;
using Content.Medical.Common.Targeting;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(OxygenKnockoutSystem))]
public sealed class OxygenKnockoutTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damage = null!;
    [SidedDependency(Side.Server)] private readonly StatusEffectsSystem _effects = null!;

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
        Damage(patient, 49);
        Assert.That(Asleep(patient), Is.False);
        Damage(patient, 1);
        Assert.That(Asleep(patient), Is.True);
        Assert.That(SEntMan.System<MobStateSystem>().IsAlive(patient), Is.True);
        Assert.That(SEntMan.System<RespiratorSystem>().CanBreathe(patient, SComp<RespiratorComponent>(patient)), Is.True);

        thresholds.SetMobStateThreshold(patient, 120, MobState.SoftCrit);
        Assert.That(Asleep(patient), Is.False, "The same damage is below the new body's threshold.");
        Damage(patient, 10);
        Assert.That(Asleep(patient), Is.True);
        SEntMan.AddComponent<BreathingImmunityComponent>(patient);
        Assert.That(Asleep(patient), Is.False);
        SEntMan.RemoveComponent<BreathingImmunityComponent>(patient);
        Assert.That(Asleep(patient), Is.True);

        _effects.TrySetStatusEffectDuration(patient, SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(10));
        Damage(patient, -1);
        Assert.That(Asleep(patient), Is.False);
        Assert.That(SEntMan.HasComponent<SleepingComponent>(patient), Is.True);
    }

    [Test]
    public async Task AnalyzerWarningUsesThePatientsReplicatedKnockoutThreshold()
    {
        await Pair.CreateTestMap();
        NetEntity patientNet = default;
        await Server.WaitPost(() =>
        {
            var patient = SSpawnAtPosition("TestArcaneOxygenHuman", TestMap!.GridCoords);
            SEntMan.System<MobThresholdSystem>().SetMobStateThreshold(patient, 120, MobState.SoftCrit);
            Damage(patient, 59);
            patientNet = SEntMan.GetNetEntity(patient);
            Server.PlayerMan.SetAttachedEntity(ServerSession, patient);
        });
        await AssertWarning(false);
        await Server.WaitPost(() => Damage(SEntMan.GetEntity(patientNet), 1));
        await AssertWarning(true);

        async Task AssertWarning(bool expected)
        {
            await Pair.RunUntilSynced();
            await Client.WaitPost(() =>
            {
                var diagnostics = new HealthAnalyzerPopulateEvent();
                CEntMan.EventBus.RaiseLocalEvent(CEntMan.GetEntity(patientNet), ref diagnostics);
                Assert.That(diagnostics.Conditions.Contains(Loc.GetString("condition-body-asphyxiation-severe")), Is.EqualTo(expected));
            });
        }
    }

    private bool Asleep(EntityUid patient) => _effects.TryGetStatusEffect(patient, OxygenKnockoutSystem.OxygenSleep, out _);

    private void Damage(EntityUid patient, int amount)
        => _damage.ChangeDamage(patient, new() { DamageDict = { ["Asphyxiation"] = amount } },
            targetPart: TargetBodyPart.Vital, ignoreResistances: true);
}
